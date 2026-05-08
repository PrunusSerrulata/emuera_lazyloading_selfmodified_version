# SELECTCASE 分支语句

| 指令 | 参数 | 返回值 |
| :--- | :--- | :--- |
| `SELECTCASE` | `any` | 无 |
| `CASE` | `any` | 无 |
| `CASEELSE` | 无 | 无 |
| `ENDSELECT` | 无 | 无 |

## 语法

```erabasic
SELECTCASE anyValue
    CASE anyValue(, anyValue...)
    CASEELSE
ENDSELECT
```

分支语句。行为类似 Visual Basic 的同名语法。

与 [`IF`](./IF.md) 语句类似，但 `SELECTCASE` 是根据一个值分派到多个分支的语句。
根据 `SELECTCASE` 指定的参数值进行分支。

## 基本用法

```erabasic
SELECTCASE X
    CASE 1
        PRINTL X是1
    CASE 3
        PRINTL X是3
    CASEELSE
        PRINTL X既不是1也不是3
ENDSELECT
```

当 `SELECTCASE` 语句执行时：

- 如果 `X` 为 `1`，则跳转到 `CASE 1` 行，执行到下一个 `CASE` 或 `CASEELSE` 为止
- 如果 `X` 为 `3`，则跳转到 `CASE 3` 行
- 如果 `X` 的值没有对应的 `CASE`，则跳转到 `CASEELSE`（如果存在），否则跳转到 `ENDSELECT`

!!! warning "与 C 的 switch 不同"
    不会从一个 `CASE` 穿透到下一个 `CASE`（没有 fall-through 行为）。
    也不能使用 [`BREAK`](./CONTINUE.md) 语句跳到 `ENDSELECT`。

## CASE 条件的三种写法

### 1. 直接指定值

```erabasic
CASE 1
CASE 2, 3
CASE "A", "B"
```

### 2. IS 比较表达式

`IS <运算符> <数值表达式>` 格式：

```erabasic
CASE IS <= 30
; 当 X <= 30 时匹配
```

### 3. TO 范围表达式

`<数值表达式> TO <数值表达式>` 格式（左值以上、右值以下）：

```erabasic
CASE 10 TO 20
; 当 10 <= X <= 20 时匹配
```

!!! warning "TO 的方向"
    `TO` 是"左值以上、右值以下"。如果右值小于左值，该 `CASE` 永远不会执行。

## 综合示例

```erabasic
SELECTCASE X
    CASE 1
        PRINTL X是1
    CASE 2, 3
        PRINTL X不是1
        PRINTL X是2或3
    CASE 10 TO 20
        PRINTL X不是1、2或3
        PRINTL X在10到20之间
    CASE IS <= 30
        PRINTL X不是1、2、3、也不是10到20
        PRINTL X在30以下
    CASE 40, 5 * 10 TO 6 * 10, IS >= 10 * 10
        PRINTL X不在30以下
        PRINTL X是40、50到60之间、或100以上
    CASEELSE
        PRINTL X不满足以上任何条件
ENDSELECT
```

## 注意事项

### IS 和 TO 的语法限制

- `IS` 必须写成 `IS <运算符> <表达式>` 的形式，不能写成 `30 < IS`
- `TO` 必须写成 `<表达式> TO <表达式>` 的形式，不能写成 `(10 TO 20) || (30 TO 40)`

### 短路求值

一个 `CASE` 中有多个条件时，从左到右依次检查，一旦找到满足条件的，剩余条件不再求值。

### 字符串模式

`SELECTCASE` 的参数也可以使用字符串表达式。此时 `CASE` 的条件也必须是字符串表达式：

```erabasic
SELECTCASE NAME
    CASE "Alice"
        PRINTL 是Alice
    CASE "Bob", "Charlie"
        PRINTL 是Bob或Charlie
    CASEELSE
        PRINTL 其他人
ENDSELECT
```

### GOTO 跳入的行为

如果通过 [`GOTO`](./GOTO.md) 等命令直接跳入 `SELECTCASE～CASE～CASEELSE～ENDSELECT` 内部，
与 [`IF～ENDIF`](./IF.md) 相同，会正常执行到 `CASE`、`CASEELSE` 或 `ENDSELECT` 的前一行，
然后跳到 `ENDSELECT` 的下一行继续执行。

## LazyLoading 扩展：编译期跳转表优化

当 `SELECTCASE` 满足以下条件时，编译器会自动构建跳转表，将运行时匹配从 O(n) 线性扫描优化为 O(1) 哈希查找：

**优化条件**：
- `SELECTCASE` 参数为整数型、字符串型或浮点型
- 所有 `CASE` 条件都是直接常量值（不含 `TO`、`IS`、非常量表达式）
- 各 `CASE` 的常量值不重复

**不优化的情况**（自动退回线性扫描）：
- 包含 `TO` 范围条件
- 包含 `IS` 比较条件
- `CASE` 中包含非常量表达式（如变量引用或计算式）
- 存在重复的常量值

```erabasic
; 会被优化（O(1) 跳转表）
SELECTCASE X
    CASE 1
        ; ...
    CASE 2
        ; ...
    CASE 3
        ; ...
ENDSELECT

; 不会被优化（包含 TO，退回 O(n) 线性扫描）
SELECTCASE X
    CASE 1 TO 10
        ; ...
    CASE IS > 20
        ; ...
ENDSELECT
```

此优化对 ERB 脚本完全透明，无需修改任何代码即可获得性能提升。

## 相关项目

- [IF-ENDIF](./IF.md)
- [PRINTDATA](./PRINTDATA.md)
