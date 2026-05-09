# 移植工作手册：下划线/删除线渲染

> **目标**：emuera_lazyloading_selfmodified_version `ConsoleStyledString.cs`  
> **日期**：2026-05-09  
> **状态**：✅ 已完成（2026-05-10）  
> **优先级**：🟡 P1

---

## DotNet 实现

DotNet 在 `ConsoleStyledString` 中补全了 `HasUnderline` 和 `HasStrikeout` 属性的渲染逻辑，使 HTML 样式中的 `<u>` 和 `<s>` 标签能在 Skia 渲染中正确显示。

## LazyLoading 现状

需确认当前 ConsoleStyledString 是否已支持装饰线渲染。若未支持，HTML 中的下划线/删除线样式将不生效。

## 移植方案

1. 检查 LazyLoading 的 ConsoleStyledString 是否已有 HasUnderline/HasStrikeout
2. 若无，参照 DotNet 实现补全
3. Skia 渲染中使用 SKPaint.UnderlineText / SKPaint.StrikeThruText 属性

## 风险评估

- **低风险**：纯新增渲染特性
- **注意**：需确认 SkiaSharp 版本是否支持 UnderlineText/StrikeThruText

## 涉及文件

- `Emuera/UI/Game/ConsoleStyledString.cs`
