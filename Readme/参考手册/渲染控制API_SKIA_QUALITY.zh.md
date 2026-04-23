# SET_TEXT_DRAWING_MODE, GET_TEXT_DRAWING_MODE, SET_SKIA_QUALITY, GET_SKIA_QUALITY, HTML_PRINT font 渲染属性扩展

| 命令/属性名称 | 参数 | 返回值/效果 |
| :--- | :--- | :--- |
| **SET_TEXT_DRAWING_MODE** | int mode | int (1=成功, 0=失败) |
| **GET_TEXT_DRAWING_MODE** | 无 | int (当前渲染模式) |
| **SET_SKIA_QUALITY** | int quality, int hinting, int edging | int (1=成功) |
| **GET_SKIA_QUALITY** | int type | int (指定参数的值) |
| **HTML_PRINT font render** | 'gdi' \| 'skia' | 控制该标签内文本的渲染管线 |
| **HTML_PRINT font edging** | 'alias' \| 'antialias' \| 'subpixel' | 控制抗锯齿方式 |
| **HTML_PRINT font hinting** | 'none' \| 'slight' \| 'normal' \| 'full' | 控制字形微调程度 |

## SET_TEXT_DRAWING_MODE / GET_TEXT_DRAWING_MODE

### API

```
SET_TEXT_DRAWING_MODE(mode)
GET_TEXT_DRAWING_MODE()
```

动态切换全局文本渲染管线。

* **mode = 1**：`TEXTRENDERER`，使用 GDI+ (TextRenderer) 渲染
* **mode = 3**：`SKIASHARP`，使用 SkiaSharp 渲染

### Hint

* 该设置影响**全局**文本渲染方式，包括 `PRINT`、`PRINTFORML`、`HTML_PRINT` 等所有文本输出。
* 默认为 `SKIASHARP` (3)。
* 切换渲染管线后，当前已缓存的字体可能需要重新加载才能生效。
* 几乎所有特殊字符在 GDI+ 模式下可能有不同的显示效果。

### Example

```erb
@TEST_RENDER_MODE
    PRINTFORML 当前渲染模式: {GET_TEXT_DRAWING_MODE()}

    ; 切换到 GDI+ 模式
    SET_TEXT_DRAWING_MODE(1)
    PRINTFORML 切换后渲染模式: {GET_TEXT_DRAWING_MODE()}

    HTML_PRINT "<font render='skia'>这段使用SkiaSharp渲染</font>"

    ; 切换回 SkiaSharp 模式
    SET_TEXT_DRAWING_MODE(3)
    HTML_PRINT "<font render='gdi'>这段强制使用GDI+渲染</font>"
```

## SET_SKIA_QUALITY / GET_SKIA_QUALITY

### API

```
SET_SKIA_QUALITY(quality, hinting, edging)
GET_SKIA_QUALITY(type)
```

控制 SkiaSharp 渲染质量参数。所有参数均可省略，省略时保持当前值不变。

**SET_SKIA_QUALITY 参数说明：**

| 参数 | 值域 | 说明 |
| :--- | :--- | :--- |
| quality | 0-3 | 图像质量等级 |
| hinting | 0-3 | 字形微调：0=none, 1=slight, 2=normal, 3=full |
| edging | 0-2 | 抗锯齿方式：0=alias, 1=antialias, 2=subpixel |

**GET_SKIA_QUALITY 参数说明：**

| type | 返回值 |
| :--- | :--- |
| 0 | 当前 ImageQuality 值 |
| 1 | 当前 FontHinting 值 |
| 2 | 当前 FontEdging 值 |

### Hint

* 修改质量设置后，FontFactory 会清除字体缓存，确保下次渲染时使用新参数创建字体。
* 这些设置主要影响 SkiaSharp 渲染管线下的文本显示效果。
* `edging='alias'` 可实现类似早期 Windows 字体的那种锐利像素风格。
* `edging='subpixel'` 提供最佳的视觉平滑效果，但可能不适合某些光栅字体。

### 默认值说明

**全局默认值（emuera.config）：**
| 参数 | 默认值 | 说明 |
| :--- | :--- | :--- |
| ImageQuality | High (3) | 图像质量等级 |
| FontHinting | None (0) | 字形微调 |
| FontEdging | SubpixelAntiAlias (2) | 抗锯齿方式 |

**制表专用字体（rasterFont）特殊规则：**
| 渲染模式 | render未指定 | render='skia' |
| :--- | :--- | :--- |
| MS Gothic MS Mincho | 自动使用GDI+渲染 | 强制SkiaSharp渲染 |
| 非光栅字体 | SkiaSharp渲染 | SkiaSharp渲染 |

当使用 SkiaSharp 渲染光栅字体时，建议配合 `edging='alias'` 以获得像素风效果。

### Example

```erb
@TEST_SKIA_QUALITY
    ; 查询当前质量参数
    PRINTFORML ImageQuality: {GET_SKIA_QUALITY(0)}
    PRINTFORML FontHinting: {GET_SKIA_QUALITY(1)}
    PRINTFORML FontEdging: {GET_SKIA_QUALITY(2)}

    ; 设置为高质量模式
    SET_SKIA_QUALITY(3, 2, 2)
    HTML_PRINT "高质量渲染的文本"

    ; 设置为像素风格（关闭抗锯齿）
    SET_SKIA_QUALITY(3, 0, 0)
    HTML_PRINT "像素风文字渲染"

    ; 只修改抗锯齿方式，保留其他设置
    SET_SKIA_QUALITY(, , 1)
```

## HTML_PRINT font 标签渲染属性扩展

### 概述

在原有 `<font face='...' color='...'> ` 基础上新增以下属性：

| 属性 | 可选值 | 说明 |
| :--- | :--- | :--- |
| render | 'gdi' \| 'skia' | 指定渲染管线，覆盖全局设置 |
| edging | 'alias' \| 'antialias' \| 'subpixel' | 控制该文本的抗锯齿方式 |
| hinting | 'none' \| 'slight' \| 'normal' \| 'full' | 控制字形微调程度 |

### Hint

* 新属性支持**嵌套继承**：内层 `<font>` 未指定的属性会继承外层 `<font>` 的设置。
* 省略 `render` 属性时：
  * **光栅字体**（MS Gothic、MS Mincho、SimHei、SimSun等）：自动使用 GDI+ 渲染
  * **非光栅字体**：使用全局设置的 `SET_TEXT_DRAWING_MODE` 渲染模式
* 省略 `edging` 属性时，默认使用 `Config.FontEdging`（全局设置）。
* `edging='alias'` 可让光栅字体在 SkiaSharp 下呈现像素风格。
* 使用 `<font render='gdi'>` 可以强制特定文本使用 GDI+ 渲染。

### Example

```erb
@TEST_HTML_FONT_ATTRIBUTES
    ; 基础用法：指定字体
    HTML_PRINT "<font face='MS Gothic'>MS Gothic 字体</font>"

    ; 使用 render 属性控制渲染管线
    HTML_PRINT "<font render='gdi' face='MS Gothic'>[♥] GDI+渲染的特殊符号</font>"
    HTML_PRINT "<font render='skia'>SkiaSharp渲染的文本</font>"

    ; 使用 edging 控制抗锯齿
    HTML_PRINT "<font edging='alias'>狗牙像素风文字（关闭抗锯齿）</font>"
    HTML_PRINT "<font edging='subpixel'>完美平滑的文字（次像素抗锯齿）</font>"

    ; 使用 hinting 控制字形微调
    HTML_PRINT "<font hinting='full'>清晰锐利的微小字体</font>"
    HTML_PRINT "<font hinting='none'>无微调的原始渲染</font>"

    ; 组合使用多个属性
    HTML_PRINT "<font render='skia' edging='alias' hinting='full'>SkiaSharp像素风+完整微调</font>"

    ; 嵌套继承示例
    HTML_PRINT "<font edging='subpixel'>外层设置subpixel"
    HTML_PRINT "<font hinting='full'>内层继承外层+覆盖hinting"
    HTML_PRINT "</font></font>"

    PRINTFORML 当前渲染模式: {GET_TEXT_DRAWING_MODE()}
    SET_SKIA_QUALITY(3, 2, 2)
    HTML_PRINT "<font face='MS Gothic' edging='alias'>全局切换后，字体设置仍生效</font>"
```

## 综合应用示例

```erb
@FONT_RENDERING_DEMO
    ; 场景：根据不同内容切换最佳渲染方式

    ; 1. 大段普通文本：使用 SkiaSharp + 抗锯齿，追求流畅度
    SET_TEXT_DRAWING_MODE(3)
    SET_SKIA_QUALITY(3, 1, 2)
    HTML_PRINT "<font face='MS Mincho'>这是大段说明文字，追求阅读体验</font>"

    ; 2. 特殊符号需要精确像素：切换 GDI+ 渲染
    HTML_PRINT "<font render='gdi' face='MS Gothic'>[武器] [防具] [道具]</font>"

    ; 3. UI按钮文字：像素风格
    HTML_PRINT "<font edging='alias' hinting='full' face='MS Gothic'>[确定]</font>"
    HTML_PRINT "<font edging='alias' hinting='full' face='MS Gothic'>[取消]</font>"

    ; 4. Emoji 处理：单色滤镜自动将彩色 Emoji 染成前景色
    HTML_PRINT "状态图标: <font color='0xFF0000'>♥</font> <font color='0x00FF00'>●</font>"

    ; 5. 嵌套组合：外层像素风格，内层大段落继承但hinting不同
    HTML_PRINT "<font edging='alias' hinting='normal'>"
    HTML_PRINT "像素风格段落"
    HTML_PRINT "<font hinting='full'>内层文字更锐利</font>"
    HTML_PRINT "</font>"
```

### Related

[HTML_PRINT](HTML_PRINT.md)
[SETFONT](SETFONT.md)
[STRICT_FONT_FALLBACK](STRICT_FONT_FALLBACK.md)
