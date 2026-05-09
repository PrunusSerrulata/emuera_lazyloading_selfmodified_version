# 移植工作手册：Stopwatch 高精度计时

> **目标**：emuera_lazyloading_selfmodified_version `SpriteAnime.cs` + `EmueraConsole.cs` + `Process.cs`  
> **日期**：2026-05-09  
> **状态**：⏳ 待处理  
> **优先级**：🟡 P1

---

## DotNet 实现

DotNet 使用 `Stopwatch.GetTimestamp()` + `Stopwatch.GetElapsedTime()` 替代 `DateTime.Now`：

### 1. SpriteAnime.cs（动画帧计时）

```csharp
long startTime;
long lastFrameTime;
int lastFrame = -1;

internal void ResetTime()
{
    startTime = Stopwatch.GetTimestamp();
    lastFrameTime = startTime;
    lastFrame = -1;
}

private AnimeFrame GetCurrentFrame()
{
    var now = Stopwatch.GetTimestamp();
    if (lastFrame == -1) { startTime = now; lastFrame = 0; return FrameList[0]; }
    if (Stopwatch.GetElapsedTime(lastFrameTime, now).TotalMilliseconds < 1 && lastFrame >= 0)
        return FrameList[lastFrame];
    var elapsedMs = Stopwatch.GetElapsedTime(startTime, now).Milliseconds;
    // ... 帧选择逻辑
}
```

### 2. EmueraConsole.cs（帧率控制 + 背景色更新）

```csharp
long _frameStartTime = Stopwatch.GetTimestamp();
uint msPerFrame = 1000 / 60;

// RefreshStrings 中：
if (Stopwatch.GetElapsedTime(_frameStartTime).TotalMilliseconds < msPerFrame && ...)
    return;

// SetBgColor 中：
var now = Stopwatch.GetTimestamp();
while (Stopwatch.GetElapsedTime(_drawStopwatch, now).TotalMilliseconds < msPerFrame)
{ Application.DoEvents(); now = Stopwatch.GetTimestamp(); }
```

### 3. Process.cs（无限循环检测）

```csharp
startTime = Stopwatch.GetTimestamp();
var elapsedTime = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
if (elapsedTime < Config.InfiniteLoopAlertTime) return;
```

## LazyLoading 现状

使用 `DateTime.Now` 计时，精度约 15ms，受系统时间修改影响。

## 移植方案

1. 将 SpriteAnime.cs 中的 `DateTime.Now` 替换为 `Stopwatch.GetTimestamp()`
2. 将 EmueraConsole.cs 中的帧率控制逻辑替换
3. 将 Process.cs 中的无限循环检测替换
4. `Stopwatch.GetElapsedTime()` 是 .NET 7+ API，LazyLoading 目标 .NET 8，兼容

## 风险评估

- **中风险**：涉及动画计时核心逻辑，需充分测试动画播放
- **注意**：DotNet 版本无动画暂停/恢复功能，LazyLoading 有此功能，移植时需保留暂停/恢复逻辑
- **注意**：`Stopwatch.GetElapsedTime(startTime, now).Milliseconds` 只取毫秒部分（0-999），而非 TotalMilliseconds，DotNet 代码可能存在 bug（长时间动画会溢出），移植时应使用 `.TotalMilliseconds`

## 涉及文件

- `Emuera/UI/Game/Image/CroppedImage.cs`（SpriteAnime 类）
- `Emuera/UI/Game/EmueraConsole.cs`
- `Emuera/Runtime/Script/Process.cs`
