# 移植工作手册：SKPaint 过时 API 修复

> **目标**：emuera_lazyloading_selfmodified_version 相关绘图代码  
> **日期**：2026-05-09  
> **状态**：✅ 已完成  
> **优先级**：🟢 P2

---

## DotNet 实现

DotNet 将 `SKPaint.TextAlign` 属性调用移至 `DrawText` 参数，消除 3 处编译警告。SkiaSharp 2.88+ 中 `SKPaint.TextAlign` 已标记为过时，推荐使用 `SKTextAlign` 参数。

## LazyLoading 现状

可能仍使用 `SKPaint.TextAlign` 属性，产生编译警告。

## 移植方案

1. 搜索所有 `paint.TextAlign =` 赋值
2. 替换为 `canvas.DrawText(text, x, y, align, paint)` 参数形式
3. 或使用 `new SKFont()` + `canvas.DrawText(text, font, paint)` 新 API

## 风险评估

- **极低风险**：仅消除编译警告，功能不变

## 涉及文件

- 需搜索 `paint.TextAlign` 使用位置
