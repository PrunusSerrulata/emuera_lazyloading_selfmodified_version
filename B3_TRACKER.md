# B.3 引入浮点数类型与废弃位编码 —— 任务追踪文档

> 本文件是 B.3 子任务（手册 B.3.1-B.3.7 / 路线图 Phase 1.3-1.6）的**总上下文**和**实施状态**。
> 来源：`../m-emuera/REFACTOR_HANDBOOK.md` B.3 节 + `../m-emuera/WORKLOG.md` 路线图 + 源码实测盘点。
> 配套工具：`tools/b3_query.ps1`（查询）、`tools/b3_verify.ps1`（验证）、`tools/b3_16_replace.py`（B.3-16 批量替换）

---

## 0. 总览

### 0.1 目标

在引入浮点数类型的同时，将全代码库的类型判别从 `typeof(long)`/`typeof(string)`（硬编码、不可扩展）统一替换为 `EraType` 枚举 + `VariableDescriptor` 数据查询。

### 0.2 战略决策（摘自手册）

> 引入浮点数意味着全代码库 746 处 `typeof(long)`/`typeof(string)` 硬编码每一处都要改。
> 此时如果只是机械地加 `typeof(double)` 分支，则每新增一种类型都是 O(n) 的全代码库扫描——这是不可持续的。
>
> **正确做法**：在引入浮点数的同时，将 `typeof(T)` 判别模式彻底消灭，替换为 `EraType` 枚举驱动的数据查询。

### 0.3 实施完成状态总表

| 层 | 编号 | 内容 | 状态 |
|----|------|------|------|
| 基础装备 | B.3-0 | `EraType.cs` + `VariableDescriptor.cs` + `VariableDescriptorTable` | ✅ |
| 核心抽象层 | B.3-1 | AExpression Type→EraType + VariableToken 位标志→Descriptor | ✅ |
| 运算层 | B.3-2 | OperatorMethod 分派 + ExpressionParser + LogicalLineParser typeof→EraType | ✅ |
| 指令层 | B.3-3 | Creator.Method.cs 109处 GetOperandType + 13文件批量替换 | ✅ |
| 存储层 | B.3-4 | VariableData Float 存储数组 + EraSaveDataType Float 段 + 存档双精度支持 | ✅ |
| 语法层 | B.3-5 | LiteralFloatWord + ReadDouble + 词法浮点检测 + #DIMF 声明解析 | ✅ |
| 验证 | B.3-6 | 编译通过，B3_SMOKE_TEST 启用（运行时发现5个错误，已由 B.3-7~11 修复） | ✅ |
| Term 层 | B.3-7 | SingleFloatTerm + TermStack.Add(double) 不再截断 + GetValue() Float 分支 | ✅ |
| 运算符层 | B.3-8 | Float-Float / Int-Float 运算符 + 一元 Float + 三元 Float + Reduce 适配 | ✅ |
| 格式化层 | B.3-9 | StrForm {} 接受 Float 类型 + FormatFloatCurlyBrace | ✅ |
| 函数层 | B.3-10 | TOINT 接受 Float + TOFLOAT 新增 + TOSTRF 新增 | ✅ |
| 存储层·Token | B.3-11 | StaticFloat1DVariableToken / PrivateFloat1DVariableToken + 工厂路由修正 + VariableTerm Float 适配 | ✅ |
| VariableType 修复 | B.3-12a | UserDefinedVariableToken Float kind → VariableType = typeof(double) | ✅ |
| 翻译 XML 同步 | B.3-12b | FloatType / CallNonFloatAsFloat 新增 + NumericType 修正 + 双端 XML 同步 | ✅ |
| LoadVariableBinary | B.3-13 | Float 段 bug 修复 + FloatArray2D/3D 加载逻辑补全 | ✅ |
| VariableType 消灭 | B.3-14 | VariableToken.GetEraType() + VariableTerm 全部 EraType 驱动 | ✅ |
| 位标志 switch 消灭 | B.3-15 | 位标志 switch→Descriptor 查询 + 隐式二元判定三路化（15a-15k） | ✅ |
| typeof→EraType 批量替换 | B.3-16 | FunctionMethod ReturnType/argumentTypeArray Type→EraType（16a-16f） | ✅ |

### 0.4 最终残留 typeof 统计（全部必要保留）

| 类别 | 文件 | 处数 | 合法性 |
|------|------|------|--------|
| AExpression/CaseExpression 桥接 | AExpression.cs, CaseExpression.cs | 5 | ✅ 桥接保留 |
| DataTable 类型映射 | EvilMask/Utils.cs | 8 | ✅ System.Data API |
| VariableType [Obsolete] | VariableToken.cs | 4 | ✅ 标记废弃 |
| DataColumn.Add/DataType | Creator.Method.cs | 5 | ✅ System.Data API |
| DataType 比较 | Instraction.Child.cs | 1 | ✅ System.Data API |
| SparseArray.cs typeof(T) | SparseArray.cs | 6 | 🔒 泛型模式 |
| SqlManager.cs typeof | SqlManager.cs | 2 | 🔒 SQLite 类型映射 |
| **合计** | | **31** | **全部必要保留** |

### 0.5 6 大二元判定模式——全部消灭

| 模式 | 原状 | 处理 | 状态 |
|------|------|------|------|
| A. 位标志 `__INTEGER__`/`__STRING__` | ~78 处 | → `Descriptor.Kind` | ✅ |
| B. `IsInteger`/`IsString` 属性 | 50+28 处 | → Float 分支 / Descriptor 驱动 | ✅ |
| C. `VariableType == typeof(...)` | 0 处 | 已在 B.3-14 清除 | ✅ |
| D. `ReturnType`/`argumentTypeArray` Type | 522 处 | → EraType（B.3-16） | ✅ |
| E. `VariableCode` 位标志 switch | 2 处（必要回退） | → Descriptor 查询 | ✅ |
| F. 隐式二元判定 | ~92 处 | → 全部补 Float 分支 | ✅ |

---

## 1. 冒烟测试结果（2026-05-04）

> 测试文件：`d:\eratw-chs\ERB\demo\B3_SMOKE_TEST.ERB`
> 测试日志：`d:\eratw-chs\20260504-043622.log`
> 引擎版本：Emuera.NET SkiaSharp 1824+v24+EMv18+EEv55 Lazyloadingv4.1

### 1.1 测试覆盖

| 测试模块 | 测试项数 | 结果 |
|----------|---------|------|
| B.3-1 类型系统基础设施 | 10 | **全部 PASS** |
| B.3-5 浮点变量基本运算 | 5 | **全部 PASS** |
| B.3-REGRESSION 踩坑回归 | 22 | **全部 PASS** |
| Phase01Bugfix 回归验证 | 18 | **全部 PASS** |
| **合计** | **55** | **0 FAIL, 0 ERROR** |

### 1.2 已验证的核心能力

| 能力 | 验证状态 |
|------|---------|
| Integer/String 变量读写不受 EraType 迁移影响 | ✅ |
| 内置变量（RESULT/RESULTS/DAY）读写正常 | ✅ |
| Int→String 隐式转换正常 | ✅ |
| Int 比较/位运算正常 | ✅ |
| #DIM DYNAMIC 读写正常 | ✅ |
| 内置函数 MAX/STRLENS 正常 | ✅ |
| #DIMF 浮点变量声明 + 初始化 | ✅ |
| #DIMF 默认值 0.0 | ✅ |
| Float 负数 | ✅ |
| Int*Float 隐式提升 | ✅ |
| TOINT(Float) 截断 | ✅ |
| Float 比较运算符 | ✅ |
| Float+Float / Float-Float / Float*Float / Float/Float | ✅ |
| Int+Float / Int-Float / Int*Float / Int/Float | ✅ |
| Float+Int | ✅ |
| Float 一元取负 | ✅ |
| Float 除法精度（10.0/3.0 ≈ 3.333...） | ✅ |
| 跨类型相等比较（1==1.0, 1.0==1） | ✅ |
| TIMES Float | ✅ |
| TOFLOAT("3.14") | ✅ |
| Int/Float 混合运算（隔离复现） | ✅ |
| ConvertArg 多余参数静默丢弃 | ✅ |
| TRYCALL isTry 安全网 | ✅ |
| SafeArithmetic 运算溢出保护 | ✅ |
| TIMES 溢出保护 | ✅ |
| Increment/Decrement 溢出保护 | ✅ |
| PlusValue 溢出保护（+= 运算） | ✅ |
| INITRAND/DUMPRAND 解耦验证 | ✅ |
| ExecutionContext 递归栈隔离 | ✅ |
| SparseArray 稀疏存储操作 | ✅ |

---

## 2. 工具脚本

### 2.1 查询脚本 — `tools/b3_query.ps1`

```powershell
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1                  # 全量查询
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Mode B15        # 仅 B.3-15 指标
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Mode B16        # 仅 B.3-16 指标
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Detail -Mode B15 # 详细行号
```

### 2.2 验证脚本 — `tools/b3_verify.ps1`

```powershell
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 15a -Mode B15
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 16a -Mode B16
```

### 2.3 批量替换脚本 — `tools/b3_16_replace.py`

```powershell
python tools/b3_16_replace.py --dry-run                  # 预览
python tools/b3_16_replace.py --dry-run --all-files      # 预览全部
python tools/b3_16_replace.py --checklist                # 生成 CSV 对照清单
python tools/b3_16_replace.py --apply                    # 16b+16c
python tools/b3_16_replace.py --apply --all-files        # 全部
python tools/b3_16_replace.py --diff                     # unified diff
python tools/b3_16_replace.py --verify --all-files       # 残留统计
```

**替换规则**：`typeof(long)`→`EraType.Integer`, `typeof(string)`→`EraType.String`, `typeof(double)`→`EraType.Float`, `typeof(void)`→`EraType.Void`

---

## 3. 未完成任务：浮点数功能补全

> 以下任务在 B.3-0~16 类型系统重构中**有意延迟**，属于浮点数运行时功能的补全。
> 2026-05-04 终审：对 `BuiltInFunctionCode.cs`、`Creator.Method.cs`、`VariableEvaluator.cs`、`VariableData.cs`、`CharacterData.cs` 进行了全面盘点。

### 3.0 战略决策：LOCAL/ARG/GLOBAL 不提供 Float 版本

**决策**：引擎内部不提供 `.LOCALF` / `.ARGF` / `.GLOBALF` 等 Float 版本的默认分配变量。
- `LOCAL` / `ARG` / `GLOBAL` 保持 Int 类型不变
- 仅新增 `RESULTF`（Float 标量返回值）
- Float 变量通过 `#DIMF` 声明使用，不通过默认分配机制

**理由**：Float 变量使用频率远低于 Int，默认分配浪费内存且增加复杂度。

### 3.1 内置数学函数 Float 重载

**现状（2026-05-04 源码盘点）**：

| 函数 | 当前状态 | Int重载 | Float重载 | 备注 |
|------|---------|---------|-----------|------|
| RAND | Int only | ✅ | ❌ | 需 RANDF |
| MIN/MAX | Int only | ✅ | ❌ | 需 MINF/MAXF |
| ABS | Int only | ✅ | ❌ | 需 ABSF |
| POWER | Int only | ✅ | ❌ | 需 POWERF |
| SQRT | Int only | ✅ | ❌ | 需 SQRTF |
| CBRT | Int only | ✅ | ❌ | 需 CBRTF |
| LOG | Int only | ✅ | ❌ | 需 LOGF |
| LOG10 | Int only | ✅ | ❌ | 需 LOG10F |
| EXPONENT | Int only | ✅ | ❌ | 需 EXPF |
| SIGN | Int only | ✅ | ❌ | 需 SIGNF |
| LIMIT | Int only | ✅ | ❌ | 需 LIMITF |

> 注：SIN/COS/TAN/ASIN/ACOS/ATAN/FLOOR/CEIL/ROUND 等函数在引擎中完全不存在（无 Int 也无 Float 版本），属于新功能请求，不在本次 Float 重载范围内。

**实施方案**：
- 在 `Creator.Method.cs` 中新增 12 个 Float 方法类（`RandFMethod`, `MaxFMethod`, `AbsFMethod` 等）
- 均设置 `ReturnType = EraType.Float`，使用 `argumentTypeArrayEx` + `ArgType.Any` 接受 Int/Float 参数
- 在 `GetFloatValue` 中处理 Int→Float 隐式转换
- 在 `Creator.cs` 注册 "RANDF", "MINF", "MAXF", "ABSF", "POWERF", "SQRTF", "CBRTF", "LOGF", "LOG10F", "EXPF", "SIGNF", "LIMITF"

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs` — 新增 12 个方法类
- `Emuera/Runtime/Script/Statements/Function/Creator.cs` — 注册 12 个新函数名

**优先级**：P1（高）
**状态**：🔄 实施中（Batch 1）

### 3.2 数组操作函数 Float 支持

**现状（2026-05-04 源码盘点）**：

| 函数 | 当前状态 | Int | String | Float | 备注 |
|------|---------|-----|--------|-------|------|
| SUMARRAY/SUMCARRAY | Int only | ✅ | ❌ | ❌ | `VariableEvaluator.GetArraySum` 用 `GetIntValue` |
| MATCH/CMATCH | Int+String | ✅ | ✅ | ❌ | `GetIntValue` 中无 Float 分支 |
| MAXARRAY/MAXCARRAY | Int only | ✅ | ❌ | ❌ | `VariableEvaluator.GetMaxArray` 用 `GetIntValue` |
| MINARRAY/MINCARRAY | Int only | ✅ | ❌ | ❌ | 同上 |
| GROUPMATCH | Int+String | ✅ | ✅ | ❌ | 无 Float 分支 |
| NOSAMES | Int+String | ✅ | ✅ | ❌ | 无 Float 分支 |
| ALLSAMES | Int+String | ✅ | ✅ | ❌ | 无 Float 分支 |
| INRANGEARRAY/INRANGECARRAY | Int only | ✅ | ❌ | ❌ | `GetInRangeArray` 用 `GetIntValue` |
| ARRAYMSORT | Int+String | ✅ | ✅ | ❌ | 需确认 |

**已支持 Float 的数组操作（指令层）**：

| 指令 | Float 支持 | 备注 |
|------|-----------|------|
| ARRAYSHIFT | ✅ | `VariableEvaluator.ShiftArray` 处理 `SparseArray<double>` + `double[]` |
| ARRAYREMOVE | ✅ | 同上 |
| ARRAYSORT | ✅ | 同上 |
| ARRAYCOPY | ✅ | 同上 |
| VARSET | ✅ | `VARSET_Instruction.DoInstruction` 有 `case EraType.Float:` |
| CVARSET | ✅ | `CVARSET_Instruction.DoInstruction` 有 `case EraType.Float:` |

**实施方案**：
- `VariableEvaluator.cs` 新增 `GetArraySumFloat` / `GetMatchFloat` / `GetMaxArrayFloat` 等方法（使用 `GetFloatValue`）
- `Creator.Method.cs` 中 SUMARRAY/MATCH/MAXARRAY/MINARRAY 等方法新增 Float 重载
- GROUPMATCH/NOSAMES/ALLSAMES 新增 Float 分支

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs` — 新增 Float 版本后端方法
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs` — 新增 Float 重载

**优先级**：P1（高）

### 3.3 反射/动态调用函数 Float 支持

> GETVAR、EVAL、GETMETH 等允许通过字符串名动态操作变量/表达式的"反射"函数。

**现状（2026-05-04 源码盘点）**：

| 函数 | 当前状态 | Int | String | Float | 备注 |
|------|---------|-----|--------|-------|------|
| GETVAR | Int only | ✅ | ❌ | ❌ | `GetVarMethod.GetIntValue` 检查 `EraType.Integer`，Float 变量被拒绝 |
| GETVARS | String only | ❌ | ✅ | ❌ | `GetVarsMethod.GetStrValue` 检查 `EraType.String` |
| **SETVAR** | **Int+String+Float** | ✅ | ✅ | ✅ | 已有 `case EraType.Float:` 分支，可写入 Float 变量 |
| GETMETH | Int only | ✅ | ❌ | ❌ | 拒绝非 Int 返回的函数 |
| GETMETHS | String only | ❌ | ✅ | ❌ | 拒绝非 String 返回的函数 |
| **EXISTMETH** | **Int+String+Float** | ✅ | ✅ | ✅ | 已有 `case EraType.Float: res \|= 32`，可检测 Float 函数 |
| EVAL | Int（截断Float） | ✅ | ❌ | ⚠️ | Float 结果被 `(long)` 截断，丢失精度 |
| EVALS | String（转换Float） | ❌ | ✅ | ⚠️ | Float 结果被 `.ToString()`，返回字符串 |

**需新增的函数**：

| 新函数 | 对应现有 | 返回类型 | 用途 |
|--------|---------|---------|------|
| **GETVARF** | GETVAR | `EraType.Float` | 通过字符串名读取 Float 变量值 |
| **GETMETHF** | GETMETH | `EraType.Float` | 通过字符串名动态调用返回 Float 的函数 |
| **EVALF** | EVAL | `EraType.Float` | 动态求值字符串表达式，返回 Float（不截断） |

**实施方案**：
- `Creator.Method.cs` 新增 `GetVarFMethod` / `GetMethFMethod` / `EvalFMethod` 三个类
- 均设置 `ReturnType = EraType.Float`，调用 `GetFloatValue(exm)`
- `Creator.cs` 注册 "GETVARF" / "GETMETHF" / "EVALF"

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs` — 新增 3 个方法类
- `Emuera/Runtime/Script/Statements/Function/Creator.cs` — 注册 3 个新函数名

**优先级**：P2（中）

### 3.4 SAVEDATA/CHARADATA Float 支持

**现状（2026-05-04 源码盘点）**：✅ **已完整支持**

| 组件 | Float 支持 | 备注 |
|------|-----------|------|
| VariableData.dataFloat | ✅ | `double[]`，长度 `__COUNT_STRING__` |
| VariableData.dataFloatArray | ✅ | `double[][]`，1D/2D/3D 均支持 |
| VariableData.SaveToStream | ✅ | 写入 `dataFloat` + `dataFloatArray` |
| VariableData.LoadFromStream | ✅ | 读取 `dataFloat` + `dataFloatArray` |
| VariableData.SaveToStreamExtended | ✅ | 扩展存档支持 |
| VariableData.LoadFromStreamExtended | ✅ | 扩展存档支持 |
| VariableData.SaveToStreamBinary | ✅ | 二进制存档支持 |
| VariableData.LoadFromStreamBinary | ✅ | 二进制存档支持 |
| CharacterData.dataFloat | ✅ | `double[]`，长度 `__COUNT_CHARACTER_FLOAT__` |
| CharacterData.dataFloatArray | ✅ | `SparseArray<double>[]` |
| CharacterData.dataFloatArray2D | ✅ | `double[][,]` |
| CharacterData.SaveToStream | ✅ | 写入 Float 段 |
| CharacterData.LoadFromStream | ✅ | 读取 Float 段 |
| CharacterData.SaveToStreamExtended | ✅ | 扩展存档支持 |
| CharacterData.LoadFromStreamExtended | ✅ | 扩展存档支持 |
| CharacterData.SaveToStreamBinary | ✅ | 二进制存档支持 |
| CharacterData.LoadFromStreamBinary | ✅ | 二进制存档支持 |

**结论**：SAVEDATA/CHARADATA 的 Float 支持已在 B.3-4（存储层）完成，无需额外工作。

### 3.5 函数参数 Float 支持（ARG）

**现状**：`ARG` 和 `ARGS` 仅支持 Int 和 String 类型。`#DIMF` 声明的 Float 变量无法作为函数参数传递。

**影响**：无法编写接受 Float 参数的用户自定义函数。

**实施方案**：
- `ExecutionContext` 新增 `ArgFloats: double[]` 字段
- `FunctionLabelLine` 新增 `ArgFloatLength` 属性
- `ConvertArg()` 支持 Float 类型参数转换
- `VariableToken` 的 ARG 相关子类新增 Float 路径

**优先级**：P1（高）

### 3.6 函数返回值 Float 支持（RESULTF）

**现状**：`RESULT` 仅支持 Int，`RESULTS` 仅支持 String。无 `RESULTF` 变量。

**影响**：用户自定义函数无法返回 Float 值。

**实施方案**：
- 新增内置变量 `RESULTF`（Float 标量返回值）
- 不扩展 `RESULT` 支持 Float 赋值（保持 Int 语义不变）
- `#FUNCTIONF` 语法支持（用户自定义 Float 返回函数）

**优先级**：P1（高）

### 3.7 #FUNCTIONF 用户自定义 Float 返回函数

**现状**：`#FUNCTION` 返回 Int，`#FUNCTIONS` 返回 String。`#FUNCTIONF`（返回 Float）尚未实现。

**实施方案**：
- `ErbLoader.cs` / `ErhLoader.cs` 新增 `#FUNCTIONF` 解析
- `FunctionLabelLine` 新增 `ReturnType = EraType.Float` 支持
- `UserDefinedMethodTerm` 适配 Float 返回类型

**优先级**：P2（中）

### 3.8 任务依赖关系

```
B.3-0~16 类型系统重构 ✅（已完成，前置条件满足）
  │
  ├── 3.1 数学函数 Float 重载（独立，无依赖）──── 可并行
  ├── 3.2 数组操作函数 Float 支持（独立）──────── 可并行
  ├── 3.3 反射函数 Float 支持（独立）─────────── 可并行
  │     ├── GETVARF / GETMETHF / EVALF 新增
  │     ├── SETVAR Float ✅（已完成）
  │     └── EXISTMETH Float ✅（已完成）
  ├── 3.4 SAVEDATA/CHARADATA Float ✅（已完成）
  ├── 3.5 ARG Float 支持 ──→ 3.7 #FUNCTIONF
  └── 3.6 RESULTF ─────────→ 3.7 #FUNCTIONF
```

---

## 4. m-emuera 迁移上下文

> 本节记录 B.3 各子任务在 emuera-lazyloading 完成后，迁移到 m-emuera 时需要的注意事项。

### 4.1 迁移状态总表

| 子任务 | m-emuera 状态 | 注意事项 |
|--------|--------------|----------|
| B.3-0~14 | ⬜ 待整体同步 | m-emuera 的 AExpression 仍使用 `Type` 而非 `EraType` |
| B.3-15a | ⬜ 待迁移 | VariableIdentifier Descriptor 查询 |
| B.3-15b | ⬜ 待迁移 | VariableToken IsSavedata 位标志→Descriptor |
| B.3-15c | ⬜ 待迁移 | VariableData GetExtSaveList + userDefinedSaveVarList 6→9 |
| B.3-15d | ⬜ 待迁移 | CharacterData 位标志 switch→Descriptor |
| B.3-15e | ⬜ 待迁移 | VariableDescriptor.FromCode() 注册表优先 |
| B.3-16 | ⬜ 待迁移 | typeof→EraType 批量替换 |

### 4.2 推荐迁移顺序

1. 先将 B.3-0~14 整体同步到 m-emuera（AExpression Type→EraType、OperatorMethod typeof→EraType、Creator.Method GetOperandType→EraType 等）
2. 同步完整的 VariableDescriptor.cs（含所有 Register 条目 + GetDescriptorByCode）
3. 同步 VariableIdentifier.cs（B.3-15a 修改）
4. 同步 B.3-15b~15e（VariableToken/CharacterData/VariableData）
5. 同步 B.3-16（typeof→EraType 批量替换）
6. 编译验证 + 运行时验证

### 4.3 关键差异点

1. **命名空间**：emuera-lazyloading 使用 `MinorShift.Emuera.Runtime.Script`，m-emuera 使用相同命名空间但项目结构不同（`src/MEmuera.Core/` 子目录）
2. **VariableDescriptorTable.Register 条目**：m-emuera 误创建版本只注册了部分变量（到 JUEL 为止），需从 lazyloading 同步完整版
3. **Config.StrComper 依赖**：VariableDescriptorTable 使用 `new Config.Config.StrComper)` 作为字典比较器，m-emuera 的 Config 命名空间可能不同
4. **extSaveListDic 键变更**：从 `Dictionary<VariableCode, List<VariableCode>>` 改为 `Dictionary<(VariableKind, VariableDimension), List<VariableCode>>`，旧 API 重载保留为向后兼容层

---

## 5. 来源引用索引

| 内容 | 位置 |
|------|------|
| B.3 重构方案（完整） | `../m-emuera/REFACTOR_HANDBOOK.md` B.3 节 |
| 路线图 | `../m-emuera/WORKLOG.md` 开发路线图 Phase 1.3-1.6 |
| 已完成 Phase 0-1 | `../m-emuera/WORKLOG.md` 第十轮 + 第十一轮 |
| B.3-0 日志 | `../m-emuera/WORKLOG.md` 第十二轮 |
| B.3-13/14 日志 + 手册重整 | `../m-emuera/WORKLOG.md` 第二十二轮 |
| EraType.cs | `Emuera/Runtime/Script/EraType.cs` |
| VariableDescriptor.cs | `Emuera/Runtime/Script/VariableDescriptor.cs` |
| VariableToken.GetEraType() | `Emuera/Runtime/Script/Statements/Variable/VariableToken.cs` |
| OperatorMethodManager | `Emuera/Runtime/Script/Statements/Expression/OperatorMethod.cs` |
| SparseArray.cs | `Emuera/Runtime/Script/SparseArray.cs` |
| AExpression.cs | `Emuera/Runtime/Script/Statements/Expression/AExpression.cs` |
| 查询工具 | `tools/b3_query.ps1` |
| 验证工具 | `tools/b3_verify.ps1` |
| 批量替换工具 | `tools/b3_16_replace.py` |
| 冒烟测试 | `d:\eratw-chs\ERB\demo\B3_SMOKE_TEST.ERB` |
| 测试日志 | `d:\eratw-chs\20260504-043622.log` |
