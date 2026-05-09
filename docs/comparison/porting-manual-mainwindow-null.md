# 移植执行手册：MainWindow console null 检查

> **源**：EmueraDotNet `MainWindow.cs` `ShowConfigDialog()` + 剪贴板处理器  
> **目标**：emuera_lazyloading_selfmodified_version `MainWindow.cs`  
> **日期**：2026-05-09  
> **状态**：✅ 已完成

---

## 一、概述

修复启动后立即点击设置按钮或剪贴板菜单项时，因 `console` 未初始化导致 `NullReferenceException` 崩溃的 bug。

**根本原因**：`ShowConfigDialog()` 和剪贴板处理器在 `console` 为 null 时直接访问其成员。

---

## 二、涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| [MainWindow.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Framework/Forms/MainWindow.cs) | 修改 | 2 处加 null 检查 |

仅需修改 **1 个文件，~5 行**。

---

## 三、前置差异分析

### 3.1 ShowConfigDialog — 当前 LazyLoading 代码（有 bug）

[MainWindow.cs:L1115](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Framework/Forms/MainWindow.cs)：

```csharp
public void ShowConfigDialog()
{
    string lang = Config.EmueraLang;
    ConfigDialog dialog = new();
    // ... 无 console null 检查
```

### 3.2 ShowConfigDialog — DotNet 修复后

[MainWindow.cs:L590-L591](file:///d:/emuera/EmueraDotNet/UI/Framework/Forms/MainWindow.cs)：

```csharp
public void ShowConfigDialog()
{
    if (console == null || GlobalStatic.Console == null)
        return;
    // ...
```

### 3.3 剪贴板处理器 — 当前 LazyLoading 代码（有 bug）

[MainWindow.cs:L1751](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Framework/Forms/MainWindow.cs)：

```csharp
if (クリップボードにコピーToolStripMenuItem.Checked)
    GlobalStatic.Console.CBProc.Init();  // 无 null 检查
else
    GlobalStatic.Console.CBProc.Reset(); // 无 null 检查
```

### 3.4 剪贴板处理器 — DotNet 修复后

DotNet 的剪贴板处理器在多个位置有 `console == null` 检查（[MainWindow.cs](file:///d:/emuera/EmueraDotNet/UI/Framework/Forms/MainWindow.cs) 共 32 处 null 检查）。

---

## 四、执行步骤

### 步骤 1：ShowConfigDialog — 加 null 检查

**文件**：[MainWindow.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Framework/Forms/MainWindow.cs)  
**位置**：`ShowConfigDialog()` 方法开头

#### 改造前：

```csharp
public void ShowConfigDialog()
{
    string lang = Config.EmueraLang;
```

#### 改造后：

```csharp
public void ShowConfigDialog()
{
    if (console == null || GlobalStatic.Console == null)
        return;
    string lang = Config.EmueraLang;
```

---

### 步骤 2：剪贴板处理器 — 加 null 检查

**文件**：[MainWindow.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Framework/Forms/MainWindow.cs)  
**位置**：`クリップボードにコピーToolStripMenuItem_Click_1` 方法

#### 改造前：

```csharp
if (クリップボードにコピーToolStripMenuItem.Checked)
    GlobalStatic.Console.CBProc.Init();
else
    GlobalStatic.Console.CBProc.Reset();
```

#### 改造后：

```csharp
if (GlobalStatic.Console == null)
    return;
if (クリップボードにコピーToolStripMenuItem.Checked)
    GlobalStatic.Console.CBProc.Init();
else
    GlobalStatic.Console.CBProc.Reset();
```

---

### 步骤 3：验证编译

```powershell
dotnet build "D:\emuera\emuera_lazyloading_selfmodified_version\emuera_lazyloading_selfmodified_version.sln" 2>&1
```

预期：0 错误，0 警告。

---

## 五、设计决策说明

### 5.1 为什么静默返回而非弹窗提示

`console == null` 意味着引擎尚未初始化完成，此时弹窗可能引发更多问题。静默返回是最安全的处理方式，与 DotNet 行为一致。

### 5.2 为什么检查 `console` 和 `GlobalStatic.Console` 两个变量

`console` 是 `MainWindow` 的实例字段，`GlobalStatic.Console` 是全局静态引用。两者在不同时机被赋值，需要同时检查。

---

## 六、风险点与验证

### 6.1 功能验证

1. 启动引擎，在 ERB 加载完成前点击菜单栏「设置」→ 应无反应（不崩溃）
2. 正常启动后点击「设置」→ 应正常打开设置对话框
3. 启动后点击剪贴板菜单项 → 应无崩溃

---

## 七、回滚方案

删除新增的 null 检查行即可。

改动仅涉及 1 个文件的 ~5 行，回滚成本极低。