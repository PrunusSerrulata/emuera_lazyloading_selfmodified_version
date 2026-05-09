# 移植工作手册：SQL_CONNECTION_OPEN 便利函数

> **目标**：emuera_lazyloading_selfmodified_version `SqlManager.cs` + `Creator.Method.cs` + `Creator.cs`  
> **日期**：2026-05-09  
> **状态**：⏳ 待处理  
> **优先级**：🟡 P1

---

## DotNet 实现

DotNet 新增 `SQL_CONNECTION_OPEN` 指令，自动定位 `sav/sql/` 目录并建立 SQLite 连接，简化 ERB 脚本中的数据库操作。

LazyLoading 已有 24 个 SQL 函数（含参数化查询和 XML 支持），但缺少自动路径映射的便捷连接函数。

## 移植方案

1. 在 SqlManager.cs 中新增 `SQL_CONNECTION_OPEN` 方法
2. 自动检测 sav/sql/ 目录路径
3. 在 Creator.Method.cs 中新增 FunctionMethod
4. 在 Creator.cs 中注册

## 风险评估

- **低风险**：纯新增函数，不影响现有 SQL API

## 涉及文件

- `Emuera/Runtime/Script/SqlManager.cs`
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs`
- `Emuera/Runtime/Script/Statements/Function/Creator.cs`
