# 知识库与技能架构

本项目使用三层知识管理架构，skills 和 knowledge 通过符号链接共享：

1. **Skill 层（路由）**：`.trae/skills/` → 符号链接到 `D:\emuera\shared-trae\skills\`
2. **Knowledge 层（知识库）**：`.trae/knowledge/` → 符号链接到 `D:\emuera\shared-trae\knowledge\`
3. **Meta 层（方法论）**：`knowledge-builder` skill — 如何积累知识

## 共享配置位置

```
D:\emuera\shared-trae\
├── skills\
│   ├── erabasic\        ← ERABASIC 语法路由
│   ├── powershell-git\  ← 终端命令避坑
│   └── knowledge-builder\ ← 知识积累方法论
└── knowledge\
    └── erabasic\        ← ERABASIC 知识条目
```

## 知识积累规则

- 每次发现新洞见时，必须按照 knowledge-builder 方法论持久化
- 新知识写入 `D:\emuera\shared-trae\knowledge\<domain>\` 对应条目
- 新场景路由更新到对应 Skill 的路由表
- 新条目注册到 `_index.md`
- **修改一处即全局生效**（符号链接机制）

## 会话结束强制检查规则

每次对话结束前（或用户说"记住"/"更新知识库"/"检查同步"时），必须执行以下检查流程：

```
1. 回顾本次对话涉及的主题和源码文件
2. 对比 erabasic skill 路由表：是否有新场景未注册？
3. 对比 _index.md：是否有新知识条目未索引？是否有已存在的文件被误标为"待创建"？
4. 对比各知识条目：是否有新洞见未写入？
5. 如有缺口，按 knowledge-builder 流程补全
```

| 检查项 | 方法 |
|--------|------|
| 路由覆盖 | 本次对话分析的每个功能/场景，是否在 skill 路由表中有对应行？ |
| 索引同步 | `_index.md` 的条目列表是否与 `knowledge/erabasic/` 目录中的实际文件一致？ |
| 内容时效 | 本次对话修改的源码逻辑，是否已反映到对应知识条目中？ |
| 日期更新 | 被修改的知识条目和 skill 文件的"最后更新"日期是否已更新？ |

**违反后果**：如果对话结束时未执行此检查，导致新认知丢失，则下次对话将从零开始。
