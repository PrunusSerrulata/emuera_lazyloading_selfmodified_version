# 移植执行手册：并行加载

> **源**：EmueraDotNet `ErbLoader.LoadErbDir()`  
> **目标**：emuera_lazyloading_selfmodified_version `ErbLoader.LoadErbDir()`  
> **日期**：2026-05-09  
> **状态**：❌ 否决

---

## 一、概述

将 DotNet 变体的 `AsParallel().ForAll()` 并行加载策略移植到 LazyLoading 变体，与现有懒加载跳过策略组合使用。

**核心思路**：懒加载跳过的文件保持跳过，未被跳过的文件使用并行加载加速。

---

## 二、涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| [ErbLoader.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Loader/ErbLoader.cs) | 修改 | 核心改动：`LoadErbDir()` 方法 |

仅需修改 **1 个文件**。

---

## 三、前置差异分析

### 3.1 using 语句差异

LazyLoading 版缺少 3 个命名空间：

```diff
// ErbLoader.cs 头部 using 区域
+ using System.Linq;               // AsParallel(), ForAll()
+ using System.Threading;          // CancellationTokenSource
+ using System.Collections.Concurrent; // ConcurrentQueue
```

### 3.2 `loadErb()` 签名差异

| 变体 | 签名 |
|------|------|
| DotNet | `loadErb(string filepath, string filename, List<string> isOnlyEvent)` |
| LazyLoading | `loadErb(string filepath, string filename, List<string> isOnlyEvent, bool isLazyLoading = false)` |

LazyLoading 多了一个 `isLazyLoading` 参数（有默认值 `false`），并行调用时无需传参，使用默认值即可。

### 3.3 本地化字符串差异

| 用途 | DotNet | LazyLoading |
|------|--------|-------------|
| 加载耗时 | `LocalizationManager.SystemLine.ElapsedTimeLoad` | `trsl.ElapsedTimeLoad` |
| 加载文件 | `LocalizationManager.SystemLine.LoadingFile` | `trsl.LoadingFile` |
| 总耗时 | `LocalizationManager.SystemLine.ElapsedTime` | `trsl.ElapsedTime` |
| 构建函数 | `LocalizationManager.SystemLine.BuildingUserFunc` | `trsl.BuildingUserFunc` |
| 语法检查 | `LocalizationManager.SystemLine.CheckingSyntax` | `trsl.CheckingSyntax` |
| 加载完成 | `LocalizationManager.SystemLine.LoadComplete` | `trsl.LoadComplete` |

LazyLoading 版使用 `trsl.*` 别名，移植时保持 LazyLoading 的本地化方式不变。

### 3.4 计时方式差异

| 变体 | 计时方式 |
|------|---------|
| DotNet | `Stopwatch.StartNew()` + `ElapsedMilliseconds` |
| LazyLoading | `DateTime.Now` + `TotalMilliseconds` |

移植时保持 LazyLoading 的计时方式不变。

### 3.5 LazyLoading 独有逻辑（不可破坏）

以下逻辑是 LazyLoading 版独有的，并行化改造**必须保留**：

1. **`*#*` 目录优先加载**（EE 扩展）：`firstDir` 遍历 → 这些文件先于主循环加载
2. **懒加载跳过**：`if (useLazyLoading && parentProcess.LazyLoadingFiles.Contains(file)) continue;`
3. **懒加载表构建/更新**：`LoadLazyLoadingTable()` / `SaveLazyLoadingList()` / `SavePartialLazyLoadingList()`
4. **懒加载状态输出**：`LazyLoadingTableCount` / `LazyLoadingNoTable` / `LazyLoadingDebugErbTime` 等
5. **WinmmTimer 总计时**：`totalstarttime = WinmmTimer.TickCount`

---

## 四、执行步骤

### 步骤 1：添加 using 语句

**文件**：[ErbLoader.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Loader/ErbLoader.cs)

在现有 using 区域末尾（`using trsl = ...` 之后，`namespace` 之前）添加：

```csharp
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
```

**位置**：约第 18 行，`using trsl = ...` 之后。

---

### 步骤 2：改造主加载循环

**文件**：[ErbLoader.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Loader/ErbLoader.cs)  
**位置**：`LoadErbDir()` 方法中，`*#*` 优先加载循环之后、主 `foreach (var erb in erbFiles)` 循环处（约第 99-117 行）

#### 改造前（当前代码）：

```csharp
foreach (var erb in erbFiles)
{
    string filename = erb.Key;
    string file = erb.Value;
    if (loadedFiles.Contains(file))
        continue;
    // 懒加载跳过
    if (useLazyLoading && parentProcess.LazyLoadingFiles.Contains(file))
        continue;

    if (displayReport && Program.AnalysisMode)
        output.PrintSystemLine(string.Format(trsl.LoadingFile.Text, filename));
    loadErb(file, filename, isOnlyEvent);
};
```

#### 改造后：

```csharp
// 分离出需要加载的文件列表（排除已加载的优先目录文件和懒加载跳过文件）
var filesToLoad = erbFiles
    .Where(erb => !loadedFiles.Contains(erb.Value))
    .Where(erb => !useLazyLoading || !parentProcess.LazyLoadingFiles.Contains(erb.Value))
    .ToList();

ConcurrentQueue<string> logQueue = [];

var task = Task.Run(() => filesToLoad.AsParallel().ForAll(erb =>
{
    string filename = erb.Key;
    string file = erb.Value;
    loadErb(file, filename, isOnlyEvent);
    if (displayReport && Program.AnalysisMode)
        logQueue.Enqueue(string.Format(trsl.LoadingFile.Text, filename));
}));

var source = new CancellationTokenSource();
if (displayReport && Program.AnalysisMode)
{
    var locks = new Lock();
    await Task.Run(() =>
    {
        while (!source.IsCancellationRequested)
        {
            if (logQueue.TryDequeue(out var log))
            {
                lock (locks)
                {
                    output.PrintSystemLine(log);
                }
            }
        }
    }, source.Token);
}
await task;
source.Cancel();
```

---

### 步骤 3：验证编译

```powershell
dotnet build "D:\emuera\emuera_lazyloading_selfmodified_version\emuera_lazyloading_selfmodified_version.sln" 2>&1
```

预期：0 错误，0 警告。

---

## 五、设计决策说明

### 5.1 为什么用 LINQ `Where` 预过滤而非在 lambda 内 `if continue`

```csharp
// 方案 A（推荐）：预过滤
var filesToLoad = erbFiles.Where(...).ToList();
filesToLoad.AsParallel().ForAll(erb => loadErb(...));

// 方案 B（不推荐）：lambda 内判断
erbFiles.AsParallel().ForAll(erb => {
    if (shouldSkip) return;
    loadErb(...);
});
```

选择方案 A 的原因：
- `AsParallel().ForAll()` 的分区策略基于集合大小，预过滤后集合更小，分区更均匀
- 避免线程池线程浪费在"判断后立即返回"的空操作上
- 语义更清晰：`filesToLoad` 明确表达"这是需要加载的文件集合"

### 5.2 为什么保留 `Program.AnalysisMode` 条件

LazyLoading 版的日志输出受 `Program.AnalysisMode` 控制（非 Analysis 模式下不输出逐文件日志）。并行化后保持此行为：仅在 `displayReport && Program.AnalysisMode` 时才启用 `ConcurrentQueue` 日志通道。

### 5.3 为什么 `*#*` 优先目录不并行化

`*#*` 目录中的文件需要**先于**其他文件加载（这是 EE 扩展的语义：目录名含 `#` 的文件优先加载，用于覆盖/补丁机制）。如果并行化会破坏加载顺序保证。因此 `*#*` 目录保持原有的顺序加载。

### 5.4 线程安全

- `loadErb()` 内部通过 `labelDic.IfFileLoadClearLabelWithPath(filename)` 操作 `LabelDictionary`，需确认该方法是线程安全的
- `isOnlyEvent` 列表在并行环境下被多线程写入，需确认 `List<T>.Add()` 的调用是否安全——如果不安全，需改用 `ConcurrentBag<string>` 或加锁
- 日志输出通过 `ConcurrentQueue` + 独立消费线程实现线程安全

---

## 六、风险点与验证

### 6.1 线程安全验证（执行前必须确认）

| 检查项 | 方法 | 风险 |
|--------|------|------|
| `labelDic.IfFileLoadClearLabelWithPath()` | 阅读源码确认是否有锁 | 中 |
| `labelDic.AddLabel()` / `AddLabelDollar()` | 阅读源码确认是否有锁 | 中 |
| `isOnlyEvent.Add()` | 多线程写 `List<T>` 不安全 | 高 |
| `ParserMediator.Warn()` | 阅读源码确认是否有锁 | 低 |

**验证命令**（在源码中搜索锁机制）：

```powershell
# 检查 LabelDictionary 是否有锁
rg "lock|Monitor|ConcurrentDictionary" "d:\emuera\emuera_lazyloading_selfmodified_version\Emuera\Runtime\Script\Data\LabelDictionary.cs"

# 检查 ParserMediator.Warn 是否有锁
rg "lock|Monitor" "d:\emuera\emuera_lazyloading_selfmodified_version\Emuera\Runtime\Script\Parser\ParserMediator.cs"
```

### 6.2 功能验证

1. 启动引擎，确认所有 ERB 文件正常加载
2. 检查 `emuera.log` 无异常错误
3. 对比并行加载前后的启动耗时（`LazyLoadingDebugErbTime`）
4. 多次启动验证结果一致性（无竞态条件导致的非确定性行为）

---

## 七、回滚方案

如果并行加载引入线程安全问题，回滚步骤：

1. 删除步骤 1 添加的 3 个 using 语句
2. 将步骤 2 改造的代码还原为原始 `foreach` 循环
3. 重新编译

改动仅涉及 1 个文件的 2 处位置，回滚成本极低。

---

## 八、后续优化（可选，不在本次范围内）

1. **可配置并行度**：`WithDegreeOfParallelism(Environment.ProcessorCount)` 限制最大线程数
2. **取消令牌集成**：将 `CancellationToken` 连接到引擎关闭流程
3. **进度报告**：利用 `ConcurrentQueue` 实现实时进度百分比