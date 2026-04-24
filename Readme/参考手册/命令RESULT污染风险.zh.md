# 命令 RESULT 污染风险说明

## 背景

Emuera 中存在一类特殊的命令，它们**技术上可以作为表达式函数使用**，但在作为**命令单独使用**时会产生意想不到的 `RESULT` 污染。

### 污染原理

当一个 `FunctionMethod` 子类（如 `SaveTextMethod`、`SAVETEXT`）作为**命令**调用时，解释器会通过 `METHOD_Instruction` 包装器执行（参见 `Instraction.Child.cs` 第 578-597 行）：

```csharp
private sealed class METHOD_Instruction : AInstruction
{
    public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
    {
        AExpression term = ((MethodArgument)func.Argument).MethodTerm;
        if (term.GetOperandType() == typeof(long))
            exm.VEvaluator.RESULT = term.GetIntValue(exm);  // 无条件覆盖 RESULT
        else
            exm.VEvaluator.RESULTS = term.GetStrValue(exm);
    }
}
```

这意味着：**任何返回整数或字符串的 FunctionMethod，作为命令调用时都会用其返回值覆盖 RESULT(S)**。

这在一些情况下是**期望的行为**（`SAVETEXT 0` 成功时 `RESULT` 为 1，失败时为 0）。但对于某些以**设置/操作**为主要用途的 FunctionMethod，用户往往误以为它们不会影响 RESULT，实际上却悄悄被覆盖。

---

## 风险函数列表

以下按类别列出所有**作为命令使用时存在 RESULT 污染风险**的 FunctionMethod：

### 1. 文本保存/加载

| 函数名 | 返回值 | 命令用法示例 | 风险说明 |
| :--- | :--- | :--- | :--- |
| **SAVETEXT** | int (0=失败, 1=成功) | `SAVETEXT "hello"` | 操作类用法下会覆盖 RESULT，成功=1，失败=0 |
| （对照）**LOADTEXT** | string | `LOADTEXT FILE, LOCATION` | 会将文件内容或空字符串写入 RESULTS |

**建议**：需要判定结果时使用表达式 `IF SAVETEXT(...)`；仅作为操作时注意 RESULT 被覆盖。

### 2. 跳跃控制

| 函数名 | 返回值 | 命令用法示例 | 风险说明 |
| :--- | :--- | :--- | :--- |
| **ISSKIP** | int (0=未跳过, 1=跳过中) | `ISSKIP` | 会将跳过状态写入 RESULT |
| **MOUSESKIP** | int | `MOUSESKIP 1` | 将 skip 开关状态写入 RESULT |
| **MESSKIP** | int | `MESSKIP 1` | 将消息跳过状态写入 RESULT |

**建议**：这些本身是 getter/setter 二合一，注意作为命令使用时 RESULT 被覆盖。

### 3. 图形操作

| 函数名 | 返回值 | 命令用法示例 | 风险说明 |
| :--- | :--- | :--- | :--- |
| **GCLEAR** | int (0=失败, 1=成功) | `GCLEAR` | 操作类用法下会覆盖 RESULT |
| **GDISPOSE** | int | `GDISPOSE SPRITE_ID` | 会将操作结果写入 RESULT |
| **SPRITEDISPOSE** | int | `SPRITEDISPOSE SPRITE_ID` | 会将操作结果写入 RESULT |
| **CBGCLEAR** | int | `CBGCLEAR` | 会将操作结果写入 RESULT |
| **GSAVE** | int | `GSAVE ID` | 会将保存结果写入 RESULT |
| **GLOAD** | int | `GLOAD ID` | 会将加载结果写入 RESULT |

**建议**：需要判定结果时使用 `IF GDISPOSE(...)`；仅作为操作时注意 RESULT 被覆盖。

### 4. 音频控制

| 函数名 | 返回值 | 命令用法示例 | 风险说明 |
| :--- | :--- | :--- | :--- |
| **SOUNDCONTROL** | int (0=失败, 1=成功) | `SOUNDCONTROL SOUND_ID, OP, VOL` | 会将操作结果写入 RESULT |
| **BGMCONTROL** | int | `BGMCONTROL BGM_ID, OP, VOL` | 会将操作结果写入 RESULT |

### 5. 其他操作类

| 函数名 | 返回值 | 命令用法示例 | 风险说明 |
| :--- | :--- | :--- | :--- |
| **OUTPUTLOG** | int | `OUTPUTLOG "text"` | 会将输出操作结果写入 RESULT |
| **SETVAR** | int (0=失败, 1=成功) | `SETVAR "LOCAL:0", 100` | 作为命令时会覆盖 RESULT（`SETVAR` 本身已有 AInstruction 版本，但 FunctionMethod 版本仍注册在 methodList 中） |

---

## 已转换为安全版本的命令（不污染 RESULT）

以下命令已从 FunctionMethod 转换为原生 AInstruction，**作为命令使用时不会污染 RESULT**：

| 命令名 | 说明 | 不污染原因 |
| :--- | :--- | :--- |
| **SETANIMETIMER** | 设置动画计时器 | AInstruction，DoInstruction 无 RESULT 赋值 |
| **SETFONT** | 设置字体 | 走 doNormalFunction 原生 switch，无 RESULT 赋值 |
| **VARSET** | 变量赋值 | AInstruction，DoInstruction 无 RESULT 赋值 |
| **STRICT_FONT_FALLBACK** | 严格字体回退开关 | AInstruction，DoInstruction 无 RESULT 赋值 |
| **SET_SKIA_QUALITY** | SkiaSharp 质量设置 | AInstruction，DoInstruction 无 RESULT 赋值 |
| **SET_TEXT_DRAWING_MODE** | 文本渲染管线切换 | AInstruction，DoInstruction 无 RESULT 赋值 |
| **BITMAP_CACHE_ENABLE** | 位图缓存开关 | AInstruction，DoInstruction 无 RESULT 赋值 |

---

## 设计建议

1. **CALLF**：这是EM对默认具有返回值、但只有赋值等功能的函数类的标准解法：调用此表达式函数，但丢弃它的返回值
2. **需要保留 RESULT 的场景**：先保存 `LOCAL = RESULT`，操作后再恢复
3. **新命令设计原则**：纯操作类命令（如 SETXXX、ENABLE_XXX、DISABLE_XXX）应实现为 AInstruction，而非 FunctionMethod
