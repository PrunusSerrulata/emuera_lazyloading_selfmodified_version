# CALLSTR, JUMPSTR, TRYCALLSTR, TRYJUMPSTR, TRYCCALLSTR, TRYCJUMPSTR ¶

| 命令名称 | 参数 | 返回值 |
| :--- | :--- | :--- |
| **CALLSTR** | stringVariable | 命令专用。无返回值 |
| **JUMPSTR** | stringVariable | 命令专用。无返回值 |
| **TRYCALLSTR** | stringVariable | 命令专用。无返回值 |
| **TRYJUMPSTR** | stringVariable | 命令专用。无返回值 |
| **TRYCCALLSTR** | stringVariable | 命令专用。无返回值 |
| **TRYCJUMPSTR** | stringVariable | 命令专用。无返回值 |

### API

``` { #language-erbapi }
CALLSTR stringVariable
JUMPSTR stringVariable
TRYCALLSTR stringVariable
TRYJUMPSTR stringVariable
TRYCCALLSTR stringVariable
TRYCJUMPSTR stringVariable
```

与 `CALL`、`JUMP` 及其 `TRY` 系统类似，但允许通过一个**字符串表达式**来指定**完整的函数调用行**（包括函数名和参数）。

与 `CALLFORM` 的区别在于：`CALLFORM` 的参数结构在编译时必须固定，而 `CALLSTR` 系列是在运行时解析整个字符串。这意味着可以动态地改变传递给函数的参数数量或类型。

### Hint

!!! hint "Hint"

    **命令专用。**

    所有CALLSTR系列函数均为命令语法，使用空格分隔参数，不能在表达式中调用。

* 该命令支持以下两种解析格式：
  1. **函数式写法**：`"FUNC_NAME(ARG1, ARG2)"`
  2. **逗号分隔写法**：`"FUNC_NAME, ARG1, ARG2"`
* **TRYCALLSTR / TRYJUMPSTR**：如果字符串中指定的函数名不存在，程序不会报错，而是直接跳过执行。
* **TRYCCALLSTR / TRYCJUMPSTR**：属于 `TRYC` 系统，如果指定的函数存在则执行；如果不存在，则执行随后的 `CATCH` 分句。
* **参数解析**：字符串内的参数会根据当前的执行上下文进行解析。例如，如果字符串为 `"MY_FUNC(LOCAL)"`，则在执行时会读取当前函数内 `LOCAL` 变量的值。
* 由于该命令涉及运行时的词法分析与语法解析，其执行效率略低于静态的 `CALL` 或 `CALLFORM`。在对性能要求极高的超大型循环中应谨慎使用。
* 字符串内的语法错误（如括号不匹配）将在运行时触发 `CodeEE` 报错。
* 接受INPUTS系指令的RESULTS作为参数时需要注意，由控制台输入的部分字符需要'\\'转义，这就包括小括号。
### Example

**MAIN.ERB**
```erb
@SYSTEM_TITLE
    #DIMS DYNAMIC COMMAND
    ; 动态构造调用字符串
    COMMAND '= "TEST_FUNC" + "(100, 200)"
    CALLSTR COMMAND
    
    ; 直接传递包含参数的字符串
    LOCALS = SHOW_STATUS, 1, "READY"
    TRYCALLSTR LOCALS

@TEST_FUNC(ARG:0, ARG:1)
    PRINTFORML 接收到的参数为: {ARG:0} 和 {ARG:1}

@SHOW_STATUS(ARG, ARGS)
	PRINTFORML ID:{ARG} 模式:%ARGS%
```

**Result**
```text
接收到的参数为: 100 和 200
ID:1 模式:READY
```

### Related ¶

[CALL](CALL.md)
[CALLFORM](CALLFORM.md)
[TRYCALL](TRYCALL.md)
[TRYC system](TRYC.md)

