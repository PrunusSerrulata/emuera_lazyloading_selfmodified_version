"""Pure supervision comparisons; no CLI, Wine, or game is launched."""

import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from smoke import same_observation


TESTS = Path(__file__).resolve().parent


def write_stub(directory, name):
    executable = directory / name
    executable.write_text("#!/usr/bin/env bash\nexit 0\n", encoding="utf-8")
    executable.chmod(0o755)


class SmokeObservationTests(unittest.TestCase):
    def test_transport_id_is_not_progress_but_script_id_is(self):
        first = {"pending": {"id": 1, "op": "run"}, "lastFullResponse": {"id": 1, "result": {"watches": {"id": 7}}}}
        second = {"pending": {"id": 2, "op": "run"}, "lastFullResponse": {"id": 2, "result": {"watches": {"id": 7}}}}
        self.assertTrue(same_observation(first, second))
        self.assertEqual(second["lastFullResponse"]["id"], 2, "raw snapshots must remain unchanged")
        second["lastFullResponse"]["result"]["watches"]["id"] = 8
        self.assertFalse(same_observation(first, second))

    def test_full_state_equality_detects_stall(self):
        state = {"phase": "request", "pending": {"op": "run"}, "lastFullResponse": {"result": {"output": ["a"]}}, "process": {"returncode": None}}
        self.assertFalse(same_observation(None, state))
        self.assertTrue(same_observation(state, dict(state)))
        changed = {**state, "lastFullResponse": {"result": {"output": ["b"]}}}
        self.assertFalse(same_observation(state, changed))

    def test_macos_entrypoint_accepts_an_empty_artifacts_path(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            binaries = root / "bin"
            binaries.mkdir()
            for name in ("dotnet", "git", "python3", "wine", "wineboot", "winepath", "wineserver"):
                write_stub(binaries, name)

            (binaries / "python3").write_text(
                '#!/bin/bash\n'
                'echo "selected:$WINEPREFIX:$DOTNET_SYSTEM_GLOBALIZATION_USENLS"\n',
                encoding="utf-8",
            )
            publish = root / "publish"
            publish.mkdir()
            (publish / "Emuera.ReferenceCli.exe").touch()
            prefix = root / "prefix"
            prefix.mkdir()
            (prefix / "system.reg").touch()

            environment = os.environ.copy()
            environment.update(
                PATH=f"{binaries}{os.pathsep}{environment['PATH']}",
                EMUERA_WINE_BIN=str(binaries),
                EMUERA_SNAKE_ARTIFACTS_PATH="",
                EMUERA_SNAKE_PUBLISH_DIR=str(publish),
                EMUERA_SNAKE_SKIP_BUILD="0",
                WINEPREFIX=str(prefix),
            )
            environment.pop("DOTNET_SYSTEM_GLOBALIZATION_USENLS", None)
            result = subprocess.run(
                ["/bin/bash", str(TESTS / "test-macos-wine.sh"), "--case", "protocol"],
                cwd=TESTS.parent.parent,
                env=environment,
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(f"selected:{prefix}:1", result.stdout)


if __name__ == "__main__":
    unittest.main()
