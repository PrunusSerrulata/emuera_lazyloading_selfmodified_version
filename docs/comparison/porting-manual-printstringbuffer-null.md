# 移植执行手册：PrintStringBuffer 空检查

> **源**：EmueraDotNet `PrintStringBuffer.cs` `Flush()`  
> **目标**：emuera_lazyloading_selfmodified_version `PrintStringBuffer.cs` `Flush()`  
> **日期**：2026-05-09  
> **状态**：✅ 已完成

---

## 一、概述

修复 `PrintStringBuffer.Flush()` 在 `ButtonsToDisplayLines()` 返回空数组时，`ret[^1]` 访问抛出 `IndexOutOfRangeException` 的 bug。

**根本原因**：`ButtonsToDisplayLines()` 可能返回空数组（如所有按钮都被过滤），但 `Flush()` 无条件访问 `ret[^1]`。

---

## 二、涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| [PrintStringBuffer.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/PrintStringBuffer.cs) | 修改 | `Flush()` 加 Length 检查 |

仅需修改 **1 个文件，~3 行**。

---

## 三、前置差异分析

### 3.1 当前 LazyLoading 代码（有 bug）

[PrintStringBuffer.cs:L187](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/PrintStringBuffer.cs)：

```csharp
public ConsoleDisplayLine[] Flush(StringMeasure stringMeasure, bool temporary)
{
    fromCssToButton();
    ConsoleDisplayLine[] ret = ButtonsToDisplayLines(m_buttonList, stringMeasure, false, temporary);
    ret[^1].IsLineEnd = isLastLineEnd;  // 无 Length 检查
    clearBuffer();
    return ret;
}
```

### 3.2 DotNet 修复后

[PrintStringBuffer.cs:L166-L170](file:///d:/emuera/EmueraDotNet/UI/Game/PrintStringBuffer.cs)：

```csharp
public ConsoleDisplayLine[] Flush(StringMeasure stringMeasure, bool temporary)
{
    fromCssToButton();
    ConsoleDisplayLine[] ret = ButtonsToDisplayLines(m_buttonList, stringMeasure, false, temporary);
    if (ret.Length > 0)
    {
        ret[^1].IsLineEnd = isLastLineEnd;
    }
    clearBuffer();
    return ret;
}
```

---

## 四、执行步骤

### 步骤 1：PrintStringBuffer.cs — 加 Length 检查

**文件**：[PrintStringBuffer.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/PrintStringBuffer.cs)  
**位置**：`Flush()` 方法，约 L187

#### 改造前：

```csharp
ConsoleDisplayLine[] ret = ButtonsToDisplayLines(m_buttonList, stringMeasure, false, temporary);
ret[^1].IsLineEnd = isLastLineEnd;
```

#### 改造后：

```csharp
ConsoleDisplayLine[] ret = ButtonsToDisplayLines(m_buttonList, stringMeasure, false, temporary);
if (ret.Length > 0)
{
    ret[^1].IsLineEnd = isLastLineEnd;
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

### 5.1 为什么空数组时不设置 IsLineEnd

空数组意味着没有输出行，`isLastLineEnd` 的状态对后续输出无影响。跳过设置是安全的。

---

## 六、风险点与验证

### 6.1 功能验证

1. 正常 PRINT 输出 → 行为不变
2. 触发空输出场景（如所有按钮被过滤）→ 不崩溃

---

## 七、回滚方案

删除 `if (ret.Length > 0)` 包裹即可。

改动仅涉及 1 个文件的 ~3 行，回滚成本极低。