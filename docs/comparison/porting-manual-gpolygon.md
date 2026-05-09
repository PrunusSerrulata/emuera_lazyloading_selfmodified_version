# 移植工作手册：G_POLYGON 多边形指令集

> **目标**：emuera_lazyloading_selfmodified_version `GraphicsImage.cs` + `Creator.Method.cs` + `Creator.cs`  
> **日期**：2026-05-09  
> **状态**：✅ 已完成（2026-05-10）  
> **优先级**：🟡 P1

---

## DotNet 实现

### 1. GraphicsImage.cs 新增方法

```csharp
List<SKPoint> _points;

public void GDrawPolygon()
{
    if (canvas == null) throw new NullReferenceException();
    if (_points == null) throw new NullReferenceException("DrawPolygonに渡されるPointsが空です");
    using (var paint = _pen ?? new SKPaint())
    {
        paint.Style = SKPaintStyle.Stroke;
        canvas.DrawPoints(SKPointMode.Polygon, [.. _points, _points[0]], paint);
    }
}

public void GFillPolygon()
{
    if (canvas == null) throw new NullReferenceException();
    if (_points == null) throw new NullReferenceException("FillPolygonに渡されるPointsが空です");
    using (var paint = _brush ?? new SKPaint())
    {
        paint.Style = SKPaintStyle.Fill;
        var path = new SKPath();
        foreach (var p in _points) path.LineTo(p);
        path.LineTo(_points[0]);
        canvas.DrawPath(path, paint);
    }
}

public void GDrawPolygonAddPoint(SKPoint point)
{
    if (canvas == null) throw new NullReferenceException();
    _points ??= [];
    _points.Add(point);
}

public void GDrawPolygonClearPoint()
{
    if (canvas == null) throw new NullReferenceException();
    if (_points == null) _points = [];
    else _points.Clear();
}
```

GDispose() 中增加 `_points = null;`

### 2. Creator.Method.cs 新增 4 个 FunctionMethod

| ERB 指令 | 类名 | 参数 | 返回值 |
|----------|------|------|--------|
| `G_POLYGON_DRAW` | GraphicsDrawPolygonMethod | `(long)` | `int` |
| `G_POLYGON_FILL` | GraphicsFillPolygonMethod | `(long)` | `int` |
| `G_POLYGON_POINT_ADD` | GraphicsPolygonPointAddMethod | `(long, long, long)` | `int` |
| `G_POLYGON_POINT_CLEAR` | GraphicsPolygonPointClearMethod | `(long)` | `int` |

### 3. Creator.cs 注册

```csharp
["G_POLYGON_DRAW"] = new GraphicsDrawPolygonMethod(),
["G_POLYGON_FILL"] = new GraphicsFillPolygonMethod(),
["G_POLYGON_POINT_ADD"] = new GraphicsPolygonPointAddMethod(),
["G_POLYGON_POINT_CLEAR"] = new GraphicsPolygonPointClearMethod(),
```

## LazyLoading 现状

无多边形绘制功能。GraphicsImage.cs 中无 `_points` 字段和相关方法。

## 移植方案

1. 在 GraphicsImage.cs 中新增 `_points` 字段和 4 个方法
2. 在 GDispose() 中增加 `_points = null;`
3. 在 Creator.Method.cs 中新增 4 个 FunctionMethod 类
4. 在 Creator.cs 中注册 4 个指令
5. 可选：在 Reference 手册中新增 G_POLYGON 系列文档

## 风险评估

- **低风险**：纯新增功能，不影响现有代码
- **注意**：GDrawPolygon 使用 `_pen`（描边），GFillPolygon 使用 `_brush`（填充），需确保 GSETPEN/GSETBRUSH 已正确设置

## 涉及文件

- `Emuera/UI/Game/Image/GraphicsImage.cs`
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs`
- `Emuera/Runtime/Script/Statements/Function/Creator.cs`
