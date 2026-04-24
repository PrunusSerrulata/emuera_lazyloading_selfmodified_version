# BitArray 位图操作函数集 ¶

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| **int BITSET** | ref int[] array, int idx (, int val, int length) | 命令/表达式。返回1=成功 |
| **int BITGET** | ref int[] array, int idx | 命令/表达式。返回位值(0/1)，超范围返回-1 |
| **int BITTOGGLE** | ref int[] array, int idx | 命令/表达式。返回1=成功，0=超范围 |
| **int BITINDEXOFFIRST** | ref int[] array (, int val) | 命令/表达式。返回第一个匹配位索引，未找到返回-1 |

### API

``` { #language-erbapi }
int BITSET arrayRef, idxID{, val, length}
int BITGET arrayRef, idxID
int BITTOGGLE arrayRef, idxID
int BITINDEXOFFIRST arrayRef{, val}
```

使用整数数组模拟位图（Bitmap），提供位的设置、读取、翻转和查找功能。

**函数说明：**

1. **BITSET** - 设置位图中指定位置的一个或多个连续位
   - `array`: 位图数组（REF传递，会被修改）
   - `idx`: 起始位索引（从0开始）
   - `val`: 要设置的值（0为清除，1为设置，非0值也视为1），默认为1
   - `length`: 连续设置的位数，默认为1
   - 返回 1 表示操作成功

2. **BITGET** - 读取位图中指定位置的位值
   - `array`: 位图数组
   - `idx`: 要读取的位索引（从0开始）
   - 返回位值（0或1），索引超出范围时返回-1

3. **BITTOGGLE** - 翻转位图中指定位置的位值（0变1，1变0）
   - `array`: 位图数组（REF传递，会被修改）
   - `idx`: 要翻转的位索引（从0开始）
   - 返回 1=成功翻转，0=索引超出范围

4. **BITINDEXOFFIRST** - 查找位图中第一个具有指定值的位的位置
   - `array`: 位图数组
   - `val`: 要查找的位值（0或1，0以外的值都视为1），默认为0
   - 返回第一个匹配位的索引，未找到时返回-1
   - 备注：从低位向高位遍历，返回第一个满足条件的位索引

**重要提示：**

- 位图使用小端序存储，每个数组元素存储64位
- 索引从0开始，超出范围的操作会被忽略
- 所有函数均为 #FUNCTIONS 类型，可在表达式中直接调用
- 使用前需先声明整数数组作为位图的存储容器

### Hint

!!! hint "Hint"

    **命令/表达式。**

    命令语法：
    ```
    BITSET DA, 5
    BITGET DA, 5
    BITTOGGLE DA, 5
    BITINDEXOFFIRST DA, 0
    ```

    表达式语法：
    ```
    LOCAL = BITSET(DA, 5)
    LOCAL = BITGET(DA, 5)
    LOCAL = BITTOGGLE(DA, 5)
    LOCAL = BITINDEXOFFIRST(DA, 0)
    ```

    所有BitArray函数均为命令语法，使用空格分隔参数，也可以作为表达式调用。
    位图使用小端序存储，每个数组元素存储64位。
    索引从0开始，超出范围的操作会被忽略。
### Example

**MAIN.ERB**
```erb
;===============================================================
;BitArray 位图操作测试
;===============================================================

@SYSTEM_BITARRAY_TEST
    ; 1. 创建位图数组（4个元素 = 256位）
    #DIM DYNAMIC BIT_ARRAY, 4

    PRINTFORML "=== BitArray 位图操作测试 ==="

    ; 2. 测试 BITSET - 设置位
    PRINTFORML "BITSET BIT_ARRAY, 5, 1, 1"
    BITSET BIT_ARRAY, 5, 1, 1
    PRINTFORML "设置第5位为1后，BITGET(BIT_ARRAY, 5) = {BITGET(BIT_ARRAY, 5)}"

    ; 3. 测试 BITGET - 读取位
    PRINTFORML ""
    PRINTFORML "BITGET(BIT_ARRAY, 4) = {BITGET(BIT_ARRAY, 4)}"
    PRINTFORML "BITGET(BIT_ARRAY, 5) = {BITGET(BIT_ARRAY, 5)}"
    PRINTFORML "BITGET(BIT_ARRAY, 6) = {BITGET(BIT_ARRAY, 6)}"

    ; 4. 测试 BITTOGGLE - 翻转位
    PRINTFORML ""
    PRINTFORML "翻转第5位：BITTOGGLE BIT_ARRAY, 5"
    BITTOGGLE BIT_ARRAY, 5
    PRINTFORML "BITGET(BIT_ARRAY, 5) = {BITGET(BIT_ARRAY, 5)} (应为0)"

    ; 5. 测试批量设置
    PRINTFORML ""
    PRINTFORML "批量设置第10-15位为1：BITSET BIT_ARRAY, 10, 1, 6"
    BITSET BIT_ARRAY, 10, 1, 6
    FOR LOCAL, 10, 16
        PRINTFORML "BITGET(BIT_ARRAY, {LOCAL}) = {BITGET(BIT_ARRAY, LOCAL)}"
    NEXT

    ; 6. 测试 BITINDEXOFFIRST - 查找第一个1
    PRINTFORML ""
    PRINTFORML "BITINDEXOFFIRST(BIT_ARRAY, 1) = {BITINDEXOFFIRST(BIT_ARRAY, 1)} (查找第一个1)"

    ; 7. 测试清除位
    PRINTFORML ""
    PRINTFORML "清除第5位：BITSET BIT_ARRAY, 5, 0, 1"
    BITSET BIT_ARRAY, 5, 0, 1
    PRINTFORML "BITGET(BIT_ARRAY, 5) = {BITGET(BIT_ARRAY, 5)} (应为0)"

    ; 8. 查找清除后的第一个1
    PRINTFORML ""
    PRINTFORML "BITINDEXOFFIRST(BIT_ARRAY, 1) = {BITINDEXOFFIRST(BIT_ARRAY, 1)} (查找第一个1)"

    ; 9. 测试越界访问
    PRINTFORML ""
    PRINTFORML "测试越界：BITGET(BIT_ARRAY, 300) = {BITGET(BIT_ARRAY, 300)} (超出256位，应返回-1)"

    PRINTFORML ""
    PRINTFORML "=== 测试完成 ==="

    ONEINPUT

```

**Result**
```
=== BitArray 位图操作测试 ===
BITSET BIT_ARRAY, 5, 1, 1
设置第5位为1后，BITGET(BIT_ARRAY, 5) = 1

BITGET(BIT_ARRAY, 4) = 0
BITGET(BIT_ARRAY, 5) = 1
BITGET(BIT_ARRAY, 6) = 0

翻转第5位：BITTOGGLE BIT_ARRAY, 5
BITGET(BIT_ARRAY, 5) = 0 (应为0)

批量设置第10-15位为1：BITSET BIT_ARRAY, 10, 1, 6
BITGET(BIT_ARRAY, 10) = 1
BITGET(BIT_ARRAY, 11) = 1
BITGET(BIT_ARRAY, 12) = 1
BITGET(BIT_ARRAY, 13) = 1
BITGET(BIT_ARRAY, 14) = 1
BITGET(BIT_ARRAY, 15) = 1

BITINDEXOFFIRST(BIT_ARRAY, 1) = 5 (查找第一个1)

清除第5位：BITSET BIT_ARRAY, 5, 0, 1
BITGET(BIT_ARRAY, 5) = 0 (应为0)

BITINDEXOFFIRST(BIT_ARRAY, 1) = 10 (查找第一个1)

测试越界：BITGET(BIT_ARRAY, 300) = -1 (超出256位，应返回-1)

=== 测试完成 ===
```
