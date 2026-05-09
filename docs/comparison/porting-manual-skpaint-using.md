# 移植执行手册：SKPaint using 资源释放

> **源**：EmueraDotNet 多个 commit（`ConsoleStyledString.cs`、`GraphicsImage.cs` 等）  
> **目标**：emuera_lazyloading_selfmodified_version `Creator.Method.cs`  
> **日期**：2026-05-09  
> **状态**：✅ 已完成  
> **优先级**：🟢 P2

---

## 一、概述

修复 `Creator.Method.cs` 中 `new SKPaint()` 未使用 `using` 导致的非托管资源泄漏。

**实际状态**：LazyLoading 的 8 处 `new SKPaint()` 中，6 处已使用 `using var`，1 处为 `static readonly`（有意为之），仅 1 处遗漏。

---

## 二、涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| [Creator.Method.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs) | ✅ 已修改 | L7122：`var paint` → `using var paint` |

---

## 三、执行记录

### 已完成的修改

**文件**：[Creator.Method.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs)  
**位置**：L7122

#### 修改前：

```csharp
var paint = new SKPaint();
```

#### 修改后：

```csharp
using var paint = new SKPaint();
```

同时移除了显式的 `paint.Dispose()` 调用（`using var` 自动处理）。

---

## 四、已验证的文件（无需修改）

| 文件 | 行号 | 状态 |
|------|------|------|
| [GraphicsImage.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/image/GraphicsImage.cs) | L172, L343 | ✅ `using var` |
| [ConsoleStyledString.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/ConsoleStyledString.cs) | L263, L501 | ✅ `using var` |
| [StringMeasure.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/StringMeasure.cs) | L17 | ✅ `using var` |
| [ConsoleShapePart.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/ConsoleShapePart.cs) | L204 | ✅ `using var` |
| [Rikaichan.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/Rikaichan.cs) | L38 | ⚪ `static readonly`（有意为之） |

---

## 五、验证编译

```powershell
dotnet build "D:\emuera\emuera_lazyloading_selfmodified_version\emuera_lazyloading_selfmodified_version.sln" 2>&1
```

预期：0 错误，0 警告。