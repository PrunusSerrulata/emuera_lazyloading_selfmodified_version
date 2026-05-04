# Graphics 绘图系 API 参考手册

本文档涵盖 Emuera 的 Graphics（G系）绘图函数，包括图像创建、绘制、颜色矩阵变换及背景图设置等。

---

## 概览

| 命令/函数 | 参数 | 返回值 | 说明 |
| :--- | :--- | :--- | :--- |
| **GCREATE** | gID, width, height | int | 创建空白 Graphics |
| **GCREATED** | gID | int | 检查 Graphics 是否存在 |
| **GCREATEFROMFILE** | gID, filePath | int | 从文件创建 Graphics |
| **GDISPOSE** | gID | int | 释放 Graphics |
| **GCLEAR** | gID, cARGB | int | 用颜色填充整个 Graphics |
| **GDRAWSPRITE** | gID, spriteName(, ...) | int | 将 Sprite 绘制到 Graphics |
| **GDRAWG** | destID, srcID, ... | int | 将 Graphics 绘制到 Graphics |
| **GDRAWGWITHMASK** | destID, srcID, maskID, destX, destY | int | 带遮罩绘制 |
| **GDRAWGWITHROTATE** | gID, destID, angle(, x, y) | int | 旋转绘制 |
| **SPRITECREATE** | spriteName, gID(, x, y, w, h) | int | 从 Graphics 创建 Sprite |
| **SPRITEDISPOSE** | spriteName | int | 释放 Sprite |
| **CBGSETG** | gID, x, y, zDepth | int | 设置 Graphics 为背景层 |
| **CBGSETSPRITE** | spriteName, x, y, zDepth | int | 设置 Sprite 为背景层 |
| **CBGCLEAR** | none | int | 清除所有背景层 |
| **SETBGIMAGE** | resourceName(, depth, opacity) | none | 设置资源图片为背景 |
| **CLEARBGIMAGE** | none | none | 清除所有背景图片 |
| **REMOVEBGIMAGE** | resourceName | none | 移除指定背景图片 |

---

## GCREATE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`GCREATE`](./GCREATE.md) | `int`, `int`, `int` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int GCREATE gID, width, height
    ```

    创建指定 `gID` 和尺寸的 Graphics。`gID` 必须为非负整数，`width` 和 `height` 必须为 1～8192 之间的整数。参数超出范围会报错。成功返回非零值。若指定 `gID` 的 Graphics 已存在则返回 0。如需重新创建，请先用 [`GDISPOSE`](#gdispose) 释放。

!!! hint "Hint"

    命令/表达式。

---

## GCREATED

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`GCREATED`](./GCREATED.md) | `int` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int GCREATED gID
    ```

    返回指定 `gID` 的 Graphics 是否已创建（1=已创建，0=未创建或已释放）。

!!! hint "Hint"

    命令/表达式。

---

## GCREATEFROMFILE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`GCREATEFROMFILE`](./GCREATEFROMFILE.md) | `int`, `string` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int GCREATEFROMFILE gID, filePath
    ```

    从 `resources` 文件夹以相对路径打开图片文件并创建 Graphics。与 CSV 声明的图片不同，文件不会被锁定。成功返回非零值。若 `gID` 已存在、文件不存在、无法识别为图片或文件过大，均返回 0。

!!! hint "Hint"

    命令/表达式。

---

## GDISPOSE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`GDISPOSE`](./GDISPOSE.md) | `int` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int GDISPOSE gID
    ```

    释放指定 `gID` 的 Graphics。成功返回非零值。若 Graphics 不存在（含已释放）则返回 0。

!!! hint "Hint"

    命令/表达式。

---

## GCLEAR

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`GCLEAR`](./GCLEAR.md) | `int`, `int` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int GCLEAR gID, cARGB
    ```

    用指定颜色填充整个 Graphics。颜色为 0xAARRGGBB 格式的整数。成功返回非零值。

!!! hint "Hint"

    命令/表达式。

---

## GDRAWSPRITE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`GDRAWSPRITE`](./GDRAWSPRITE.md) | `int`, `string` | `int` |
| | `int`, `string`, `int`, `int` | `int` |
| | `int`, `string`, `int`, `int`, `int`, `int` | `int` |
| | `int`, `string`, `int`, `int`, `int`, `int`, `integerVariable` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int GDRAWSPRITE gID, spriteName
    int GDRAWSPRITE gID, spriteName, destX, destY
    int GDRAWSPRITE gID, spriteName, destX, destY, destWidth, destHeight
    int GDRAWSPRITE gID, spriteName, destX, destY, destWidth, destHeight, colorMatrix
    ```

    将指定 `spriteName` 的 Sprite 绘制到指定 `gID` 的 Graphics 上。

    - 省略位置参数时，绘制到 (0, 0) 位置，使用 Sprite 原始尺寸。
    - 指定 `destX, destY` 可设置绘制位置。
    - 指定 `destWidth, destHeight` 可缩放绘制。
    - 指定 `colorMatrix`（5×5 二维整数数组变量）可在绘制时应用颜色矩阵变换。

    Sprite 尺寸可通过 `SPRITEWIDTH(str)` / `SPRITEHEIGHT(str)` 获取。成功返回非零值。

    若指定动画 Sprite，则绘制其中一帧。

!!! hint "Hint"

    命令/表达式。

---

## GDRAWG

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`GDRAWG`](./GDRAWG.md) | `int`, `int`, `int`, `int`, `int`, `int`, `int`, `int`, `int`, `int` | `int` |
| | `int`, `int`, `int`, `int`, `int`, `int`, `int`, `int`, `int`, `int`, `integerVariable` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int GDRAWG destID, srcID, destX, destY, destWidth, destHeight, srcX, srcY, srcWidth, srcHeight
    int GDRAWG destID, srcID, destX, destY, destWidth, destHeight, srcX, srcY, srcWidth, srcHeight, colorMatrix
    ```

    将 `srcID` 的 Graphics 绘制到 `destID` 的 Graphics 上。用 4 个整数指定目标位置和尺寸（dest），另外 4 个指定源位置和尺寸（src）。

    可选指定 `colorMatrix`（5×5 二维整数数组变量）在绘制时应用颜色矩阵变换。

    成功返回非零值。若源或目标 Graphics 不存在则返回 0。源和目标可以为同一个 Graphics。

!!! hint "Hint"

    命令/表达式。

---

## GDRAWGWITHMASK

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`GDRAWGWITHMASK`](./GDRAWGWITHMASK.md) | `int`, `int`, `int`, `int`, `int` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    GDRAWGWITHMASK destID, srcID, maskID, destX, destY
    ```

    将 `srcID` 的 Graphics 绘制到 `destID` 上，以 `maskID` 的 Graphics 作为遮罩。`destX, destY` 指定绘制位置。

    遮罩的含义：将遮罩图像的蓝色通道值作为源图像的不透明度。例如遮罩全白（蓝色通道为最大值）时等同于无遮罩；遮罩全黑（蓝色通道为 0）时源图像完全透明。

    成功条件：`srcID` 和 `maskID` 的宽高必须完全一致，且绘制区域不能超出 `destID` 的边界。

    !!! warning "注意"
        此命令由 CPU 单线程处理，不要期望高性能。

!!! hint "Hint"

    命令/表达式。

---

## GDRAWGWITHROTATE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEE.webp)[`GDRAWGWITHROTATE`](./GDRAWGWITHROTATE.md) | `int`, `int`, `int`(, `int`, `int`) | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int GDRAWGWITHROTATE gID, destID, Angle(, x, y)
    ```

    将 `destID` 的图像旋转指定 `Angle` 度后绘制到 `gID` 上。`x, y` 指定旋转中心，省略时为图像中心点。

!!! hint "Hint"

    命令/表达式。

---

## SPRITECREATE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`SPRITECREATE`](./SPRITECREATE.md) | `string`, `int` | `int` |
| | `string`, `int`, `int`, `int`, `int`, `int` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int SPRITECREATE spriteName, gID
    int SPRITECREATE spriteName, gID, x, y, width, height
    ```

    从指定 `gID` 的 Graphics 的全部或部分区域创建名为 `spriteName` 的 Sprite。成功返回非零值。若同名 Sprite 已存在或创建失败则返回 0。

    Sprite 仅记录父 Graphics 的 `gID` 和裁剪位置，因此修改父 Graphics 也会影响 Sprite。若父 Graphics 被释放，Sprite 也视为已释放。

    创建的 Sprite 可像 CSV 资源声明的 Sprite 一样使用，例如 [`PRINT_IMG`](./PRINT_IMG.md) 或 [`HTML_PRINT` img 标签](../Emuera/HTML_PRINT.md#img)。

!!! hint "Hint"

    命令/表达式。

---

## SPRITEDISPOSE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`SPRITEDISPOSE`](./SPRITEDISPOSE.md) | `string` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int SPRITEDISPOSE spriteName
    ```

    释放指定名称的 Sprite。成功返回非零值。

!!! hint "Hint"

    命令/表达式。

---

## CBGSETG

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`CBGSETG`](./CBGSETG.md) | `int`, `int`, `int`, `int` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int CBGSETG gID, x, y, zDepth
    ```

    将指定 `gID` 的 Graphics 设置为客户端区域的背景图。`x` 和 `y` 均为 0 时，图像左下角与客户端区域左下角对齐。`x` 正方向向右，`y` 正方向向下。`zDepth` 必须为非零值，正常文本绘制对应 `zDepth==0`；`zDepth` 为负值时绘制在文本前方。

!!! hint "Hint"

    命令/表达式。

---

## CBGSETSPRITE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`CBGSETSPRITE`](./CBGSETSPRITE.md) | `string`, `int`, `int`, `int` | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int CBGSETSPRITE spriteName, x, y, zDepth
    ```

    将指定 `spriteName` 的 Sprite 设置为客户端区域的背景图。坐标与深度规则同 [`CBGSETG`](#cbgsetg)。

!!! hint "Hint"

    命令/表达式。

---

## CBGCLEAR

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/IconEmuera.webp)[`CBGCLEAR`](./CBGCLEAR.md) | none | `int` |

!!! info "API"

    ``` { #language-erbapi }
    int CBGCLEAR
    ```

    清除所有由 CBG 命令设置的背景图像。

!!! hint "Hint"

    命令/表达式。

---

## SETBGIMAGE / CLEARBGIMAGE / REMOVEBGIMAGE

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| ![](../assets/images/Iconetc.webp)[`SETBGIMAGE`](./BACKGROUND.md) | `string`(, `int`, `int`) | None |
| ![](../assets/images/Iconetc.webp)[`REMOVEBGIMAGE`](./BACKGROUND.md) | `string` | None |
| ![](../assets/images/Iconetc.webp)[`CLEARBGIMAGE`](./BACKGROUND.md) | None | None |

!!! info "API"

    ``` { #language-erbapi }
    SETBGIMAGE resourceName(, depth, opacity)
    REMOVEBGIMAGE resourceName
    CLEARBGIMAGE
    ```

    - **SETBGIMAGE**：将 CSV 资源中定义的图片设置为背景。`depth` 用于图层排序（默认 0，-1 在 0 前方）。`opacity` 为 0～255 的不透明度。
    - **REMOVEBGIMAGE**：移除指定资源名的背景图。
    - **CLEARBGIMAGE**：清除所有背景图。

    !!! warning "注意"
        仅支持命令语法，不支持表达式。WINAPI 模式不支持。背景图会自动缩放以适应控制台窗口并保持宽高比。

!!! hint "Hint"

    仅命令语法可用。

---

## ColorMatrix 颜色矩阵详解

`GDRAWSPRITE` 和 `GDRAWG` 的最后一个可选参数 `colorMatrix` 允许在绘制时应用 5×5 颜色矩阵变换。这是实现着色、亮度调节、透明度控制、灰度化等图像特效的核心机制。

### 矩阵格式

`colorMatrix` 参数接受一个 **5×5 的二维整数数组变量**（`#DIM` 声明的二维数组），所有元素在传入渲染引擎前会被**除以 256**。即：**对角线全为 256 的矩阵等同于单位矩阵（不改变图像）**。

```erb
#DIM CM, 5, 5
```

### 矩阵布局与运算规则

矩阵按**行优先**排列，每行对应一个输出通道：

```
         输入 →  R      G      B      A     偏移
输出 ↓
R'   =  [m00    m01    m02    m03    m04]   ← 行0 (R输出)
G'   =  [m10    m11    m12    m13    m14]   ← 行1 (G输出)
B'   =  [m20    m21    m22    m23    m24]   ← 行2 (B输出)
A'   =  [m30    m31    m32    m33    m34]   ← 行3 (A输出)
齐次 =  [m40    m41    m42    m43    m44]   ← 行4 (齐次行)
```

变换公式：

```
R' = (m00×R + m01×G + m02×B + m03×A) / 256 + m04 / 256
G' = (m10×R + m11×G + m12×B + m13×A) / 256 + m14 / 256
B' = (m20×R + m21×G + m22×B + m23×A) / 256 + m24 / 256
A' = (m30×R + m31×G + m32×B + m33×A) / 256 + m34 / 256
```

其中 R, G, B, A 的输入值范围为 0～255，输出值会被钳制到 0～255。

### ERB 中的写法

在 ERB 中，矩阵以二维数组 `CM:行:列` 的形式赋值，传递时使用起始地址 `CM:0:0`：

```erb
#DIM CM, 5, 5

; 单位矩阵（不改变图像）
CM:0:0 = 256,   0,   0,   0,   0  ; 行0: R输出
CM:1:0 =   0, 256,   0,   0,   0  ; 行1: G输出
CM:2:0 =   0,   0, 256,   0,   0  ; 行2: B输出
CM:3:0 =   0,   0,   0, 256,   0  ; 行3: A输出
CM:4:0 =   0,   0,   0,   0, 256  ; 行4: 齐次行

GDRAWSPRITE destGID, "spriteName", 0, 0, 256, 256, CM:0:0
```


### 三维数组写法

当需要存储多套矩阵预设时，可使用三维数组：

```erb
#DIM CM_PRESET, 10, 5, 5

; 预设0：单位矩阵
CM_PRESET:0:0:0 = 256,   0,   0,   0,   0
CM_PRESET:0:1:0 =   0, 256,   0,   0,   0
CM_PRESET:0:2:0 =   0,   0, 256,   0,   0
CM_PRESET:0:3:0 =   0,   0,   0, 256,   0
CM_PRESET:0:4:0 =   0,   0,   0,   0, 256

; 使用预设0
GDRAWSPRITE destGID, "spriteName", 0, 0, 256, 256, CM_PRESET:0:0:0
```

### 常用矩阵预设

#### 1. 单位矩阵（无变换）

```erb
CM:0:0 = 256,   0,   0,   0,   0
CM:1:0 =   0, 256,   0,   0,   0
CM:2:0 =   0,   0, 256,   0,   0
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256
```

#### 2. 灰度化

将 RGB 各通道设为 R×0.299 + G×0.587 + B×0.114（标准亮度权重）：

```erb
; 0.299×256≈77, 0.587×256≈150, 0.114×256≈29
CM:0:0 =  77, 150,  29,   0,   0
CM:1:0 =  77, 150,  29,   0,   0
CM:2:0 =  77, 150,  29,   0,   0
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256
```

#### 3. 亮度调节（偏移量）

利用第5列（偏移列）增加/减少亮度。正值变亮，负值变暗：

```erb
; 整体变亮（偏移 +64/256 ≈ +0.25）
CM:0:0 = 256,   0,   0,   0,  64
CM:1:0 =   0, 256,   0,   0,  64
CM:2:0 =   0,   0, 256,   0,  64
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256

; 整体变暗（偏移 -64/256 ≈ -0.25）
CM:0:0 = 256,   0,   0,   0, -64
CM:1:0 =   0, 256,   0,   0, -64
CM:2:0 =   0,   0, 256,   0, -64
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256
```

#### 4. 透明度调节

修改 A 通道的对角线值（行3列3）：

```erb
; 半透明（Alpha × 128/256 = 0.5）
CM:0:0 = 256,   0,   0,   0,   0
CM:1:0 =   0, 256,   0,   0,   0
CM:2:0 =   0,   0, 256,   0,   0
CM:3:0 =   0,   0,   0, 128,   0
CM:4:0 =   0,   0,   0,   0, 256
```

#### 5. 夜间效果（冷色调变暗）

降低 R 和 G 通道，保留 B 通道：

```erb
; R×90/256≈0.35, G×90/256≈0.35, B×200/256≈0.78
CM:0:0 =  90,   0,   0,   0,   0
CM:1:0 =   0,  90,   0,   0,   0
CM:2:0 =   0,   0, 200,   0,   0
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256
```

#### 6. 颜色着色（Color Tinting）

将图像染成指定颜色。以目标颜色的 RGB 分量作为权重，将原图灰度值映射到目标色：

```erb
; 着色为目标色 (RED, GREEN, BLUE)，值范围 0～256
; 假设目标色为紫色 (196, 0, 196)
CM:0:0 = 196, 196,   0,   0,   0  ; R' = 196×(R+G)/256
CM:1:0 =   0, 196, 196,   0,   0  ; G' = 196×(G+B)/256
CM:2:0 = 196,   0, 196,   0,   0  ; B' = 196×(R+B)/256
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256
```

#### 7. 通道交换

通过重新排列对角线位置实现通道交换：

```erb
; R↔B 交换（红蓝互换）
CM:0:0 =   0,   0, 256,   0,   0  ; R' = B
CM:1:0 =   0, 256,   0,   0,   0  ; G' = G
CM:2:0 = 256,   0,   0,   0,   0  ; B' = R
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256
```

#### 8. 反色

将对角线设为 0，偏移设为 256（即 255/256 ≈ 1.0）：

```erb
CM:0:0 =   0,   0,   0,   0, 256
CM:1:0 =   0,   0,   0,   0, 256
CM:2:0 =   0,   0,   0,   0, 256
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256
```

#### 9. 肤色变换

通过缩放 RGB 对角线实现肤色调整：

```erb
; 褐色皮肤 (R×0.9, G×0.7, B×0.7)
CM:0:0 = 231,   0,   0,   0,   0  ; 0.9×256≈231
CM:1:0 =   0, 180,   0,   0,   0  ; 0.7×256≈180
CM:2:0 =   0,   0, 180,   0,   0  ; 0.7×256≈180
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256

; 晒黑皮肤 (R×1.2, G×0.95, B×0.95)
CM:0:0 = 308,   0,   0,   0,   0  ; 1.2×256≈308
CM:1:0 =   0, 244,   0,   0,   0  ; 0.95×256≈244
CM:2:0 =   0,   0, 244,   0,   0  ; 0.95×256≈244
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256
```

### 矩阵运算原理

颜色矩阵本质是一个仿射变换，将输入像素 `[R, G, B, A]` 映射为输出像素 `[R', G', B', A']`：

```
[R']   [m00 m01 m02 m03 m04] [R]
[G'] = [m10 m11 m12 m13 m14] [G]
[B']   [m20 m21 m22 m23 m24] [B]
[A']   [m30 m31 m32 m33 m34] [A]
                           [1]
```

第5行（齐次行）在标准用法中为 `[0, 0, 0, 0, 256]`，通常不需要修改。

**关键规则：**

- 所有值以 **256 为基准**：256 等同于 1.0（100%），128 等同于 0.5（50%）
- 值可以为负数（如偏移列中使用负值来降低亮度）
- 值可以超过 256（如 308 表示 1.2 倍增益，用于增亮）
- 输出值会被自动钳制到 0～255 范围

### 典型工作流：创建着色 Sprite

以下是一个完整的着色 Sprite 创建流程，展示了从 Graphics 创建到应用 ColorMatrix 再到注册 Sprite 的全过程：

```erb
@CREATE_TINTED_SPRITE(SPR_NAME, BASE_SPR, TINT_HEX)
#DIMS SPR_NAME
#DIMS BASE_SPR
#DIM TINT_HEX
#DIM G_ID
#DIM CM, 5, 5
#DIM RED
#DIM GREEN
#DIM BLUE

IF !SPRITECREATED(BASE_SPR)
    RETURN 0
ENDIF

; 从十六进制颜色提取 RGB
BLUE  = TINT_HEX % 256
TINT_HEX = TINT_HEX / 256
GREEN = TINT_HEX % 256
TINT_HEX = TINT_HEX / 256
RED   = TINT_HEX % 256

; 构建着色矩阵
VARSET CM
CM:0:0 = RED, GREEN, BLUE, 0, 0
CM:1:0 = 256 - 2*RED, 256 - 2*GREEN, 256 - 2*BLUE, 0, 0
CM:2:0 = RED, GREEN, BLUE, 0, 0
CM:3:0 = 0, 0, 0, 256, 0
CM:4:0 = 0, 0, 0, 0, 256

; 创建空白 Graphics
G_ID = NEXT_GID
WHILE GCREATED(G_ID)
    G_ID ++
WEND
NEXT_GID = G_ID + 1
GCREATE G_ID, SPRITEWIDTH(BASE_SPR), SPRITEHEIGHT(BASE_SPR)

; 绘制并应用颜色矩阵
GDRAWSPRITE G_ID, BASE_SPR, 0, 0, SPRITEWIDTH(BASE_SPR), SPRITEHEIGHT(BASE_SPR), CM:0:0

; 注册为 Sprite
SPRITECREATE SPR_NAME, G_ID

RETURN 1
```

### 典型工作流：CBG 背景图

```erb
@SETUP_BACKGROUND
#DIM G_ID
#DIM CM, 5, 5

; 创建 Graphics
G_ID = 0
GCREATE G_ID, 800, 600

; 设置夜间效果矩阵
CM:0:0 =  90,   0,   0,   0,   0
CM:1:0 =   0,  90,   0,   0,   0
CM:2:0 =   0,   0, 200,   0,   0
CM:3:0 =   0,   0,   0, 256,   0
CM:4:0 =   0,   0,   0,   0, 256

; 绘制带颜色矩阵的精灵到 Graphics
GDRAWSPRITE G_ID, "background", 0, 0, 800, 600, CM:0:0

; 注册为 Sprite 并设为背景
SPRITECREATE "bg_night", G_ID
CBGSETSPRITE "bg_night", 0, 0, -1
```

---

## 渲染引擎说明

当前版本使用 **SkiaSharp** 作为图形渲染引擎（替代了原版 GDI+）。所有 G 系函数在 WINAPI 模式下不可用。

ColorMatrix 的内部实现已针对 SkiaSharp 适配：

- ERB 中的 256 基准值在传入渲染引擎前会除以 256，转换为 0.0～1.0 范围的浮点数
- 偏移列（第5列）的值会自动从 GDI+ 的归一化空间 (0.0～1.0) 转换为 SkiaSharp 的非归一化空间 (0～255)
- 矩阵布局保持行优先，与 GDI+ ColorMatrix 完全一致

**ERB 脚本无需因渲染引擎变更而修改**——所有 ColorMatrix 的写法与原版 Emuera 完全兼容。
