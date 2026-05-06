# Project Rules — emuera_lazyloading_selfmodified_version

## 知识库与技能架构

本项目使用三层知识管理架构，skills 和 knowledge 通过符号链接共享：

1. **Skill 层（路由）**：`.trae/skills/` → 符号链接到 `D:\emuera\shared-trae\skills\`
2. **Knowledge 层（知识库）**：`.trae/knowledge/` → 符号链接到 `D:\emuera\shared-trae\knowledge\`
3. **Meta 层（方法论）**：`knowledge-builder` skill — 如何积累知识

### 共享配置位置

```
D:\emuera\shared-trae\
├── skills\              ← 所有项目共享的技能
│   ├── erabasic\        ← ERABASIC 语法路由
│   ├── powershell-git\  ← 终端命令避坑
│   └── knowledge-builder\ ← 知识积累方法论
└── knowledge\           ← 所有项目共享的知识库
    └── erabasic\        ← ERABASIC 知识条目
```

### 知识积累规则

- 每次发现新洞见时，必须按照 knowledge-builder 方法论持久化
- 新知识写入 `D:\emuera\shared-trae\knowledge\<domain>\` 对应条目
- 新场景路由更新到对应 Skill 的路由表
- 新条目注册到 `_index.md`
- **修改一处即全局生效**（符号链接机制）

### 默认技能激活规则

> **本项目的所有工作流均以 ERABASIC 解释器实现和 ERB 脚本行为分析为核心。**
> **Agent 在本项目中工作时，应默认激活以下技能，无需用户显式 @ 调用。**

| 技能 | 激活条件 | 说明 |
|------|---------|------|
| **erabasic** | 任何涉及 .cs 文件中 ERABASIC 解析/运行时逻辑的分析，或涉及 .erb/.erh/.csv 文件的操作 | 本项目研究 ERABASIC 源码实现或 ERB 脚本行为，erabasic 是默认技能 |
| **powershell-git** | 任何涉及终端命令、Git 操作、文件 IO 的操作 | 终端命令避坑，Git 操作规范 |
| **knowledge-builder** | 发现新洞见、需要持久化知识时 | 元方法论，指导知识积累流程 |

### 违反后果

如果 Agent 在分析 ERABASIC 解析/运行时逻辑或涉及 .erb/.erh/.csv 文件的操作中未激活 `erabasic` 技能，则违反了本规则。`erabasic` 技能包含 ERABASIC 语法路由和知识库，是理解引擎源码、解读脚本报错的基础。

## 项目特异配置

- 解决方案：`emuera_lazyloading_selfmodified_version.sln`
- 构建命令：`dotnet build 2>&1`
- 测试命令：`dotnet test 2>&1`
- 项目类型：.NET C# 解决方案（Emuera 原版派生的懒加载修改版）

## ERB 脚本编写规则

> 当需要编写 ERB/ERH 测试脚本时，必须先查阅 erabasic 语法知识库：[syntax-quickref.md](file:///d:/emuera/shared-trae/knowledge/erabasic/syntax-quickref.md)

- `#DIM` 是预处理指令，`#` 不可省略（不能写成 `DIM`）
- 字符串字面量需用 `""` 包裹（如 `"pet_1"`），否则会被当作变量名
- 数组可用内联初始化：`#DIM ARR, 20 = 1, 2, 3, ...`
- A-Z 单字母变量是引擎保留变量，不可用于 `#DIM`

### API 确认流程（强制）

> 调用任何 ERB 指令/函数前，必须确认 API 签名：

1. 先查知识库是否有该指令的 API 文档
2. 若无，查源码 `DoInstruction` / `ArgumentBuilder` 确认参数个数和类型
3. **禁止凭猜测或类比编写参数**（如 CBGSETIMAGE 仅 1 个 STR 参数，不支持颜色矩阵；HTML img 无 cm 属性）
