# 移植工作手册：SQL 泛型重构

> **目标**：emuera_lazyloading_selfmodified_version `SqlManager.cs`  
> **日期**：2026-05-09  
> **状态**：⏳ 待处理  
> **优先级**：🟡 P1

---

## DotNet 实现

DotNet 引入 `ExecuteScaler<T>` 内部泛型方法，合并 Long/String/Float 三类标量查询代码：

```csharp
// 合并前：3 个独立方法
SQL_EXECUTE_SCALER_LONG → ExecuteScaler<long>
SQL_EXECUTE_SCALER_STRING → ExecuteScaler<string>
// 合并后：1 个泛型方法 + 3 个 ERB 入口
```

## LazyLoading 现状

LazyLoading 已有独立的 Long/String 标量查询方法，无 Float 标量查询。

## 移植方案

1. 在 SqlManager.cs 中引入内部泛型方法 `ExecuteScalar<T>`
2. 将现有 Long/String 标量查询重构为泛型调用
3. 可选：新增 Float 标量查询支持
4. ERB 层 API 不变，仅内部实现重构

## 风险评估

- **中风险**：涉及 SQL 核心路径重构，需回归测试所有 SQL 函数
- **注意**：LazyLoading 的 SQL 实现与 DotNet 差异较大（24 vs 9 函数），不宜直接复制代码

## 涉及文件

- `Emuera/Runtime/Script/SqlManager.cs`
