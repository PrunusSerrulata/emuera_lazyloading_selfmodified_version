---
hide:
  - toc
---

# CBGSETSPRITE / CBGSETCIMG

| 関数名                                                                   | 引数                                                                                           | 戻り値 |
| :----------------------------------------------------------------------- | :--------------------------------------------------------------------------------------------- | :----- |
| ![](../assets/images/IconEmuera.webp)[`CBGSETSPRITE`](./CBGSETSPRITE.zh.md) | `str`, `int`, `int`, `int`, `int`, `int`, `int`, `var`                                         | `int`  |
| ![](../assets/images/IconEmuera.webp)[`CBGSETCIMG`](./CBGSETSPRITE.zh.md)   | `str`, `int`, `int`, `int`, `int`, `int`, `int`, `var`                                         | `int`  |

!!! info "API"

    ``` { #language-erbapi }
    int CBGSETSPRITE imgName(, x, y, zDepth, width, height, opacity, colorMatrix)
    int CBGSETCIMG imgName(, x, y, zDepth, width, height, opacity, colorMatrix)
    ```

    将指定 `imgName` 的 Sprite 设置为客户端区域的背景图层。`CBGSETSPRITE` 与 `CBGSETCIMG` 功能完全相同，为同一函数的两个别名。

    **参数**（第 2 个参数起均可省略）：

    | 参数 | 类型 | 默认值 | 说明 |
    | :--- | :--- | :----- | :--- |
    | `imgName` | str | — | Sprite 名称（不可省略） |
    | `x` | int | `0` | 绘制 X 坐标 |
    | `y` | int | `0` | 绘制 Y 坐标 |
    | `zDepth` | int | `1` | 层深度（不可为 0）。正值在文本后方，负值在文本前方 |
    | `width` | int | `0` | 目标宽度。0 表示使用 Sprite 原始宽度 |
    | `height` | int | `0` | 目标高度。0 表示使用 Sprite 原始高度 |
    | `opacity` | int | `255` | 不透明度（0～255）。255 为完全不透明 |
    | `colorMatrix` | var | `null` | 5×5 颜色矩阵数组引用（如 `CM_GRAY:0:0`）。省略则不应用颜色变换 |

    **返回值**：成功返回 1，失败（Sprite 不存在）返回 0。

    !!! warning "注意"
        - `zDepth` 不可为 0。正常文本绘制对应 `zDepth == 0`。
        - `x` 和 `y` 正方向：x 向右，y 向下。均为 0 时图像左下角与客户端区域左下角对齐。
        - `opacity` 为整数 0～255，引擎内部会除以 255 转为浮点。
        - `colorMatrix` 参数传递二维/三维整数数组的起始地址（如 `CM:0:0` 或 `CM_PRESET:0:0:0`），引擎读取 5×5 子矩阵并除以 256 转为浮点。详见 [ColorMatrix 颜色矩阵详解](./Graphics绘图系API.zh.md#colormatrix-颜色矩阵详解)。
        - WINAPI 模式不支持此命令。

!!! hint "ヒント"

    命令、式中関数両方対応しています。

!!! example "例"

    ``` { #language-erb title="MAIN.ERB" }
    @SYSTEM_TITLE
        ; 基本渲染：在 (0,0) 位置以 depth=1 显示 Sprite
        CBGSETSPRITE "pet_1", 0, 0, 1

        ; 缩放 + 透明度：缩放到 200x200，约 78% 不透明
        CBGSETSPRITE "pet_2", 100, 50, 2, 200, 200, 200

        ; 颜色矩阵：灰度效果
        #DIM CM_GRAY, 5, 5
        CM_GRAY:0:0 =  77, 150,  29,   0,   0
        CM_GRAY:1:0 =  77, 150,  29,   0,   0
        CM_GRAY:2:0 =  77, 150,  29,   0,   0
        CM_GRAY:3:0 =   0,   0,   0, 256,   0
        CM_GRAY:4:0 =   0,   0,   0,   0, 256
        CBGSETSPRITE "pet_3", 300, 50, 3, 150, 150, 255, CM_GRAY:0:0

        ; 作为表达式调用
        RESULT = CBGSETSPRITE("pet_1", 0, 0, 1)
        PRINTVL RESULT
    ```
    ``` title="結果"
    1
    ```
