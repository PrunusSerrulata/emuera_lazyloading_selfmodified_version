#!/usr/bin/env python3
"""Dependency-free Windows/Wine protocol smoke tests; fixtures are copied, never run in place."""

import argparse
import json
import math
import os
from pathlib import Path
import queue
import shutil
import signal
import subprocess
import sys
import tempfile
import threading
import time

TESTS = Path(__file__).resolve().parent
BASELINE = "57170459b3d5ca175a1c57933058b569088bee0e"


class SmokeWatchdog:
    """Independent observer; never sends a second request to the busy oracle."""

    def __init__(self, deadline):
        self.deadline = deadline
        self.lock = threading.Lock()
        self.closed = threading.Event()
        self.oracle = None
        self.state = {"case": None, "phase": "starting", "pending": None, "lastFullResponse": None}
        self.thread = threading.Thread(target=self._watch, daemon=True)
        self.thread.start()

    def publish(self, **values):
        with self.lock:
            self.state.update(values)

    def attach(self, oracle):
        with self.lock:
            self.oracle = oracle

    def _watch(self):
        previous, next_sample = None, time.monotonic() + 5
        while not self.closed.wait(0.05):
            now = time.monotonic()
            if now < next_sample and now < self.deadline:
                continue
            with self.lock:
                oracle = self.oracle
                process = None if oracle is None else {"pid": oracle.process.pid, "returncode": oracle.process.poll()}
                snapshot = {**self.state, "process": process}
            print(json.dumps({"smokeWatchdog": snapshot}, ensure_ascii=False), file=sys.stderr, flush=True)
            failure = "wall-clock budget exhausted" if now >= self.deadline else (
                "unchanged complete observations at consecutive 5s samples" if same_observation(previous, snapshot) else None)
            if failure:
                print(f"FAIL watchdog: {failure}", file=sys.stderr, flush=True)
                try:
                    if oracle is not None:
                        oracle.kill()
                finally:
                    # Terminate even if the main thread is blocked in pipe/file work.
                    os._exit(2)
            previous = snapshot
            next_sample += 5

    def close(self):
        self.closed.set()
        self.thread.join(timeout=1)


def same_observation(previous, current):
    return previous is not None and comparison_state(previous) == comparison_state(current)


def comparison_state(snapshot):
    """Strip only known envelope metadata, never recurse into script result/watches."""
    state = dict(snapshot)
    state.pop("reportMetadata", None)
    for key in ("pending", "lastFullResponse"):
        envelope = state.get(key)
        if isinstance(envelope, dict):
            state[key] = {name: value for name, value in envelope.items() if name != "id"}
    return state


def equal(actual, expected):
    if actual != expected:
        raise AssertionError(f"expected {expected!r}, got {actual!r}")


class Oracle:
    def __init__(self, args, directory, deadline, watchdog):
        self.args, self.directory, self.deadline = args, directory, deadline
        self.watchdog = watchdog
        self.responses = queue.Queue()
        self.sequence = 0
        self.last_response = None
        self.stderr_path = directory / "stderr.log"
        self.stderr = self.stderr_path.open("w", encoding="utf-8")
        command = ([args.wine] if args.wine else []) + [str(args.exe)]
        self.process = subprocess.Popen(
            command, cwd=directory, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            stderr=self.stderr, text=True, encoding="utf-8", errors="strict",
            start_new_session=os.name != "nt",
        )
        watchdog.attach(self)
        threading.Thread(target=self._read, daemon=True).start()

    def _read(self):
        try:
            for line in self.process.stdout:
                self.responses.put(line)
        except Exception as error:
            self.responses.put(error)
        finally:
            self.responses.put(None)

    def windows_path(self, path):
        if not self.args.wine:
            return str(path)
        self.watchdog.publish(phase="winepath", pending={"path": str(path)})
        return subprocess.check_output(
            [self.args.winepath, "-w", str(path)], text=True,
            timeout=min(10, self.remaining()),
        ).strip()

    def remaining(self):
        seconds = self.deadline - time.monotonic()
        if seconds <= 0:
            raise TimeoutError("smoke suite wall-clock budget exhausted")
        return seconds

    def request(self, request, ok=True):
        if isinstance(request, str):
            wire, request_id = request, None
        else:
            self.sequence += 1
            request = {"id": self.sequence, **request}
            request_id = request["id"]
            wire = json.dumps(request, ensure_ascii=False)
        self.watchdog.publish(phase="request", pending=request)
        self.process.stdin.write(wire + "\n")
        self.process.stdin.flush()
        try:
            line = self.responses.get(timeout=min(self.args.timeout, self.remaining()))
        except queue.Empty as error:
            raise TimeoutError(f"no response to {wire}") from error
        if line is None:
            raise RuntimeError(f"oracle exited before responding to {wire}")
        if isinstance(line, Exception):
            raise line
        response = json.loads(line)
        self.last_response = response
        self.watchdog.publish(phase="response", pending=None, lastFullResponse=response)
        equal(response["id"], request_id)
        equal(response["ok"], ok)
        equal(response["schemaVersion"], 2)
        equal(response["referenceCommit"], BASELINE)
        if not isinstance(response["diagnostics"], list):
            raise AssertionError("diagnostics must be an array")
        return response.get("result") if ok else response["error"]

    def call(self, op, **fields):
        return self.request({"op": op, **fields})

    def fixture(self, name="fixture", overlays=()):
        destination = self.directory / name
        self.watchdog.publish(phase="copy_fixture", pending={"source": str(TESTS / "fixture"), "destination": str(destination)})
        shutil.copytree(TESTS / "fixture", destination)
        for overlay in overlays:
            self.watchdog.publish(phase="copy_fixture", pending={"source": str(TESTS / overlay), "destination": str(destination)})
            shutil.copytree(TESTS / overlay, destination, dirs_exist_ok=True)
        return destination

    def load(self, directory=None, **fields):
        directory = directory or self.fixture()
        result = self.call("load", gameDir=self.windows_path(directory), seed=123456, **fields)
        equal(result["termination"], "waitingInput")
        return result

    def value(self, source):
        return self.call("eval", source=source)["value"]

    def run(self, entry, **fields):
        result = self.call("run", entry=entry, **fields)
        equal(result["termination"], "completed")
        return result

    def kill(self):
        if os.name == "nt":
            try:
                subprocess.run(["taskkill", "/PID", str(self.process.pid), "/T", "/F"],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                               timeout=3, check=False)
            finally:
                self.process.kill()
        else:
            try:
                os.killpg(self.process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass

    def close(self):
        self.watchdog.publish(phase="closing", pending="oracle_process_exit")
        self.process.stdin.close()
        try:
            self.process.wait(timeout=3)
        except subprocess.TimeoutExpired:
            self.kill()
            self.process.wait(timeout=3)
        self.process.stdout.close()
        self.stderr.close()
        return self.process.returncode


def protocol(oracle):
    oracle.request("{", ok=False)
    oracle.request("[]", ok=False)
    oracle.request({"id": {"nested": [1, "中文"]}, "op": "unknown"}, ok=False)
    capability = oracle.call("capabilities")
    equal(capability["implementation"], "emuera_lazyloading_selfmodified_version")
    equal(len(capability["operations"]), 14)
    if not {"observe", "injectInput"}.issubset(capability["operations"]):
        raise AssertionError("headless observation/input operations are missing")
    equal(oracle.call("lex", source="1 + 2")["tokens"][0]["value"], 1)
    equal(oracle.call("lex", source="1.25")["tokens"][0]["value"], 1.25)
    for source, operand in [("1 + 2 * 3", "System.Int64"), ("1.25 + 2.5", "System.Double")]:
        equal(oracle.call("parseExpression", source=source)["operandType"], operand)
    error = oracle.request({"op": "parseLine", "source": "PRINTL hello"}, ok=False)
    equal(error["type"], "System.InvalidOperationException")
    oracle.request({"op": "execute", "statement": "RESULT = 1"}, ok=False)
    equal(oracle.call("reset"), {"reset": True})
    oracle.call("capabilities")


def csv(oracle):
    loaded = oracle.load()
    equal(oracle.call("parseLine", source="PRINTL hello")["functionCode"], "PRINTL")
    equal(loaded["randomSeed"], 123456)
    for source, expected in [
        ('VARSIZE("ABL")', 120), ('GETNUM(ABL, "later")', 2),
        ("ITEMPRICE:5", 120), ("STR:0", "initial text"),
        ("CSVABL(10, 2)", 5), ("GAMEBASE_GAMECODE", 42),
    ]:
        equal(oracle.value(source), expected)
    analysis = oracle.call("analyzeLine", source="RESULT = 9")
    if analysis["argument"] is None:
        raise AssertionError("semantic argument missing")
    functions = {item["name"]: item for item in oracle.call("analyzeProject")["functions"]}
    equal(functions["ORACLE_FLOAT_METHOD"]["returnType"], "System.Double")
    equal(functions["ORACLE_FLOAT_METHOD"]["isMethod"], True)


def runtime(oracle):
    oracle.load()
    equal(oracle.call("execute", statement="FLAG:10 = 9", watch=["FLAG:10"])["watches"], {"FLAG:10": 9})
    result = oracle.run("ORACLE_TEST")
    if "ORACLE_OK" not in "\n".join(result["output"]):
        raise AssertionError("isolated function output missing")
    result = oracle.run("ORACLE_ARGUMENTS", arguments='41, "中文"', watch=["RESULT", "RESULTS"])
    equal(result["watches"], {"RESULT": 42, "RESULTS": "中文"})
    result = oracle.run("ORACLE_EXTENSIONS", watch=["ORACLE_FLOAT", "ORACLE_SPARSE:999999", "RESULT:60"])
    equal(result["watches"], {"ORACLE_FLOAT": 3.75, "ORACLE_SPARSE:999999": 42, "RESULT:60": 0})
    equal(oracle.value("ORACLE_FLOAT_METHOD(1.25)"), 2.5)
    oracle.request({"op": "execute", "statement": "CALL ORACLE_TEST"}, ok=False)


def inputs(oracle):
    oracle.load(oracle.fixture(overlays=("fixture-oneinput",)))
    equal(oracle.call("run", entry="ORACLE_INPUT")["termination"], "waitingInput")
    resumed = oracle.call("run", inputs=["42"], watch=["RESULT"])
    equal(resumed["termination"], "completed")
    equal(resumed["watches"], {"RESULT": 42})
    result = oracle.run("ORACLE_ONEINPUT", uiInputs=[{"text": value} for value in ["12", "βx", "34", "yz"]],
                        watch=["RESULT:40", "RESULTS:40", "RESULT:41", "RESULTS:41"])
    equal(result["watches"], {"RESULT:40": 1, "RESULTS:40": "β", "RESULT:41": 3, "RESULTS:41": "y"})
    waiting = oracle.call("run", entry="ORACLE_NF")
    equal(waiting["termination"], "waitingInput")
    equal(waiting["state"], "WaitInputNoFocus")
    resumed = oracle.call("run", inputs=["NF_OK"], watch=["RESULTS:60"])
    equal(resumed["termination"], "completed")
    equal(resumed["watches"], {"RESULTS:60": "NF_OK"})
    equal(oracle.run("ORACLE_SEQUENCE", watch=["RESULT:61"])["watches"], {"RESULT:61": 17})


def reload(oracle):
    first = oracle.fixture("first", ("fixture-oneinput", "fixture-oneinput-long"))
    second = oracle.fixture("second", ("fixture-oneinput",))
    (second / "setting.json").write_text('{"UseNewRandom":true}', encoding="utf-8")
    ui = [{"text": "42", "changedByMouse": True}, {"text": "LONG", "changedByMouse": True}]
    for directory, algorithm, watches in [
        (first, "sfmt19937", {"RESULT:42": 42, "RESULTS:42": "LONG"}),
        (second, "dotnet", {"RESULT:42": 4, "RESULTS:42": "L"}),
        (first, "sfmt19937", {"RESULT:42": 42, "RESULTS:42": "LONG"}),
    ]:
        equal(oracle.load(directory)["randomAlgorithm"], algorithm)
        equal(oracle.run("ORACLE_ONEINPUT_MOUSE", uiInputs=ui, watch=list(watches))["watches"], watches)
    random_value = oracle.value("RAND:1000000")
    oracle.load(first)
    equal(oracle.value("RAND:1000000"), random_value)
    oracle.call("reset")
    oracle.request({"op": "eval", "source": "FLAG:10"}, ok=False)


def save(oracle):
    fixture = oracle.fixture(overlays=("fixture-save",))
    oracle.load(fixture)
    oracle.call("execute", statement="FLAG:10 = 123")
    oracle.call("execute", statement='SAVEDATA 0, "ORACLE_SAVE"')
    saved = fixture / "save00.sav"
    if not saved.is_file():
        raise AssertionError("save fixture was not written")
    external = oracle.directory / "external.sav"
    shutil.copy2(saved, external)
    contents = external.read_bytes()
    oracle.call("execute", statement="FLAG:10 = 456")
    result = oracle.call("loadSave", savePath=oracle.windows_path(external), watch=["FLAG:10", "FLAG:11", "FLAG:12"])
    equal(result["termination"], "waitingInput")
    equal(result["watches"], {"FLAG:10": 123, "FLAG:11": 111, "FLAG:12": 222})
    if "ORACLE_SAVE_RESTORED" not in "\n".join(result["output"]):
        raise AssertionError("save continuation did not reach EVENTLOAD")
    equal(external.read_bytes(), contents)


def limits(oracle):
    fixture = oracle.fixture()
    oracle.load(fixture)
    oracle.request({"op": "run", "entry": "ORACLE_TEST", "instructionLimit": 0}, ok=False)
    result = oracle.call("run", entry="ORACLE_LOOP", instructionLimit=100, timeoutMs=2000)
    equal(result["termination"], "instructionLimit")
    oracle.load(fixture)
    result = oracle.call("run", entry="ORACLE_LOOP", instructionLimit=1000000000, timeoutMs=1)
    equal(result["termination"], "timeout")
    oracle.load(fixture)
    equal(oracle.call("run", entry="ORACLE_ERROR")["termination"], "error")
    oracle.call("reset")
    oracle.call("capabilities")


def presentation(oracle):
    oracle.load()
    oracle.run("ORACLE_PRESENTATION")
    oracle.run("ORACLE_PRINT_FAMILY")
    oracle.run("ORACLE_LINECOUNT")
    oracle.run("ORACLE_HTML_POP")
    oracle.call("execute", statement="TOOLTIP_SETDELAY 0")
    equal(oracle.value('GETCONFIGS("描画インターフェース")'), "SKIASHARP")


CASES = {case.__name__: case for case in (protocol, csv, runtime, inputs, reload, save, limits, presentation)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, required=True)
    parser.add_argument("--wine", help="Wine executable; omit on Windows")
    parser.add_argument("--winepath", default="winepath")
    parser.add_argument("--case", action="append", choices=CASES, help="run only a directly affected case")
    parser.add_argument("--timeout", type=float, default=30, help="maximum seconds per response")
    parser.add_argument("--budget-seconds", type=float, default=300, help="total smoke wall-clock budget")
    args = parser.parse_args()
    args.exe = args.exe.resolve(strict=True)
    if not math.isfinite(args.timeout) or not math.isfinite(args.budget_seconds) or args.timeout <= 0 or args.budget_seconds <= 0:
        parser.error("timeouts must be positive")
    deadline = time.monotonic() + args.budget_seconds
    watchdog = SmokeWatchdog(deadline)
    failures = []
    for case in args.case or CASES:
        watchdog.publish(case=case, phase="case_setup", pending=None, lastFullResponse=None)
        if time.monotonic() >= deadline:
            failures.append(case)
            print(f"FAIL {case}: total budget exhausted; remaining cases not started", file=sys.stderr)
            break
        with tempfile.TemporaryDirectory(prefix="emuera-selfmodified-smoke-") as temporary:
            oracle = Oracle(args, Path(temporary), deadline, watchdog)
            try:
                CASES[case](oracle)
                print(f"PASS {case} ({oracle.sequence} requests)", flush=True)
            except Exception as error:
                failures.append(case)
                print(f"FAIL {case}: {error}\nlast response: {oracle.last_response}", file=sys.stderr, flush=True)
            finally:
                exit_code = oracle.close()
                if exit_code != 0 and case not in failures:
                    failures.append(case)
                    print(f"FAIL {case}: oracle exit code {exit_code}", file=sys.stderr)
                diagnostics = oracle.stderr_path.read_text(encoding="utf-8", errors="replace")
                if diagnostics.strip():
                    print(diagnostics, file=sys.stderr)
                watchdog.attach(None)
    watchdog.close()
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
