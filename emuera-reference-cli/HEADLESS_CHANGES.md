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
