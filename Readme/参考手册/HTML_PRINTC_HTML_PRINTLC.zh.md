# HTML_PRINTC, HTML_PRINTLC ¶

| 命令名称 | 参数 | 返回值 |
| :--- | :--- | :--- |
| **HTML_PRINTC** | string, (int) | 命令专用。无返回值 |
| **HTML_PRINTLC** | string, (int) | 命令专用。无返回值 |

### API

``` { #language-erbapi }
HTML_PRINTC htmlString{, cellWidthPx}
HTML_PRINTLC htmlString{, cellWidthPx}
```

基于像素的 HTML 制表命令。与原版 `PRINTC` / `PRINTLC` 类似，但使用像素宽度而非半角字符数来计算制表填充，彻底解决非等距字体、FONTSTYLE、SETFONT 等操作导致的制表错位问题。

**参数说明：**

| 参数 | 类型 | 省略 | 说明 |
| :--- | :--- | :--- | :--- |
| htmlString | string | 不可 | HTML 字符串，支持 HTML_PRINT 的所有标签（`<b>` `<i>` `<font>` `<button>` 等） |
| cellWidthPx | int | 可 | 制表宽度（像素）。省略时使用 `PrintCLength × FontSize / 2` 作为默认宽度 |

### Hint

!!! hint "Hint"

    **命令专用。**

    命令语法：
    ```
    HTML_PRINTC @"<b>标题</b>", 200
    HTML_PRINTLC @"内容", 300
    ```

* **HTML_PRINTC**：右对齐制表。内容靠右，左侧填充像素空格。
* **HTML_PRINTLC**：左对齐制表。内容靠左，右侧填充像素空格。
* 两个命令共享同一制表逻辑，仅对齐方向不同。
* **自动换行**：当当前行已占用宽度 + 制表宽度超过 `DrawableWidth`（即窗口可绘制宽度，已扣除边框 padding）时，自动 Flush 当前行并换行。
* **省略第二参数**：使用 `Config.PrintCLength × Config.FontSize / 2` 作为默认制表宽度，与原版 PRINTC 的字符宽度语义对齐。
* HTML 字符串内可自由使用 `<font>` `<b>` `<i>` `<button>` 等所有 HTML_PRINT 支持的标签，制表计算会精确测量包含样式后的实际像素宽度。
* 连续调用 `HTML_PRINTC` / `HTML_PRINTLC` 时，内容会依次排列在同一行中，直到行宽不足时自动换行——无需手动管理缓冲区和 FLUSH。
* 使用 `PRINTL` 或 `PRINTW` 等命令可手动换行，结束当前制表行。

### 与 PRINTC / PRINTLC 的对比

| 特性 | PRINTC / PRINTLC | HTML_PRINTC / HTML_PRINTLC |
| :--- | :--- | :--- |
| 填充计算方式 | 半角字符数 | 像素宽度 |
| 对齐精度 | 仅等距字体精确 | 所有字体精确 |
| FONTSTYLE 影响 | 破坏制表 | 不受影响 |
| SETFONT 影响 | 破坏制表 | 不受影响 |
| HTML 标签支持 | 不支持 | 完整支持 |
| 按钮支持 | 需 PRINTBUTTON | 原生 `<button>` 标签 |
| 边框 padding | 不考虑 | 自动考虑（使用 DrawableWidth） |

### Related

[HTML_PRINT](HTML_PRINT.md)
[PRINTC](PRINTC.md)
[PRINTLC](PRINTLC.md)
[SETFONT](SETFONT.md)
[FONTSTYLE](FONTSTYLE.md)
