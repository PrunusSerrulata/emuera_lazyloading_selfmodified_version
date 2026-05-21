---
alwaysApply: true
---

# 默认技能激活规则

本项目的所有工作流均以 ERABASIC 解释器实现和 ERB 脚本行为分析为核心。Agent 在本项目中工作时，应默认激活以下技能，无需用户显式 @ 调用。

| 技能 | 激活条件 | 说明 |
|------|---------|------|
| **erabasic** | 涉及 .cs 文件中 ERABASIC 解析/运行时逻辑的分析，或涉及 .erb/.erh/.csv 文件的操作 | 本项目研究 ERABASIC 源码实现或 ERB 脚本行为，erabasic 是默认技能 |
| **powershell-git** | 涉及终端命令、Git 操作、文件 IO 的操作 | 终端命令避坑，Git 操作规范 |
| **knowledge-builder** | 发现新洞见、需要持久化知识时 | 元方法论，指导知识积累流程 |

## 违反后果

如果 Agent 在分析 ERABASIC 解析/运行时逻辑或涉及 .erb/.erh/.csv 文件的操作中未激活 `erabasic` 技能，则违反了本规则。`erabasic` 技能包含 ERABASIC 语法路由和知识库，是理解引擎源码、解读脚本报错的基础。
