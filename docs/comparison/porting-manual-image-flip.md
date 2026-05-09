# 移植工作手册：图像负尺寸翻转

> **目标**：emuera_lazyloading_selfmodified_version `ASpriteSingle.GraphicsDraw()`  
> **日期**：2026-05-09  
> **状态**：⏳ 待处理  
> **优先级**：🟡 P1

---

## DotNet 实现

DotNet 在 `CroppedImage.cs` 的 `ASpriteSingle.GraphicsDraw(SKCanvas g, Rectangle destRect)` 中检测 `destRect` 宽高符号：

```csharp
var sx = Math.Sign(destRect.Width);
var sy = Math.Sign(destRect.Height);
if (sx != 1 || sy != 1)
{
    var flipedBitmap = new SKBitmap(Math.Abs(destRect.Width), Math.Abs(destRect.Height));
    using var canvas = new SKCanvas(flipedBitmap);
    canvas.Scale(sx, sy, flipedBitmap.Width / 2, flipedBitmap.Height / 2);
    canvas.DrawImage(Image, SrcRectangle.ToSKRect(), SKRect.Create(new SKPoint(), flipedBitmap.Info.Size), JSONConfig.SamplingOptions);
    var point = destRect.Location.ToSKPoint();
    if (sx < 0) point.Offset(-flipedBitmap.Width, 0);
    if (sy < 0) point.Offset(0, -flipedBitmap.Width);
    g.DrawBitmap(flipedBitmap, point, _paint);
}
else
{
    g.DrawImage(Image, SrcRectangle.ToSKRect(), destRect.ToSKRect(), JSONConfig.SamplingOptions, _paint);
}
```

同样逻辑应用于带 `SKColorFilter attr` 的重载版本。

## LazyLoading 现状

LazyLoading 的 `ASpriteSingle.GraphicsDraw` 不检测负尺寸，直接使用 `destRect.ToSKRect()` 绘制。负尺寸传入时 SKRect 会产生异常或静默忽略。

## 移植方案

1. 在 `ASpriteSingle.GraphicsDraw(SKCanvas g, Rectangle destRect)` 中加入符号检测逻辑
2. 在 `ASpriteSingle.GraphicsDraw(SKCanvas g, Rectangle destRect, SKColorFilter attr)` 中同步修改
3. 注意：DotNet 使用 `JSONConfig.SamplingOptions`，LazyLoading 使用 `_paint`，保持 LazyLoading 现有方案

## 风险评估

- **低风险**：纯新增逻辑分支，不影响现有正尺寸路径
- **注意**：`flipedBitmap` 每次翻转都创建新位图，频繁调用可能产生 GC 压力，可考虑缓存

## 涉及文件

- `src/Emuera.UI.Game/Image/CroppedImage.cs`（或 LazyLoading 对应路径）
