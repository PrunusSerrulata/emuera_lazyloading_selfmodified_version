# 自改版 Emuera 测试 CLI

以本仓库 `fc4fb21416768c17256d0e82f997e5f99c9bba91` 为语义基准，直接调用真实
lexer、parser、项目加载器和 VM。接口沿用兄弟 `emuera.em/emuera-reference-cli` 的
schema 2 持久 NDJSON 协议，但响应明确标识本自改版，不冒充固定参考实现。

只用于测试，不是游戏启动器。普通 GUI 入口不启用 headless 模式；所有源码 hook 和
隔离边界见 [HEADLESS_CHANGES.md](HEADLESS_CHANGES.md)。

## 构建与使用

需要支持 .NET 8 的 SDK、Windows x64；macOS 通过 Wine 运行。CLI 保持 .NET 8
目标框架，不将自改版升级到参考树的 .NET 10。构建配置统一使用 `Debug-NAudio`。

```sh
dotnet build emuera-reference-cli/Emuera.ReferenceCli.csproj -c Debug-NAudio -p:Platform=x64 -p:RuntimeIdentifiers=win-x64 -r win-x64
```

每行一个 UTF-8 JSON object，stdout 每行一个响应。保持进程存活可复用项目、变量和
等待状态；诊断来自本版本。`referenceCommit` 为上游语义基准，非当前 wrapper commit。

```json
{"id":1,"op":"capabilities"}
{"id":2,"op":"lex","source":"1.25 + 2"}
{"id":3,"op":"load","gameDir":"C:\\test-game","seed":123456}
{"id":4,"op":"run","entry":"ORACLE_EXTENSIONS","watch":["ORACLE_FLOAT","ORACLE_SPARSE:999999"]}
{"id":5,"op":"reset"}
```

| 操作 | 请求与用途 |
| --- | --- |
| `capabilities` / `reset` | 查询操作、扩展能力与版本；释放当前项目 |
| `lex` | `source`，可选 `endWith`、`flags` |
| `parseExpression` / `parseLine` | `source`；后者可选 `reduceArguments` |
| `analyzeLine` / `analyzeProject` | 已加载项目中的语义分析/函数与行投影 |
| `load` | `gameDir`，可选 `debug`、`seed`、`watch`、执行限制 |
| `loadSave` | `savePath`，沿原存档恢复链路运行，不改写输入存档 |
| `eval` | `source`，返回当前表达式值 |
| `execute` | `statement`，执行单条非控制流指令 |
| `run` | 可选 `entry`、原 CALL 语法的 `arguments`、`inputs`、`uiInputs` |

本版行解析依赖项目指令字典，因此 `parseLine` 也必须先 `load`；未加载时返回明确的
操作错误。完整清单见 `capabilities.requiresLoad`。独立 `parseExpression` 可解析常量
表达式，引用项目变量/函数时仍需已加载上下文。

`load`/`loadSave`/`execute`/`run` 支持 `watch` 表达式数组及正数 `instructionLimit`
（默认 1000 万）、`timeoutMs`（默认 20000）。执行限制覆盖 VM dispatch，不代表
所有文件 I/O 或原生调用可被中断；测试驱动还必须设置进程级响应超时。

`run` 无 `entry` 时继续等待中的输入。`uiInputs` 为 `{text, changedByMouse}` 数组，
复现 ONEINPUT 文本截断；普通 `inputs` 直接交给原 VM。`output` 是当前完整 display
buffer，不是增量。`termination` 可为 `completed`、`waitingInput`、`instructionLimit`、
`timeout`、`quit`、`error`。NF 等待保留 `state=WaitInputNoFocus`，同时归类为
`termination=waitingInput`。

浮点 token、`eval` 和 `watch` 返回 JSON number；非有限浮点数返回字符串
`NaN`/`Infinity`/`-Infinity`。AST reflection graph 与参考 CLI 一样将数值字段转为
字符串，并使用 `$id`、`$ref`、`$truncated` 表示引用和深度限制。

每次 `load` 独立读取本项目配置，包括 `setting.json`；seed 在标题执行前应用。
存档/RNG/延迟加载语义仍由自改版实现决定，不应假定与 `emuera.em` 相同。

## 固定 fixture 与冒烟测试

测试只加载临时副本，结束时删除副本、存档和日志。不要直接加载 `tests/fixture`，
因为引擎可能写配置或存档。测试不依赖兄弟 RustyEra 仓库或本机游戏。

- `fixture`：移植参考 CSV 与 ERB 用例；另增 `selfmodified.erb/.erh`，覆盖浮点、
  稀疏数组、函数参数、NF 输入、SEQUENCEINPUT、错误和执行限额。
- `fixture-oneinput` / `fixture-oneinput-long`：普通与长鼠标输入覆盖层。
- `fixture-save`：证明存档变量恢复后继续进入 `SYSTEM_LOADEND`/`EVENTLOAD` 的覆盖层。
- `fixture-system`：参考系统训练链路覆盖层，供后续定向差分使用。
- `smoke.py`：Python 3.9+ 标准库驱动，逐请求超时、总预算和持久进程协议校验。

Windows：

```powershell
emuera-reference-cli/tests/protocol-smoke.ps1
```

macOS/Wine：

```sh
bash emuera-reference-cli/tests/test-macos-wine.sh
```

两个入口先还原、构建并检查脚本语法/补丁空白，再运行相同的 8 组用例：`protocol`、
`csv`、`runtime`、`inputs`、`reload`、`save`、`limits`、`presentation`。原参考树中
更多 fixture 入口仍可手动调用，但不把未执行的入口算作已验证。

同一任务全量只启动一次；失败后构建修复并直接调用已发布 binary，仅复验相关组：

```sh
WINEPREFIX=/absolute/path/to/prefix WINEDEBUG=-all python3 emuera-reference-cli/tests/smoke.py --wine wine --exe emuera-reference-cli/bin/smoke-win-x64/Emuera.ReferenceCli.exe --case inputs --budget-seconds 120
```

默认独立 Wine prefix 为工作区 `.wine-prefix/emuera-selfmodified-cli`；也可显式提供
`WINEPREFIX`。任何测试成功只说明该版本 oracle 的相应用例可用，不能替代 Rust/C#
同输入差分，更不代表 Skia/WinForms 实际画面或原生窗口交互通过验证。

## Batch-0 observation extensions (schema 2)

The optional `observationVersions` capability advertises `presentationSnapshot: 1`
and `headlessInputTrace: 1`. Existing requests, `output` and semantic
`referenceCommit` retain their meaning. The wrapper revision must be recorded
separately; adding these hooks does not advance the semantic baseline.

`load` accepts optional `presentationFont: {family, file, sha256}`. The wrapper
verifies the file bytes and rejects a measured text family that does not match.
`observePresentation: true` on load/run/execute requests returns the existing
line/button/node layout, including pixel-space nodes, font and provider metadata.
It never flushes or measures; use an explicit script `PRINTL` before observing
complete lines. `observe` is read-only, ignores watch expressions, does not drain
warnings and does not consume keys/latches. It accepts `observePresentation`
(default true). This is measured layout, not a raster/GPU/GUI compatibility claim.

`run`, `execute`, and the new `injectInput` operation accept:

```json
{"inputTrace":{"active":true,"beforeRun":[{"keyCode":65,"down":true,"toggle":false}],"awaitPumps":[[{"keyCode":65,"down":false,"toggle":false}]]}}
```

The complete trace is validated before any input mutation (key codes 0..255,
at most 4096 events and 256 queued pumps per request). `beforeRun` is applied
before the requested entry or pending input is resumed. Each original AWAIT event
pump consumes one `awaitPumps` batch, preserving event order; `[down, up]` in one
batch represents a click entirely inside that pump. Omitting `awaitPumps` leaves
the queue intact; supplying it replaces the queue. Active defaults to its current
injected value, initially false. Use AWAIT 0 for deterministic tests.

Original Emuera reads the explicit held/toggle sample through a gated Win32
primitive adapter. Snake calls its real SetKeyPressed/SetKeyReleased functions;
toggle is an original-device sampling field and does not invent a snake toggle
or latch. Its original ClearLatches-before-pump ordering remains intact. Tests
therefore exercise the real evaluators and snake latch, not Windows event delivery.
Without inputTrace the old inactive headless behavior is unchanged. Reset/load
clears held state, latches, evaluator toggles, active state, queues and pump hooks.

Responses after enabling input trace include non-consuming `primitiveInput`
state for every key plus event/pump counters and remaining queue length. A normal
watch expression can still have its ordinary script side effects; do not use
GETKEYTRIGGERED/RAND watches as passive observers.

Font evidence distinguishes `suppliedFontFileSha256` (the provided file bytes) from
observed font families. `fontByteSource=unverified-installed-source` means the
projection does not prove which installed file supplied those bytes. The snake
projection reports each node's cached measurement provider and aggregate
`providerVersions`; TEXTRENDERER may still select Skia for non-raster fonts.
Malformed execute/run/injectInput parameters are rejected before applying their
input trace. Valid requests retain normal script side effects, including errors.
