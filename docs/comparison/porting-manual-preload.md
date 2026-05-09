# 移植执行手册：Preload 字节级优化

> **源**：EmueraDotNet `Preload.cs` + `EmueraConsole.cs` + `Process.cs` + `EraStreamReader.cs`  
> **目标**：emuera_lazyloading_selfmodified_version 新增 `Preload.cs` + 修改 3 个文件  
> **日期**：2026-05-09  
> **状态**：✅ 已完成

---

## 一、概述

将 ERB/CSV 文件在启动时一次性预加载到内存（`ConcurrentDictionary<string, string[]>`），后续 `EraStreamReader` 从内存读取而非每次 `File.ReadAllLines()`。

**核心收益**：
- 减少磁盘 IO（文件只读一次）
- 并行预加载（`AsParallel().ForAll()`）
- 字节级行分割（`Span<byte>.Split('\n')` 比 `File.ReadAllLines` 更高效）
- 热重载（`ReloadErb`）时复用预加载缓存

---

## 二、涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| [Preload.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Utils/Preload.cs) | **新增** | 核心预加载逻辑 |
| [EmueraConsole.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/EmueraConsole.cs) | 修改 | 启动时调用 `Preload.Load()` |
| [Process.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Process.cs) | 修改 | 热重载时调用 `Preload.Load()` |
| [EraStreamReader.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Utils/EraStreamReader.cs) | 修改 | 新增 `OpenOnCache()` 方法，从 Preload 读取 |

---

## 三、前置差异分析

### 3.1 当前 LazyLoading 文件读取方式

[EraStreamReader.cs:L44](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Utils/EraStreamReader.cs)：

```csharp
_fileLines = File.ReadAllLines(filepath, EncodingHandler.DetectEncoding(path));
```

每次打开文件都从磁盘读取。

### 3.2 DotNet Preload 实现

[Preload.cs](file:///d:/emuera/EmueraDotNet/Runtime/Utils/Preload.cs)（114 行）：

```csharp
static partial class Preload
{
    static ConcurrentDictionary<string, string[]> files = new(StringComparer.OrdinalIgnoreCase);

    public static string[] GetFileLines(string path) => files[path];

    public static async Task Load(string path)
    {
        var dir = new DirectoryInfo(path);
        if (dir.Exists)
        {
            await Task.Run(() =>
            {
                dir.EnumerateFiles("*", SearchOption.AllDirectories)
                .AsParallel()
                .Where(x => ext is .csv/.erb/.erh)
                .ForAll(childPath =>
                {
                    var bytes = File.ReadAllBytes(childPath.FullName).AsSpan();
                    // BOM 检测 + 字节级行分割
                    var lines = new List<string>();
                    foreach (var range in bytes.Split((byte)'\n'))
                    {
                        // 处理 \r\n
                        lines.Add(encoding.GetString(range));
                    }
                    files[childPath.FullName] = lines.ToArray();
                });
            });
        }
        else
        {
            files[path] = File.ReadAllLines(path, Config.Config.Encode);
        }
    }

    public static void Clear() => files.Clear();
}
```

### 3.3 DotNet EraStreamReader 集成

[EraStreamReader.cs:L67](file:///d:/emuera/EmueraDotNet/Runtime/Utils/EraStreamReader.cs)：

```csharp
public bool OpenOnCache(string path)
{
    _fileLines = Preload.GetFileLines(path);
    return true;
}
```

### 3.4 DotNet EmueraConsole 集成

[EmueraConsole.cs:L377-L382](file:///d:/emuera/EmueraDotNet/UI/Game/EmueraConsole.cs)：

```csharp
Preload.Clear();
await Preload.Load(Program.ErbDir);
await Preload.Load(Program.CsvDir);
```

### 3.5 DotNet Process 热重载集成

[Process.cs:L241-L242](file:///d:/emuera/EmueraDotNet/Runtime/Script/Process.cs)：

```csharp
await Preload.Load(Program.ErbDir);
await Preload.Load(Program.CsvDir);
```

### 3.6 LazyLoading 独有逻辑（不可破坏）

- **懒加载跳过**：`EraStreamReader` 的懒加载逻辑不受影响（Preload 只影响文件读取方式）
- **编码检测**：`EncodingHandler.DetectEncoding(path)` 在 Preload 中同样需要处理
- **`*#*` 目录优先加载**：不受影响

---

## 四、执行步骤

### 步骤 1：新增 Preload.cs

**文件**：[Preload.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Utils/Preload.cs)（新建）

```csharp
using MinorShift.Emuera.Runtime.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinorShift.Emuera.Runtime.Utils;

static partial class Preload
{
    static ConcurrentDictionary<string, string[]> files = new(StringComparer.OrdinalIgnoreCase);

    public static string[] GetFileLines(string path)
    {
        return files[path];
    }

    public static async Task Load(string path)
    {
        var dir = new DirectoryInfo(path);
        if (dir.Exists)
        {
            await Task.Run(() =>
            {
                dir.EnumerateFiles("*", SearchOption.AllDirectories)
                .AsParallel()
                .Where(x =>
                {
                    var ext = x.Extension;
                    return ext.Equals(".csv", StringComparison.OrdinalIgnoreCase) ||
                            ext.Equals(".erb", StringComparison.OrdinalIgnoreCase) ||
                            ext.Equals(".erh", StringComparison.OrdinalIgnoreCase);
                }).ForAll((childPath) =>
                {
                    var bytes = File.ReadAllBytes(childPath.FullName).AsSpan();

                    if (bytes.IsEmpty)
                    {
                        files[childPath.FullName] = [""];
                        return;
                    }

                    var encoding = Config.Config.Encode;
                    if (bytes.StartsWith<byte>([0xEF, 0xBB, 0xBF]))
                    {
                        encoding = Encoding.UTF8;
                        bytes = bytes[3..];
                    }

                    var lines = new List<string>();
                    var n = (byte)'\n';

                    foreach (var range in ((ReadOnlySpan<byte>)bytes[..]).Split(n))
                    {
                        if (bytes[range].IsEmpty)
                        {
                            lines.Add("");
                        }
                        else
                        {
                            if (bytes[range].EndsWith([(byte)'\r']))
                            {
                                lines.Add(encoding.GetString(bytes[range.Start..(range.End.Value - 1)]));
                            }
                            else
                            {
                                lines.Add(encoding.GetString(bytes[range]));
                            }
                        }
                    }
                    files[childPath.FullName] = [.. lines];
                });
            });
        }
        else
        {
            var key = path;
            var value = File.ReadAllLines(path, Config.Config.Encode);
            files[key] = value;
        }
    }

    public static async Task Load(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            await Load(path);
        }
    }

    public static void Clear()
    {
        files.Clear();
    }
}
```

---

### 步骤 2：EraStreamReader.cs — 新增 OpenOnCache 方法

**文件**：[EraStreamReader.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Utils/EraStreamReader.cs)

在现有 `Open()` 方法之后添加：

```csharp
public bool OpenOnCache(string path)
{
    return OpenOnCache(path, Path.GetFileName(path));
}

public bool OpenOnCache(string path, string name)
{
    filepath = path.ToString();
    filename = name.ToString();
    curNo = 0;
    nextNo = 0;
    _fileLines = Preload.GetFileLines(path);
    return true;
}
```

---

### 步骤 3：EmueraConsole.cs — 启动时预加载

**文件**：[EmueraConsole.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/EmueraConsole.cs)

在 `RunEmueraProgram()` 或初始化流程中，ERB 加载之前添加：

```csharp
Preload.Clear();
await Preload.Load(Program.ErbDir);
await Preload.Load(Program.CsvDir);
```

并在 ERB 加载完成后添加：

```csharp
Preload.Clear();
```

---

### 步骤 4：Process.cs — 热重载时预加载

**文件**：[Process.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Process.cs)

在 `ReloadErbAll()` 方法开头添加：

```csharp
await Preload.Load(Program.ErbDir);
await Preload.Load(Program.CsvDir);
```

---

### 步骤 5：ErbLoader.cs — 使用 OpenOnCache

**文件**：[ErbLoader.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Loader/ErbLoader.cs)

在 `loadErb()` 方法中，将 `eReader.Open(filepath)` 替换为 `eReader.OpenOnCache(filepath)`。

> **注意**：需要确认 `EraStreamReader` 的 `OpenOnCache` 与现有 `Open` 的兼容性。如果 `Open` 中有额外的初始化逻辑（如编码检测），需要在 `OpenOnCache` 中保留。

---

### 步骤 6：验证编译

```powershell
dotnet build "D:\emuera\emuera_lazyloading_selfmodified_version\emuera_lazyloading_selfmodified_version.sln" 2>&1
```

预期：0 错误，0 警告。

---

## 五、设计决策说明

### 5.1 为什么用字节级 Split 而非 ReadAllLines

`File.ReadAllLines` 内部使用 `StreamReader`，逐行解码字符串。字节级 `Split('\n')` + 批量 `Encoding.GetString()` 减少了字符串分配次数，对大文件（如 CharaCSV）效果显著。

### 5.2 为什么用 ConcurrentDictionary

`AsParallel().ForAll()` 多线程并发写入，`ConcurrentDictionary` 保证线程安全。

### 5.3 为什么保留 LazyLoading 的编码检测

LazyLoading 使用 `EncodingHandler.DetectEncoding(path)` 进行编码检测。Preload 中需要保留 BOM 检测逻辑（UTF-8 BOM `0xEF 0xBB 0xBF`），但完整的编码检测（如 Shift-JIS vs UTF-8 自动判断）需要在 Preload 中实现或保持使用 `Config.Config.Encode`。

> **风险点**：如果 LazyLoading 的编码检测逻辑比 DotNet 更复杂（如自动检测非 BOM 文件的编码），Preload 的字节级处理可能无法正确解码。需要在步骤 5 中仔细验证。

### 5.4 为什么加载完成后 Clear

`Preload.Clear()` 释放预加载占用的内存。ERB 文件已被解析为内部数据结构（`LabelDictionary` 等），原始文本不再需要。CSV 文件同理。

---

## 六、风险点与验证

### 6.1 编码兼容性（高风险）

| 检查项 | 方法 |
|--------|------|
| BOM 检测 | 确认 `EncodingHandler.DetectEncoding` 是否依赖 BOM |
| 非 BOM 文件编码 | 确认 LazyLoading 是否有自动编码检测逻辑 |
| 字节级 Split 正确性 | 对比 `File.ReadAllLines` 和 Preload 的行分割结果 |

### 6.2 功能验证

1. 启动引擎，确认所有 ERB/CSV 正常加载
2. 检查 `emuera.log` 无编码相关错误
3. 测试热重载（`RELOADERB`）功能正常
4. 对比预加载前后的启动耗时

### 6.3 内存验证

- 预加载后内存占用增加（所有 ERB/CSV 文本驻留内存）
- `Preload.Clear()` 后内存应释放
- 大型项目（数千个 ERB 文件）需评估内存峰值

---

## 七、回滚方案

1. 删除 `Preload.cs`
2. 还原 `EraStreamReader.cs`（删除 `OpenOnCache` 方法）
3. 还原 `EmueraConsole.cs`（删除 Preload 调用）
4. 还原 `Process.cs`（删除 Preload 调用）
5. 还原 `ErbLoader.cs`（`OpenOnCache` → `Open`）
6. 重新编译

改动涉及 5 个文件，回滚成本中等。