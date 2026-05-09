# 移植执行手册：TOINT try-catch 边界修复

> **源**：EmueraDotNet `Creator.Method.cs` `ToIntMethod.GetIntValue()`  
> **目标**：emuera_lazyloading_selfmodified_version `Creator.Method.cs` `ToIntMethod.GetIntValue()`  
> **日期**：2026-05-09  
> **状态**：✅ 已完成

---

## 一、概述

修复 `TOINT` 函数在遇到非法输入（如 `"0e"`、`"-1-"`）时抛出 `CodeEE` 导致脚本崩溃的 bug。

**根本原因**：`LexicalAnalyzer.ReadInt64()` 在遇到无法解析的字符时会抛出异常，而 LazyLoading 的 `TOINT` 没有 `try-catch` 包裹。

---

## 二、涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| [Creator.Method.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs) | 修改 | `ToIntMethod.GetIntValue()` 加 try-catch |

仅需修改 **1 个文件，~5 行**。

---

## 三、前置差异分析

### 3.1 当前 LazyLoading 代码（有 bug）

[Creator.Method.cs:L5912](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs)：

```csharp
long ret = LexicalAnalyzer.ReadInt64(st, true);
```

无 `try-catch`，非法输入直接抛异常。

### 3.2 DotNet 修复后代码

[Creator.Method.cs:L2378-L2384](file:///d:/emuera/EmueraDotNet/Runtime/Script/Statements/Function/Creator.Method.cs)：

```csharp
long ret = 0;
try
{
    ret = LexicalAnalyzer.ReadInt64(st, true);
}
catch (Exception)
{
    return 0;
}
```

### 3.3 ISNUMERIC 已修复

LazyLoading 的 `ISNUMERIC`（[Creator.Method.cs:L6226](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs)）已使用 `LexicalAnalyzer.NumericCheck(st)`，无需修复。

---

## 四、执行步骤

### 步骤 1：Creator.Method.cs — 加 try-catch

**文件**：[Creator.Method.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs)  
**位置**：`ToIntMethod.GetIntValue()` 方法中，约 L5912

#### 改造前：

```csharp
long ret = LexicalAnalyzer.ReadInt64(st, true);
```

#### 改造后：

```csharp
long ret = 0;
try
{
    ret = LexicalAnalyzer.ReadInt64(st, true);
}
catch (Exception)
{
    return 0;
}
```

---

### 步骤 2：验证编译

```powershell
dotnet build "D:\emuera\emuera_lazyloading_selfmodified_version\emuera_lazyloading_selfmodified_version.sln" 2>&1
```

预期：0 错误，0 警告。

---

## 五、设计决策说明

### 5.1 为什么 catch 所有 Exception 而非特定类型

`LexicalAnalyzer.ReadInt64()` 可能抛出多种异常（`CodeEE`、`IndexOutOfRangeException` 等），且这些异常类型是内部实现细节。`TOINT` 的语义是"无法解析时返回 0"，因此 catch 所有异常是合理的。

### 5.2 为什么不在 catch 中记录日志

`TOINT` 返回 0 是正常的容错行为（与 `VAL` 指令一致），不需要日志。如果脚本需要知道转换是否成功，应使用 `ISNUMERIC` 预检查。

---

## 六、风险点与验证

### 6.1 功能验证

在 ERB 中测试：

```erb
LOCAL = TOINT("0e")
PRINTFORML TOINT("0e") = {LOCAL}  ; 应为 0

LOCAL = TOINT("-1-")
PRINTFORML TOINT("-1-") = {LOCAL}  ; 应为 0

LOCAL = TOINT("123")
PRINTFORML TOINT("123") = {LOCAL}  ; 应为 123（正常情况不受影响）
```

### 6.2 性能影响

`try-catch` 仅在异常路径有开销。正常输入不触发异常，性能无影响。

---

## 七、回滚方案

将 `try-catch` 块还原为原始的单行 `LexicalAnalyzer.ReadInt64(st, true)` 即可。

改动仅涉及 1 个文件的 ~5 行，回滚成本极低。