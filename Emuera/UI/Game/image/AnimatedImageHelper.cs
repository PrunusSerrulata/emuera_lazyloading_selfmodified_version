using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace MinorShift.Emuera.UI.Game.Image
{
    public static class AnimatedImageHelper
    {
        // 缓存已解码的动画帧，避免同一个文件在 CSV 中被多次引用时重复解码
        private static Dictionary<string, List<(SKBitmap Bitmap, int Delay)>> _cache = new(System.StringComparer.OrdinalIgnoreCase);

        public static List<(SKBitmap Bitmap, int Delay)> Decode(string filepath)
        {
            // 1. 检查缓存 (线程安全)
            lock (_cache)
            {
                if (_cache.TryGetValue(filepath, out var cachedFrames)) 
                    return cachedFrames;
            }

            if (!File.Exists(filepath)) return null;

            using var codec = SKCodec.Create(filepath);
            if (codec == null || codec.FrameCount <= 1) 
                return null; // 不是动画文件（单帧）

            var frames = new List<(SKBitmap, int)>();
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height);
            
            // 必须使用一个虚拟画布来合成帧，因为 WebP/GIF 通常存储的是差异帧 (Delta)
            using var canvasBitmap = new SKBitmap(info);
            using var canvas = new SKCanvas(canvasBitmap);
            canvas.Clear(SKColors.Transparent);

            SKBitmap previousFrame = null;

            for (int i = 0; i < codec.FrameCount; i++)
            {
                var frameInfo = codec.FrameInfo[i];
                int delay = frameInfo.Duration > 0 ? frameInfo.Duration : 100;

                // 1. 处理上一帧的清理模式 (DisposalMethod)
                if (i > 0)
                {
                    var prevFrameInfo = codec.FrameInfo[i - 1];
                    if (prevFrameInfo.DisposalMethod == SKCodecAnimationDisposalMethod.RestoreBackgroundColor)
                    {
                        using var clearPaint = new SKPaint { BlendMode = SKBlendMode.Src, Color = SKColors.Transparent };
                        // 使用整个画布大小，因为当前SkiaSharp版本不支持FrameRect
                        canvas.DrawRect(0, 0, info.Width, info.Height, clearPaint);
                    }
                    else if (prevFrameInfo.DisposalMethod == SKCodecAnimationDisposalMethod.RestorePrevious)
                    {
                        if (previousFrame != null)
                        {
                            canvas.Clear(SKColors.Transparent);
                            canvas.DrawBitmap(previousFrame, 0, 0);
                        }
                    }
                }

                // 2. 如果当前帧需要 RestorePrevious，则备份当前画布状态
                if (frameInfo.DisposalMethod == SKCodecAnimationDisposalMethod.RestorePrevious)
                {
                    previousFrame?.Dispose();
                    previousFrame = canvasBitmap.Copy();
                }

                // 3. 提取当前帧的原始像素
                using var frameBmp = new SKBitmap(info);
                var result = codec.GetPixels(info, frameBmp.GetPixels(), new SKCodecOptions(i));
                
                if (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput)
                {
                    // 4. 使用默认的混合模式，因为当前SkiaSharp版本不支持Blend属性
                    using var blendPaint = new SKPaint { BlendMode = SKBlendMode.SrcOver };
                    canvas.DrawBitmap(frameBmp, 0, 0, blendPaint);
                }

                // 5. 保存合成后的最终帧 (必须 Copy，否则会被下一帧覆盖)
                frames.Add((canvasBitmap.Copy(), delay));
            }

            previousFrame?.Dispose();
            
            // 存入缓存
            lock (_cache)
            {
                _cache[filepath] = frames;
            }
            return frames;
        }

        public static void ClearCache()
        {
            lock (_cache)
            {
                foreach (var frames in _cache.Values)
                {
                    foreach (var frame in frames)
                    {
                        frame.Bitmap?.Dispose();
                    }
                }
                _cache.Clear();
            }
        }
    }
}