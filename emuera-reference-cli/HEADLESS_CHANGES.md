# 自改版 CLI hook 审计

日期：2026-08-27。上游语义基准：`fc4fb21416768c17256d0e82f997e5f99c9bba91`。
协议层和公共 fixture 移植自兄弟 `emuera.em/emuera-reference-cli`；不修改兄弟参考树。

## 不变量

- 只有 friend assembly `Emuera.ReferenceCli` 配置 headless；普通 `Program.Main`
  保持原 GUI/backend 链路，不更换语言、存档、随机数或稀疏数组实现。
- 不构造隐藏 `MainWindow` 或 WinForms Timer。隐藏窗口仍会触发原生初始化，无法作为
  无窗口测试适配；窗口副作用需在调用点隔离，而不是改写 parser/VM。
- 只有 oracle 的独立函数 root 与执行限额改变 headless 控制边界。普通模式下相关
  检查立即返回，原系统状态机和 `BEFORE_ERROR` 行为不变。
- CLI 输出表示本自改版真实结果；不会为匹配固定参考树或 Rust 而修改后端语义。

## 引擎改动逐文件清单

| 文件 | 必要性、隔离方式与正常游戏影响 |
| --- | --- |
| `Emuera/Emuera.csproj` | 开启跨平台 Windows targeting、授予 friend assembly internal 访问权；原目标框架、依赖版本和 GUI 入口不变。 |
| `Emuera/Program.Headless.cs` | CLI 专用目录/debug/seed/限额初始化；普通入口不调用。 |
| `Emuera/Runtime/Config/ConfigData.cs` | 静态配置路径和 Fixed 标记不能跨 CLI 项目复用；headless 路径动态取当前项目并新建配置实例。正常模式仍使用首次捕获的原路径和原实例。 |
| `Emuera/Runtime/Config/JSON/JSONConfig.cs` | gated helper 按当前游戏路径读取 setting.json，缺失时使用默认配置；普通 Load/Save 不变。 |
| `Emuera/Runtime/Script/Data/ParserMediator.Headless.cs` | 在原 warning lock 内投影并清空诊断，只由 CLI 调用。 |
| `Emuera/Runtime/Script/Process.Headless.cs` | gated seed、传统存档路径、独立指令/函数入口、计数和限额；全部复用原 parser、argument builder、dispatch、RNG 和存档逻辑。 |
| `Emuera/Runtime/Script/Process.ScriptProc.cs` | 原 dispatch 边界插入限额检查；非 headless 为空操作。 |
| `Emuera/Runtime/Script/Process.cs` | 独立 root 结束时不再进入标题状态机；预算耗尽时终止 headless run，防止 BEFORE_ERROR 越过测试预算；普通模式行为不变。 |
| `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs` | seed helper 调用原 Randomize；槽位读取与 headless 任意只读路径共享未改变的存档解码主体，正常槽位/LastLoadNo 语义不变。 |
| `Emuera/Runtime/Script/Statements/Instraction.Child.cs` | 六处 INPUT-family textbox 调用经 adapter；普通模式仍调用原 window 方法。 |
| `Emuera/UI/Dialog.cs` | headless 不显示 modal，询问保守返回 No；普通 WinForms 对话框不变。 |
| `Emuera/UI/Game/EmueraConsole.Headless.cs` | gated 无窗口 constructor、state/process/input accessor 和恢复入口；不创建原生 Timer。 |
| `Emuera/UI/Game/EmueraConsole.cs` | headless 跳过重绘、滚动、debug/标题/tooltip 控件副作用；保留原输入请求、NF 等待状态及 SEQUENCEINPUT 宏处理。初始化前播种并应用 CLI 预算，独立函数完成单独标识。 |
| `Emuera/UI/Game/EmueraConsole.Print.cs` | 保留原 display buffer，headless 仅跳过窗口刷新、背景控件、rich text 清理及日志通知的窗口访问。 |

## Wrapper 与 fixture

`emuera-reference-cli` 下的 Program、OracleService、ReferenceHost、JsonProjection 与项目
文件构成独立 CLI；浮点值使用真实 `GetFloatValue`，方法返回类型由自改版 `EraType`
投影；NF 等待可继续输入。reset 清理旧 console/global/diagnostic/rename 状态。
显式 UTF-8 标准流避免 .NET 8 在 Wine 重定向管道上设置控制台 code page 时抛出
`IOException: Invalid access`；不需要隐藏窗口或修改引擎。
JSON options 显式指定 .NET 8 所需的 `DefaultJsonTypeInfoResolver`，避免复杂快照的
JsonNode 序列化使进程提前退出。`parseLine` 明确声明须先 load 本版本的项目字典。
读档后直接恢复 VM，不向旧标题的 InputRequest 提交空字符串，避免变量虽然已恢复、
`SYSTEM_LOADEND`/`EVENTLOAD` 却没有执行；`fixture-save` 对两个阶段分别断言。

`tests/fixture*/csv` 与公共 ERB 来源于参考 CLI；未复制其缓存或运行产物。
`selfmodified.erb/.erh`、明确配置和 Python 分组驱动为本版本新增。脚本复制到系统临时
目录后运行，生成文件均随副本清理；Windows/Wine 共用同一请求/断言集合。

## 验证与边界

先执行 .NET 构建/发布、Python 语法、shell 语法与 `git diff --check`，再执行平台
smoke。全量仅一次，修复后按 `--case` 定向复验；具体本次结果见任务交付。

2026-08-27 本次验证记录：

- .NET 8 Windows x64 `Debug-NAudio` 构建通过（0 errors，920 warnings）；自包含
  publish 通过。未修改原有依赖版本，保留 NU1701 等现有构建告警。
- Python AST、`bash -n`、`git diff --check` 通过。
- 首次完整 8 组均在 `Console.InputEncoding` 启动处失败，未进入各组实际断言。
- 之后只按命名组定向复验：`protocol`、`csv`、`runtime`、`inputs`、`reload`、
  `save`、`limits`、`presentation` 均通过；没有第二次启动完整套件。
- 定向过程发现并修复了 .NET 8 JsonNode resolver、未加载的行解析前置条件和旧输入
  阻挡读档 continuation。自改版除零只告警，错误 fixture 改用原生 `THROW`，
  没有修改除零语义。加强后的 `save` 组再次单独通过。
- 验证环境为 macOS/Wine，使用独立 `emuera-selfmodified-cli` prefix。Windows
  PowerShell 入口已提供但未在 Windows 执行；未运行实际 GUI 或 Rust 差分。

不以无窗口测试宣称 WinForms/Skia 实际画面、GPU、剪贴板、真实鼠标键盘、原生定时器
超时或音频通过。固定 fixture 不访问用户游戏。没有修改 RustyEra 组件，因此本次
不运行 Rust/C# 兼容性差分，也不把自改版结果解释为 Rust 实现能力。

### 2026-08-27：批次 0 显式授权的布局观察与输入 trace

用户明确授权扩展两个 oracle 的 headless 观察/输入入口；正常游戏语义保持不变。语义基准 SHA 不变，wrapper revision 另记。

| 文件 | 目的与隔离 |
| --- | --- |
| `Emuera/Runtime/Utils/HeadlessInput.cs` | 新增 opt-in headless active、设备原语及事件泵接入；所有操作检查 HeadlessMode，正常游戏不调用。 |
| `Emuera/Runtime/Utils/WinInput.cs` | 新增 gated 非消费 latch 观察；按键和 latch 算法不变。 |
| `Emuera/Runtime/Script/Statements/Function/Creator.Headless.cs` | 仅 headless 清理/只读复制静态 keytoggle，防止多个 fixture 相互污染；未改 GETKEY evaluator。 |
| `Emuera/UI/Game/EmueraConsole.cs` | IsActive 只在显式 headless 输入模式读注入 active；AWAIT 原事件泵位置接 headless 队列，正常路径保持原调用。 ClearLatches 顺序不变。 |
| `Emuera/UI/Game/EmueraConsole.Headless.cs` | 增加 pending display 的只读观察，不隐式 flush。 |
| `Emuera/UI/Game/ConsoleStyledString.cs` | 仅 headless 返回已有字体 fallback runs 的只读副本，不进行字体选择或测量。 |
| `emuera-reference-cli/HeadlessInputTrace.cs` | 完整校验后投递设备事件；beforeRun 与每次 AWAIT 泵分离，非消费观察和 reset。 |
| `emuera-reference-cli/PresentationProjection.cs` | 投影已有布局对象；验证字体 hash/family，不重测量、重排版或创建窗口。 |
| `emuera-reference-cli/ReferenceHost.cs` | 接入可选布局观察、输入 trace、纯 observe 和 reset；保留旧 output。 |
| `emuera-reference-cli/OracleService.cs` | schema 2 上 additive capability 版本和操作；observe 不清空诊断队列。 |
| `emuera-reference-cli/README.md` | 记录 schema、注入时点、字体门禁和正常路径隔离边界。 |

固定自有 fixture 和 Python driver 位于蛇版专用 core worktree 的 `tools/runtime-tester/fixture-snake-compatibility/` 与 `tools/snake-compatibility-oracle/`；本参考仓库不在运行时修改游戏。

验证状态：代码已编写，尚未运行 build/check/test；等待批次 0 统一唯一重构审查与静态门禁。不得将本条记录视为正常工程编译、oracle smoke、布局/按键差分或 Rust 兼容通过。首次全量、修复后定向复验及最终 wrapper commit 由批次实施记录另行登记。
# Batch 0 follow-up: isolated smoke build inputs (2026-08-27)

| File | Headless-only change | Normal behavior |
|---|---|---|
| `emuera-reference-cli/tests/test-macos-wine.sh` | Optional `EMUERA_SNAKE_PUBLISH_DIR` and `EMUERA_SNAKE_ARTIFACTS_PATH` isolate publish/intermediate output; `EMUERA_SNAKE_SKIP_BUILD=1` requires an existing CLI from the completed static build gate. Caller still supplies its isolated `WINEPREFIX`. | Unset variables preserve the prior restore/publish/default paths; no engine semantics change. |

The edit has not been executed, checked, or built. It joins the single batch review and subsequent test budget.

### 批次 0 唯一审查落实（尚待验证）

- `HeadlessInputTrace.cs`：把完整 JSON 解析与输入状态应用拆开；`ReferenceHost.cs` 在
  execute/run/injectInput 的参数、watch、uiInputs、observePresentation 全部有效后才应用
  trace。有效请求执行后的脚本错误仍保留真实状态；不回滚正常引擎行为。
- `PresentationProjection.cs`：明确 suppliedFontFileSha256 仅验证传入文件，不证明系统实际
  选择了该字节来源；family/fallback 是另列的实际观察，fontByteSource 保持未验证。
- `README.md`：补充上述证据边界。唯一独立审查已完成；尚无批次 0 build/test 通过结果。
- `Emuera/UI/Game/ConsoleStyledString.cs`：只读报告 SetWidth 实际缓存分支所用 provider、
  version、family、size；TEXTRENDERER 只有 GDI/raster 缓存存在时报告 GDI，否则报告 Skia。
  不调用测量、字体选择或重排版；入口检查 HeadlessMode。
- `Emuera/Runtime/Utils/HeadlessInput.cs`：删除未消费的 states 数组；保留实际 WinInput
  press/release/latch 行为，不实现新的 toggle 语义。
- `tests/smoke.py`：独立 5 秒完整状态看门狗覆盖跨 case 请求，并按新增 observe/injectInput
  操作将 capability 数量更新为 14；原 12 项断言保持。
- `tests/test_supervision.py`：补充相同状态、持续变化和阻塞进程监督回归。

### 批次 0 当前验收更新（2026-08-27）

本节更新以上“尚待验证”的历史状态：唯一独立审查及全部要求已在首条测试前落实。
正常 Emuera 工程和独立 reference CLI 构建/发布通过，正常工程 917 个既有警告、0 错误。
本批首次完整 macOS/Wine smoke 通过；未再次执行完整 smoke。实际 primitive input 的
活动状态、AWAIT 事件泵、无消费观察、无效请求原子拒绝与 reset 隔离已完成定向观察。
PRINTC 记录现有布局/实际 provider、family/fallback；传入字体 hash 不证明已安装字节来源，
仍报告 `unverified-installed-source`，不宣称 GUI/GPU 或跨客户端像素等价。

用户取消本次测试总时限；仍保持静态先行、单次全量和五秒完整状态监督。使用专用
worktree 组内 Wine prefix、已有工具和隔离游戏副本，不下载 Chromium。
具体命令、首次结果与定向修复、Rust/各 oracle 的逐例比较及最终 wrapper SHA 统一记录于
专用 core 的 `docs/snake-compatibility/SNAKE_EMUERA_IMPLEMENTATION_LOG.md`。
语义基准未更新；分项 wrapper 提交仅整理已验证的集成源码，未改正常引擎算法。

### 批次 2E：无窗口动画计时器状态（尚待验证）

| 文件 | 目的与隔离 |
| --- | --- |
| `Emuera/UI/Game/EmueraConsole.Headless.cs` | 增加仅由显式 headless 实例持有的逻辑动画计时器值；不创建 WinForms Timer。 |
| `Emuera/UI/Game/EmueraConsole.cs` | headless 的设置与查询复用正常路径的停用和最小 10ms 规范化规则；非 headless 继续读写原 `redrawTimer`。 |
| `emuera-reference-cli/PresentationProjection.cs` | 只读投影逻辑动画计时器、全局文本背景和已有行的 styled-text 资格，供逐例 oracle 比较；不绘制或改写展示状态。 |

该接线只修复 reference CLI 先前跳过设置且随后解引用未创建 Timer 的问题，不改变 parser、
指令/方法执行或正常游戏计时器语义。代码尚未进入批次 2E 的唯一重构审查及静态/动态门禁。
