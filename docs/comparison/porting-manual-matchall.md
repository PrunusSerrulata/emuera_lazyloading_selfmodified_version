# 移植执行手册：MATCHALL + MATCHALLEX 数组全量搜索

> **源**：EmueraDotNet `MATCHALL_Instruction`（指令形式）→ 重新设计为表达式函数  
> **目标**：emuera_lazyloading_selfmodified_version `Creator.Method.cs` + `Creator.cs`  
> **日期**：2026-05-09  
> **状态**：✅ 已完成

---

## 一、概述

新增 2 个 ERB 表达式函数，在数组/字符变量中搜索所有匹配值的索引。

| 函数 | 第一参数类型 | 说明 |
|------|------------|------|
| `MATCHALL(var, value[, beg, end[, outArr]])` | 变量引用 | 与 `GETNUM` 一致 |
| `MATCHALLEX("varName", value[, beg, end[, outArr]])` | 字符串变量名 | 与 `GETNUMB` 一致 |

**与 DotNet 指令形式的差异**：

| 特性 | DotNet（指令） | LazyLoading（表达式函数） |
|------|---------------|--------------------------|
| 返回值 | 写入 `RESULT:0` | `return` 匹配数量（表达式值） |
| 结果索引 | 写入 `RESULT:1..N`（从 1 开始） | 写入第五参数 `outArr[0..]`（从 0 开始） |
| 调用方式 | 独立指令行 | 可在表达式中使用 |
| 无第五参数时 | 仍写入 RESULT | 仅返回计数 |

---

## 二、涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| [Creator.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.cs) | 修改 | 注册 2 个函数名 + 多重重载 |
| [Creator.Method.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs) | 修改 | 新增 `MatchAllMethod` 类 |

---

## 三、前置差异分析

### 3.1 底层数据结构

LazyLoading 使用 `SparseArray<T>`（[SparseArray.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/SparseArray.cs)）——基于 `Dictionary<long, T>` 的稀疏数组。遍历方式与 DotNet 的 `long[]` 不同，但 `VariableToken.GetIntValue(exm, idxs)` / `GetStrValue(exm, idxs)` 接口一致，遍历逻辑可复用。

### 3.2 现有 MATCH 实现参考

`VariableEvaluator.GetMatch()`（[VariableEvaluator.cs:L317-L338](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs)）已实现线性遍历 + 逐元素比较模式。MATCHALL 复用相同遍历逻辑，区别是收集所有匹配索引而非仅计数。

### 3.3 表达式函数注册模式

参照 `REPLACE` 的多重载模式（[Creator.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.cs)），`MATCHALL` 需要注册多个重载（不同参数个数和类型组合）。

### 3.4 第五参数（输出数组）

参照 `SPLIT` 的第五参数模式：`VariableTerm` 引用，通过 `SetValue()` 逐索引写入。自动处理 `SparseArray<long>` 和 `long[]` 两种底层类型。

---

## 四、执行步骤

### 步骤 1：Creator.cs — 注册函数

**文件**：[Creator.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.cs)

在函数注册字典中添加：

```csharp
["MATCHALL"] = new MatchAllMethod(false),
["MATCHALLEX"] = new MatchAllMethod(true),
```

---

### 步骤 2：Creator.Method.cs — 新增 MatchAllMethod 类

**文件**：[Creator.Method.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs)

新增类（约 120 行）：

```csharp
private sealed class MatchAllMethod : FunctionMethod
{
    private readonly bool _useStringName; // true = MATCHALLEX（字符串变量名）

    public MatchAllMethod(bool useStringName)
    {
        ReturnType = typeof(long);
        // 多重重载在 CheckArgumentType 中处理
        argumentTypeArray = null;
        CanRestructure = false;
        _useStringName = useStringName;
    }

    public override string CheckArgumentType(string name, List<AExpression> arguments)
    {
        // 最少 2 个参数（var, value），最多 5 个（var, value, beg, end, outArr）
        if (arguments.Count < 2)
            return name + "関数には少なくとも2つの引数が必要です";
        if (arguments.Count > 5)
            return name + "関数の引数が多すぎます";

        // 第一参数：变量引用（MATCHALL）或字符串（MATCHALLEX）
        if (_useStringName)
        {
            if (arguments[0] is not SingleStrTerm)
                return name + "関数の1番目の引数は文字列である必要があります";
        }
        else
        {
            if (arguments[0] is not VariableTerm)
                return name + "関数の1番目の引数は変数参照である必要があります";
        }

        // 第二参数：值（类型需与 var 一致，运行时检查）
        // 第三、四参数：可选 long
        // 第五参数：可选变量引用（输出数组）
        if (arguments.Count >= 5 && arguments[4] is not VariableTerm)
            return name + "関数の5番目の引数は変数参照である必要があります";

        return null;
    }

    public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
    {
        // 解析第一参数
        VariableToken token;
        if (_useStringName)
        {
            var varName = ((SingleStrTerm)arguments[0]).Str;
            token = exm.VEvaluator.GetVariableToken(varName, exm);
            if (token == null)
                throw new CodeEE("変数 " + varName + " が見つかりません");
        }
        else
        {
            token = ((VariableTerm)arguments[0]).Identifier;
        }

        // 解析搜索值
        var valExpr = arguments[1];
        var type = valExpr.GetOperandType();

        // 解析 beg, end
        long beg = 0;
        if (arguments.Count >= 3 && arguments[2] != null)
            beg = arguments[2].GetIntValue(exm);

        long len;
        if (token.IsCharacterData)
        {
            if (token.IsArray1D)
                len = exm.VEvaluator.CHARANUM;
            else
                len = exm.VEvaluator.CHARANUM;
        }
        else
        {
            if (token.IsArray1D)
                len = token.GetLength(0);
            else
                len = 1; // 非数组字符变量
        }

        long end = len;
        if (arguments.Count >= 4 && arguments[3] != null)
            end = arguments[3].GetIntValue(exm);

        if (beg < 0 || end < 0)
            throw new CodeEE("検索範囲に負の値が渡されました");
        if (beg > end)
            throw new CodeEE("検索範囲の指定が不正です");
        if (end > len)
            end = len; // 自动截断

        // 解析第五参数（输出数组）
        VariableTerm outArr = null;
        if (arguments.Count >= 5 && arguments[4] is VariableTerm vt)
            outArr = vt;

        // 遍历搜索
        var idxs = new long[2];
        int p = 0;
        long count = 0;

        if (type == typeof(long))
        {
            var val = valExpr.GetIntValue(exm);
            for (var i = beg; i < end; i++)
            {
                idxs[p] = i;
                if (val == token.GetIntValue(exm, idxs))
                {
                    if (outArr != null)
                    {
                        var outLen = outArr.Identifier.GetLength(0);
                        if (count < outLen)
                            outArr.Identifier.SetValue(i, [count]);
                    }
                    count++;
                }
            }
        }
        else if (type == typeof(string))
        {
            var val = valExpr.GetStrValue(exm);
            for (var i = beg; i < end; i++)
            {
                idxs[p] = i;
                if (val == token.GetStrValue(exm, idxs))
                {
                    if (outArr != null)
                    {
                        var outLen = outArr.Identifier.GetLength(0);
                        if (count < outLen)
                            outArr.Identifier.SetValue(i, [count]);
                    }
                    count++;
                }
            }
        }
        else
        {
            throw new ExeEE("MATCHALL: サポートされていない型です");
        }

        return count;
    }
}
```

---

### 步骤 3：验证编译

```powershell
dotnet build "D:\emuera\emuera_lazyloading_selfmodified_version\emuera_lazyloading_selfmodified_version.sln" 2>&1
```

预期：0 错误，0 警告。

---

## 五、设计决策说明

### 5.1 为什么是表达式函数而非指令

DotNet 的 `MATCHALL` 是指令形式，结果写入 `RESULT:0..N`。但 `RESULT` 是全局变量，指令形式会污染它。改为表达式函数后：
- 返回值直接作为表达式值（如 `LOCAL = MATCHALL(ARR, 42)`）
- 结果索引通过第五参数输出（从 0 开始），不污染 RESULT

### 5.2 为什么双变体（MATCHALL + MATCHALLEX）

参照 `GETNUM`/`GETNUMB` 模式：
- `MATCHALL`：第一参数是变量引用（编译期解析），性能更好
- `MATCHALLEX`：第一参数是字符串变量名（运行期解析），灵活性更高

### 5.3 第五参数索引从 0 开始

与 ERB 数组索引惯例一致（`ARR:0` 是第一个元素）。DotNet 的 RESULT 从 1 开始是历史遗留问题。

### 5.4 长度不足时自动抛弃

如果第五参数数组长度小于匹配数量，多余索引被静默丢弃。返回值仍为实际匹配数量，脚本可通过返回值判断是否需要更大的数组。

---

## 六、风险点与验证

### 6.1 编译期验证

- `CheckArgumentType` 确保参数个数和类型正确
- 变量引用在编译期解析（MATCHALL），字符串变量名在运行期解析（MATCHALLEX）

### 6.2 功能验证

```erb
#DIM ARR, 10 = 1, 2, 3, 2, 5, 2, 7, 8, 2, 10
#DIM IDX, 10

; 仅计数
LOCAL = MATCHALL(ARR, 2)
PRINTFORML 找到 {LOCAL} 个 2

; 输出索引到 IDX
LOCAL = MATCHALL(ARR, 2, 0, 10, IDX)
FOR I, 0, LOCAL
    PRINTFORML IDX:{I} = {IDX:I}
NEXT
```

预期输出：
```
找到 4 个 2
IDX:0 = 1
IDX:1 = 3
IDX:2 = 5
IDX:3 = 8
```

### 6.3 边界测试

- 空数组（beg == end）：返回 0
- 无匹配：返回 0，outArr 不写入
- 第五参数数组长度不足：多余索引被抛弃，返回值仍为实际匹配数

---

## 七、回滚方案

1. 删除 `Creator.cs` 中新增的 2 行注册
2. 删除 `Creator.Method.cs` 中新增的 `MatchAllMethod` 类
3. 重新编译

改动涉及 2 个文件，回滚成本低。