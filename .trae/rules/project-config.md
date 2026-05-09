# 项目特异配置

- 解决方案：`Emuera.sln`
- 构建命令：`dotnet build "D:\emuera\emuera_lazyloading_selfmodified_version\Emuera.sln" 2>&1`
- 测试命令：`dotnet test 2>&1`
- 项目类型：.NET C# 解决方案（Emuera 原版派生的懒加载修改版）

## 会话结束规则

> **Agent 在结束 LazyLoading 工作区对话前，必须执行以下操作：**

1. **更新 CHANGELOG.md**：将本次对话中实现的功能/修复/变更添加到 `CHANGELOG.md` 顶部对应版本条目中
2. **更新移植评估手册**：如涉及 DotNet 移植任务，更新 `docs/comparison/emueradotnet-portability-assessment.md` 的进度和待办清单
3. **知识库同步**：按 knowledge-builder 方法论，将新洞见持久化到共享知识库
