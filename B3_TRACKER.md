# B.3 引入浮点数类型与废弃位编码 —— 任务追踪文档

> 本文件是 B.3 子任务（手册 B.3.1-B.3.7 / 路线图 Phase 1.3-1.6）的**总上下文**和**逐项检查清单**。
> 来源：`../m-emuera/REFACTOR_HANDBOOK.md` B.3 节 + `../m-emuera/WORKLOG.md` 路线图 + 源码实测盘点。
> 配套工具：`tools/b3_query.ps1`（查询）、`tools/b3_verify.ps1`（验证）

---

## 0. 总览

### 0.1 目标

在引入浮点数类型的同时，将全代码库的类型判别从 `typeof(long)`/`typeof(string)`（硬编码、不可扩展）统一替换为 `EraType` 枚举 + `VariableDescriptor` 数据查询。

### 0.2 战略决策（摘自手册）

> 引入浮点数意味着全代码库 746 处 `typeof(long)`/`typeof(string)` 硬编码每一处都要改。
> 此时如果只是机械地加 `typeof(double)` 分支，则每新增一种类型都是 O(n) 的全代码库扫描——这是不可持续的。
>
> **正确做法**：在引入浮点数的同时，将 `typeof(T)` 判别模式彻底消灭，替换为 `EraType` 枚举驱动的数据查询。

### 0.3 影响范围实测（2026-05-03 重整）

| 指标 | B.3-15 范围 | B.3-16 范围 |
|------|-------------|-------------|
| `.IsInteger`/`.IsString` 调用 | **125** 处（16 文件） | — |
| `__INTEGER__`/`__STRING__` 位标志（活跃代码） | **~78** 处（5 文件） | — |
| `typeof(long/string/double)` | — | **675** 处（16 文件） |
| `GetOperandType()` | — | **93** 处（11 文件） |

### 0.4 依赖链——实施顺序

```
B.3-0「基础装备」← ✅ 已完成
  ├─ EraType.cs             ← enum EraType { Integer, String, Float }
  ├─ VariableDescriptor.cs  ← struct VariableDescriptor + VariableDescriptorTable
  └─ (OperatorMethodManager 已存在，B.3.2a 修改)

B.3-1「核心抽象层」← ✅ 已完成
  ├─ AExpression: Type→EraType（桥接 GetOperandType() 保留）
  └─ VariableToken: 位标志→Descriptor 查询

B.3-2「运算层」← ✅ 已完成
  ├─ OperatorMethodManager: 3×3 查表替代 typeof 分派 → 分派方法中 typeof 比较已替换为 EraType（ReturnType 保留 Type 供 FunctionMethod 用，待 B.3-3a）
  ├─ ExpressionParser: 解析时类型推断
  └─ LogicalLineParser: 解析时类型检查

B.3-3「指令层」← ✅ 已完成（GetOperandType 分派全清除，ReturnType/argumentTypeArray Type 保留）
  ├─ Creator.Method.cs: 109 处 GetOperandType → 全清除（59处注释中残留，其余已替换）
  ├─ ArgumentBuilder.cs: GetOperandType → EraType（数量缩减）
  └─ 零散文件: 13 个文件 GetOperandType 批量替换

B.3-4「存储层」← ✅ 已完成
  ├─ VariableData: dataFloat/dataFloatArray/dataFloatArray2D/dataFloatArray3D + EraDataWriter/Reader double 支持
  ├─ EraSaveDataType: Float/FloatArray/FloatArray2D/FloatArray3D 新枚举值
  └─ VariableData Save/Load: Float 段处理（SaveToStream/LoadFromStream + LoadVariableBinary）
     （Float 变量实际存储将在 B.3-5 #DIMF 中填充）

B.3-5「语法层」← ✅ 已完成
  ├─ 词法分析器: 浮点字面量 + LiteralFloatWord + ReadDouble
  └─ #DIMF 浮点变量声明（ErhLoader/ErbLoader/LogicalLineParser/VariableData factory）

B.3-6「验证」← ✅ 已完成（编译验证，但运行时浮点功能未生效）
  └─ 端到端测试: B3_SMOKE_TEST.ERB 启用（B3-1 + B3-5 全部测试用例）
  └─ ⚠️ 运行时发现5个错误：浮点字面量截断、#DIMF 路由到 String token、无 Float 运算符

B.3-7「Term 层」← ✅ 已完成
  ├─ SingleFloatTerm 类（持有 double 值，EraType.Float）
  ├─ TermStack.Add(double) → SingleFloatTerm（不再截断为 SingleLongTerm）
  └─ AExpression.GetValue() Float 分支返回 SingleFloatTerm

B.3-8「运算符层」← ✅ 已完成
  ├─ Float-Float 运算符（+, -, *, /, ==, !=, <, >, <=, >=）
  ├─ Int-Float / Float-Int 运算符（隐式提升 Integer→Float）
  ├─ Float 一元运算符（+, -）
  └─ OperatorMethodManager 注册 + ReduceBinaryTerm/ReduceUnaryTerm 适配

B.3-9「格式化层」← ✅ 已完成
  ├─ StrForm {} 接受 Float 类型（CurlyBraceSubWord）
  └─ FormatFloatCurlyBrace 或扩展 FormatCurlyBrace

B.3-10「函数层」← ✅ 已完成
  ├─ TOINT 接受 Float 参数（截断小数）
  ├─ TOFLOAT 新增（String→Float）
  └─ TOSTRF 新增（Float→String 格式化）

B.3-11「存储层·Float Token」← ✅ 已完成
  ├─ StaticFloat1DVariableToken / PrivateFloat1DVariableToken 等
  ├─ UserDefinedVariableToken 传递正确 Descriptor（Float kind）
  ├─ VariableData 工厂方法：TypeIsFloat → Float token（不再路由到 String token）
  └─ VariableTerm 适配 GetFloatValue/SetValue(double)

B.3-12「验证·全量」← ⚠️ 需手动运行引擎验证
  └─ B3_SMOKE_TEST.ERB 全部通过（0 警告 0 错误）

B.3-12a「VariableType 修复」← ✅ 已完成
  ├─ UserDefinedVariableToken 构造函数：Float kind → VariableType = typeof(double)
  └─ 连锁修复：FVAL 不再被识别为 String，PRINTFORML {} 和 == 运算符均正常

B.3-12b「翻译 XML 同步」← ✅ 已完成
  ├─ 新增 FloatType（浮点型）类型名称条目
  ├─ 新增 CallNonFloatAsFloat / CallNonFloatArrayAsFloat 错误条目
  ├─ 修正 NumericType 中文翻译"整数型"→"数值型"
  ├─ 修正 IsNotNumericBrace / ExpressionResultIsNotNumeric 中文翻译
  ├─ OperatorMethod.cs Float 分支使用 FloatType 替代 NumericType
  ├─ VariableToken 基类 Float 虚方法使用专用错误消息
  └─ lazyloading + m-emuera 双端 XML 同步
```

---

## 1. 工具脚本

### 1.1 查询脚本 — `tools/b3_query.ps1`

```powershell
# 全量查询（所有指标）
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1

# 仅 B.3-15 指标（IsInteger/IsString/bit flags）
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Mode B15

# 仅 B.3-16 指标（typeof/GetOperandType）
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Mode B16

# 详细行号输出
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Detail -Mode B15
```

### 1.2 验证脚本 — `tools/b3_verify.ps1`

```powershell
# B.3-15 批次验证（编译 + IsInteger/IsString/bit flags 残留统计）
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 15a -Mode B15

# B.3-16 批次验证（编译 + typeof/GetOperandType 残留统计）
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 16a -Mode B16
```

### 1.3 测试 ERB — `tools/B3_SMOKE_TEST.ERB`

```erb
# 运行: 在 emuera 中 CALL B3_SMOKE_TEST
# 验证: EraType 枚举、VariableDescriptor 查询、浮点变量声明
```

---

## 2. B.3-1 ~ B.3-6 全部任务逐文件检查清单

### 2.1 第1层「核心抽象层」

#### B.3-1a — AExpression: Type→EraType

**文件**: `Emuera/Runtime/Script/Statements/Expression/AExpression.cs`

**详见 REFACTOR_HANDBOOK.md B.3.1**

| # | 变更点 | 当前代码 | 目标代码 | 完成 |
|---|--------|---------|---------|------|
| 1 | 新增 `EraType` 自动属性 | — | `public EraType EraType { get; }` | ✅ |
| 2 | 构造函数接收 `EraType` | `AExpression(Type t)` | `AExpression(EraType et)` | ✅ |
| 3 | `type: Type` 字段 → 删除 | `readonly Type type;` | 删除 | ✅ |
| 4 | `GetOperandType()` → 桥接保留 | `return type;` | `return EraType switch { Integer=>typeof(long), String=>typeof(string), Float=>typeof(double) }` | ✅ |
| 5 | `IsInteger` → 枚举判等 | `type == typeof(long)` | `EraType == EraType.Integer` | ✅ |
| 6 | `IsString` → 枚举判等 | `type == typeof(string)` | `EraType == EraType.String` | ✅ |
| 7 | 新增 `IsFloat` | — | `EraType == EraType.Float` | ✅ |
| 8 | `SingleTerm GetValue(exm)` 适配 | 两个分支 | 三个分支（Integer/String/Float） | ✅ |
| 9 | 新增 `GetFloatValue` 虚方法 | — | `public virtual double GetFloatValue(exm) => 0.0` | ✅ |
| 10 | 新增 `GetEraType()` | — | `EraType GetEraType()` | ✅ |

**子类清单**（继承 AExpression，需适配构造函数）:

| 子类 | 位置 | 适配项 |
|------|------|--------|
| `ConstantTerm` | Term.cs | `base(typeof(long/string))` → `base(EraType.Integer/String)` |
| `SingleTerm` | Term.cs | 同上 |
| `SingleLongTerm` | Term.cs | 同上 |
| `SingleStrTerm` | Term.cs | 同上 |
| `VariableTerm` | VariableTerm.cs | 同上 |
| `VariableArgTerm` | VariableStrArgTerm.cs | 同上 |
| `StrFormTerm` | StrForm.cs | 同上 |
| `FunctionMethodTerm` | FunctionMethodTerm.cs | 同上 |
| `ElementTerm` | Term.cs | 同上 |
| `CaseStrTerm` | Term.cs | 同上 |
| 其他 Term 子类 | 各文件 | `grep ': base(typeof'` 全量排查 |

#### B.3-1b — VariableToken: 位标志→Descriptor 查询

**文件**: `Emuera/Runtime/Script/Statements/Variable/VariableToken.cs`
**文件**: `Emuera/Runtime/Script/Statements/Variable/VariableCode.cs`

**详见 REFACTOR_HANDBOOK.md B.3.4**

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | VariableToken 新增 `Descriptor` 属性 | 从 VariableDescriptorTable 查询，用户定义变量用 FromCode 回退 | ✅ |
| 2 | `IsInteger` → `Descriptor.IsInteger` | 替换位标志判等 | ✅ |
| 3 | `IsString` → `Descriptor.IsString` | 同上 | ✅ |
| 4 | 新增 `IsFloat` | `Descriptor.IsFloat` | ✅ |
| 5 | `VariableDescriptor.FromCode()` 工厂 | 位标志 → Descriptor 回退（用户定义变量用） | ✅ |

### 2.2 B.3-2「运算层」

#### B.3-2a — OperatorMethodManager: 3×3 查表

**文件**: `Emuera/Runtime/Script/Statements/Expression/OperatorMethod.cs`

**详见 REFACTOR_HANDBOOK.md B.3.3**

当前实现是分散的多字典（`binaryIntIntDic` / `binaryStrStrDic`），改为 `OperatorMethod[3,3]` 统一查表。

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | `ReduceUnaryTerm()` 中 `typeof(long)` → `EraType.Integer` | 6 处 typeof 比较 | ✅ |
| 2 | `ReduceUnaryAfterTerm()` 中 `typeof(long)` → `EraType.Integer` | 4 处 typeof 比较 | ✅ |
| 3 | `ReduceBinaryTerm()` 中 `typeof` → `EraType` | 10 处 typeof 比较 + GetEraType() 本地变量 | ✅ |
| 4 | `ReduceTernaryTerm()` 中 `typeof` → `EraType` | 6 处 typeof 比较 | ✅ |
| 5 | `MultStrInt.GetStrValue()` 中 `typeof(long)` → `EraType.Integer` | 1 处 | ✅ |
| 6 | 算子子类 `ReturnType = typeof(long/string/double)` | 61处（48 long + 3 string + 10 double）保留（FunctionMethod.Type 属性，待 B.3-4 基类 ReturnType→EraType 迁移） | 🔒 有意延迟 |

#### B.3-2b — ExpressionParser 类型推断

**文件**: `Emuera/Runtime/Script/Statements/Expression/ExpressionParser.cs`

| # | 变更点 | 数量 | 完成 |
|---|--------|------|------|
| 1 | `typeof(long)` → `EraType.Integer` | 3 处 | ✅ |
| 2 | `GetOperandType()` → `GetEraType()` | 8 → 2 处 | ✅ |

#### B.3-2c — LogicalLineParser 类型检查

**文件**: `Emuera/Runtime/Script/Parser/LogicalLineParser.cs`

| # | 变更点 | 数量 | 完成 |
|---|--------|------|------|
| 1 | `typeof(long)` → `EraType.Integer` | 7 → 6 处（1处 GetOperandType 已替换） | ✅ |
| 2 | `GetOperandType()` → `GetEraType()` | 1 处 | ✅ |

### 2.3 B.3-3「指令层」

#### B.3-3a — Creator.Method.cs（最大文件，526 处，分 3 批）

**文件**: `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs`

| 批次 | 变更 | 数量 | 策略 | 完成 |
|------|------|------|------|------|
| 批次3 | `GetOperandType() == typeof(long/string)` 运行时分派 | ~109 全清除（含 59 处注释） | 机械替换 4 模式 | ✅ |
| 批次1 | `ReturnType = typeof(long/string/double)` | 243处 | 保留（FunctionMethod.ReturnType 仍为 Type，待 B.3-4 基类迁移） | 🔒 有意延迟 |
| 批次2 | `argumentTypeArray = [typeof(long/string/double), ...]` | 123处 | 保留（Type[] 字段，待 B.3-4 基类迁移） | 🔒 有意延迟 |

#### B.3-3b — ArgumentBuilder.cs

**文件**: `Emuera/Runtime/Script/Statements/ArgumentBuilder.cs`

| # | 变更点 | 数量 | 完成 |
|---|--------|------|------|
| 1 | `GetOperandType()` → `GetEraType()` | 17→ 8 处（ExpressionParser/ArgumentBuilder 等已缩减） | ✅ |
| 2 | `typeof(long/string/void)` → 保留 | 98处（含 32 处 argumentTypeArray 赋值，待 B.3-4 基类迁移） | 🔒 有意延迟 |

#### B.3-3c — 零散文件（每文件 ≤8 处 typeof）

| 文件 | typeof | GetOpType | 完成 |
|------|--------|-----------|------|
| `Instraction.Child.cs` | 5 | 0 | ✅ |
| `Utils.cs` (EvilMask) | 8 | 0 | ✅ |
| `Term.cs` | 0 | 0 | ✅ |
| `FunctionMethod.cs` | 5 | 4 | ✅ (GetOpType 已替换) |
| `Process.ScriptProc.cs` | 0 | 1 | ✅ |
| `SparseArray.cs` | 6 | 0 | 🔒 (typeof(T) 泛型模式，含 double 分支，无需替换) |
| `StrForm.cs` | 1 | 0 | ✅ |
| `ExpressionParser.cs` | 1 | 2 | ✅ |
| `Process.CalledFunction.cs` | 0 | 1 | ✅ |
| `VariableToken.cs` | 2 | 0 | ✅ |
| `VariableTerm.cs` | 4 | 0 | ✅ |
| `UserDefinedRefMethod.cs` | 2 | 0 | ✅ |
| `SqlManager.cs` | 2 | 0 | 🔒 (SQLite 列类型映射，DataType→SqliteType，非 EraType 范畴) |
| `ErbLoader.cs` | 0 | 2 | ✅ |
| `EmueraConsole.cs` | 0 | 0 | ✅ |
| `Process.State.cs` | 0 | 0 | ✅ |
| `CaseExpression.cs` | 0 | 2 | ✅ |
| `AExpression.cs` | 2 | 1 | ✅ (桥接保留) |
| `LogicalLineParser.cs` | 6 | 0 | ✅ (MethodType Type 保留) |

### 2.4 B.3-4「存储层」

#### B.3-4a — VariableData 扩展

**文件**: `Emuera/Runtime/Script/Statements/Variable/VariableData.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | 新增 `dataFloat` / `dataFloatArray` / `dataFloatArray2D` / `dataFloatArray3D` | `double[]` / `double[][]` / `double[][,]` / `double[][,,]`，空数组占位，B.3-5 #DIMF 填充 | ✅ |
| 2 | 新增 `DataFloat` / `DataFloatArray` / `DataFloatArray2D` / `DataFloatArray3D` 属性 | 公共只读 getter | ✅ |
| 3 | SaveToStream / LoadFromStream 浮点段 | 读写 dataFloat / dataFloatArray | ✅ |
| 4 | EraDataWriter/Reader 新增 `Write(double/double[])` / `ReadDouble()/ReadDoubleArray()` | 文本存档格式扩展 | ✅ |

#### B.3-4b — 存档序列化扩展

**文件**: `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | `EraSaveDataType.Float` / `.FloatArray` / `.FloatArray2D` / `.FloatArray3D` | 0x04/0x05/0x06/0x07 新枚举值 | ✅ |
| 2 | EraBinaryDataWriter.WriteWithKey 添加 double/double[]/double[,]/double[,,] 分支 | 二进制存档格式 | ✅ |
| 3 | EraBinaryDataReader 添加 ReadDouble()/ReadInt32() 抽象 + 实现 | 二进制读档支持 | ✅ |
| 4 | VariableData.LoadVariableBinary 添加 Float/FloatArray/FloatArray2D/FloatArray3D case | 加载时 Float 段识别（旧存档无 Float 段自动跳过） | ✅ |

### 2.5 B.3-5「语法层」

#### B.3-5a — 词法分析器浮点字面量

**文件**: `Emuera/Runtime/Script/Parser/LexicalAnalyzer.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | `ReadDouble()` | 双精度字面量扫描（整数部分+小数点+小数部分+科学计数法 e/E） | ✅ |
| 2 | `LiteralFloatWord` | Word 子类，`Type = 'R'`，含 Float(×1.0) 和 Int(×1.0→long) 属性 | ✅ |
| 3 | 词法分析器浮点检测 | 数字→预扫描判断是否有 '.' → float/int 分派 | ✅ |
| 4 | ExpressionParser case 'R' | TermStack.Add(double) → SingleLongTerm（当前截断，Float Term 待 B.3-6） | ✅ |

#### B.3-5b — `#DIMF` 浮点变量声明

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | ErhLoader 支持 `#DIMF` | sharpID==DIMF → DimLineWC(wc, isString, isFloat=true) 入队 | ✅ |
| 2 | ErbLoader/LogicalLineParser 支持 `#DIMF` | 函数内 `#DIMF` → CreatePrivateVariable | ✅ |
| 3 | DimLineWC 新增 `Dimf` 字段 | `bool Dimf` + 构造函数参数 | ✅ |
| 4 | UserDefinedVariableData 新增 `TypeIsFloat` | Create 重载 + TypeIsFloat 分支 → Str token（临时） | ✅ |
| 5 | VariableData 工厂方法适配 TypeIsFloat | CreateUserDefVariable / CreatePrivateVariable 三处 `TypeIsFloat` → Str token 路由 | ✅（临时路由，B.3-11 修正） |

### 2.6 B.3-6「验证」

| # | 测试 | 验证点 | 完成 |
|---|------|--------|------|
| 1 | 基本浮点运算 | `#DIMF a=3.14; a*2 → 6.28` — B3_SMOKE_TEST 已启用 | ✅（编译通过） |
| 2 | Int↔Float 类型转换 | `TOINT(3.14) → 3` — 词法/表达式路径验证 | ⚠️ 运行时报错 |
| 3 | 存档兼容 | 旧存档无 Float 段 → EraSaveDataType switch 正常 default | ✅ |
| 4 | 现有脚本零影响 | typeof 746→332 (55%清除)、GetOperandType 桥接零影响 | ✅ |
| 5 | Creator.Method 全量回归 | 515+ 内置函数 GetOperandType→EraType 机械替换，编译 0 错误 | ✅ |

### 2.7 B.3-7「Term 层」

**文件**: `Emuera/Runtime/Script/Statements/Expression/Term.cs`, `ExpressionParser.cs`, `AExpression.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | 新增 `SingleFloatTerm` | 持有 `double fValue`，`EraType.Float`，`GetFloatValue()`/`GetValue()`/`ToString()` | ✅ |
| 2 | `TermStack.Add(double)` 修正 | `new SingleLongTerm((long)d)` → `new SingleFloatTerm(d)` | ✅ |
| 3 | `AExpression.GetValue()` Float 分支 | `new SingleLongTerm(0)` → `new SingleFloatTerm(0.0)` | ✅ |

### 2.8 B.3-8「运算符层」

**文件**: `Emuera/Runtime/Script/Statements/Expression/OperatorMethod.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | Float-Float 算术运算符 | PlusFloatFloat / MinusFloatFloat / MultFloatFloat / DivFloatFloat | ✅ |
| 2 | Float-Float 比较运算符 | EqualFloatFloat / NotEqualFloatFloat / LessFloatFloat / GreaterFloatFloat / LessEqualFloatFloat / GreaterEqualFloatFloat | ✅ |
| 3 | Int-Float / Float-Int 运算符 | 隐式提升 Integer→Float，结果为 Float（binaryMixedFloatDic） | ✅ |
| 4 | Float 一元运算符 | PlusFloat / MinusFloat | ✅ |
| 5 | 注册到 OperatorMethodManager | binaryFloatFloatDic / binaryMixedFloatDic + unaryFloat 内联 | ✅ |
| 6 | ReduceBinaryTerm 适配 | 新增 Float-Float / Int-Float / Float-Int 分支 | ✅ |
| 7 | ReduceUnaryTerm 适配 | 新增 Float 分支 | ✅ |
| 8 | ReduceTernaryTerm 适配 | 新增 ternaryIntFloatFloat 分支 | ✅ |

### 2.9 B.3-9「格式化层」

**文件**: `Emuera/Runtime/Script/Data/StrForm.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | CurlyBraceSubWord 接受 Float | `operand.GetEraType() != EraType.Integer` → 允许 Float | ✅ |
| 2 | FormatFloatCurlyBrace | `arguments[0].GetFloatValue(exm).ToString()` 格式化 | ✅ |

### 2.10 B.3-10「函数层」

**文件**: `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs`, `Creator.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | TOINT 接受 Float | 新增 Float 分支，`(long)arguments[0].GetFloatValue(exm)` | ✅ |
| 2 | TOFLOAT 新增 | String→Float，`double.TryParse(s)` | ✅ |
| 3 | TOSTRF 新增 | Float×String→String，格式化输出 | ✅ |
| 4 | 注册到 Creator.cs | `["TOFLOAT"]` / `["TOSTRF"]` | ✅ |

### 2.11 B.3-11「存储层·Float Token」

**文件**: `Emuera/Runtime/Script/Statements/Variable/VariableToken.cs`, `VariableData.cs`, `VariableTerm.cs`, `UserDefinedVariable.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | StaticFloat1DVariableToken | `double[] array`，GetIntValue→截断，GetFloatValue→直接返回 | ✅ |
| 2 | PrivateFloat1DVariableToken | 同上 + arrayStack | ✅ |
| 3 | UserDefinedVariableToken Descriptor 传递 | 构造函数中根据 TypeIsFloat 设置 Float kind Descriptor | ✅ |
| 4 | VariableData 工厂方法修正 | `TypeIsFloat` → Float token（不再路由到 String token） | ✅ |
| 5 | VariableTerm GetFloatValue | Float 变量返回 `Identifier.GetFloatValue(...)` | ✅ |
| 6 | VariableTerm SetValue(double) | Float 变量接受 double 赋值 | ✅ |
| 7 | VariableToken 基类 Float 虚方法 | GetFloatValue/SetValue(double)/SetValue(double[])/SetValueAll(double) | ✅ |
| 8 | UserDefinedVariableData.DefaultFloat | `double[]` 初始值数组 | ✅ |

### 2.12 B.3-12「验证·全量」

| # | 测试 | 验证点 | 完成 |
|---|------|--------|------|
| 1 | B3_SMOKE_TEST.ERB | 0 警告 0 错误全部通过 | ⚠️ 需手动运行引擎验证 |
| 2 | 编译验证 | dotnet build 0 错误 | ✅ |
| 3 | 现有脚本回归 | Integer/String 运算零影响 | ✅（编译通过，类型系统桥接保留） |
| 4 | 存档兼容 | 旧存档加载正常 | ⚠️ 需手动运行引擎验证 |

> **B.3-12 手动验证步骤**：
> 1. 启动 `Emuera.exe`
> 2. 加载任意 ERB 游戏项目
> 3. 在控制台输入 `CALL B3_SMOKE_TEST`
> 4. 确认所有测试输出 `[PASS]`，无 `[FAIL]`

### 2.13 B.3-12a「VariableType 修复」

**文件**: `Emuera/Runtime/Script/Statements/Variable/VariableToken.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | UserDefinedVariableToken 三参数构造函数 | `descriptor.Kind == VariableKind.Float` → `VariableType = typeof(double)` | ✅ |
| 2 | 连锁修复：VariableTerm 构造函数 | `VariableType == typeof(double)` → `EraType.Float`（已有代码，现生效） | ✅ |
| 3 | 连锁修复：PRINTFORML {FVAL} | StrForm 检查 `GetEraType() == EraType.Float` 通过（已有代码，现生效） | ✅ |
| 4 | 连锁修复：IF FVAL == 150.0 | ReduceBinaryTerm Float-Float 运算符匹配（已有代码，现生效） | ✅ |

> **根因**：`VariableToken` 基类构造函数 `VariableType = ((varCode & __INTEGER__) == __INTEGER__) ? typeof(long) : typeof(string)`
> Float token 使用 `VariableCode.VARS`（非 `__INTEGER__`），导致 `VariableType = typeof(string)`。
> VariableTerm 依赖 `VariableType` 判断类型，连锁导致 FVAL 被识别为 String。

### 2.14 B.3-12b「翻译 XML 同步」

**文件**: `Lang.cs`, `emuera-zhs.xml`, `emuera-eng.xml`, `OperatorMethod.cs`, `VariableToken.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | Lang.cs 新增 `FloatType` | `"浮動小数点型"` 日语原文 | ✅ |
| 2 | Lang.cs 新增 `CallNonFloatAsFloat` | `"浮動小数点型でない変数\"{0}\"を浮動小数点型として呼び出しました"` | ✅ |
| 3 | Lang.cs 新增 `CallNonFloatArrayAsFloat` | `"浮動小数点型配列でない変数\"{0}\"を浮動小数点型配列として呼び出しました"` | ✅ |
| 4 | zhs.xml 新增 `FloatType` | `"浮点型"` | ✅ |
| 5 | zhs.xml 新增 `CallNonFloatAsFloat` | `"非浮点型变量\"{0}\"被当作浮点型调用"` | ✅ |
| 6 | zhs.xml 新增 `CallNonFloatArrayAsFloat` | `"非浮点型数组变量\"{0}\"被当作浮点型数组调用"` | ✅ |
| 7 | zhs.xml `NumericType` 修正 | `"整数型"` → `"数值型"` | ✅ |
| 8 | zhs.xml `IsNotNumericBrace` 修正 | `"不是整数"` → `"不是数值"` | ✅ |
| 9 | zhs.xml `ExpressionResultIsNotNumeric` 修正 | `"不是整数"` → `"不是数值"` | ✅ |
| 10 | eng.xml 新增 `FloatType` | `"Floating-point type"` | ✅ |
| 11 | eng.xml 新增 `CallNonFloatAsFloat` | `"Non-float variable \"{0}\" is called as a float"` | ✅ |
| 12 | eng.xml 新增 `CallNonFloatArrayAsFloat` | `"Non-float array variable \"{0}\" is called as a float array"` | ✅ |
| 13 | OperatorMethod.cs ReduceUnaryTerm | Float 分支 `NumericType` → `FloatType` | ✅ |
| 14 | OperatorMethod.cs ReduceBinaryTerm | Float 分支 `NumericType` → `FloatType`（2处） | ✅ |
| 15 | VariableToken.cs GetFloatValue 虚方法 | `CallIntAsStr` → `CallNonFloatAsFloat` | ✅ |
| 16 | VariableToken.cs SetValue(double) 虚方法 | `CallIntAsStr` → `CallNonFloatAsFloat` | ✅ |
| 17 | VariableToken.cs SetValue(double[]) 虚方法 | `CallNDIntAsStr` → `CallNonFloatArrayAsFloat` | ✅ |
| 18 | VariableToken.cs SetValueAll(double) 虚方法 | `CallNDIntAsStr` → `CallNonFloatArrayAsFloat` | ✅ |
| 19 | m-emuera zhs.xml 同步 | 同上 4-9 | ✅ |
| 20 | m-emuera eng.xml 同步 | 同上 10-12 | ✅ |

---

## 3. 工程规则

### 3.0 B.3 遗留项追踪

> B.3-6 验证时发现运行时5个错误，根因是浮点数3处"临时路由/截断"未消除。
> 现已拆分为 B.3-7 ~ B.3-12 六个子任务逐步修复。

| 项 | 原说明 | 现状 | 归属 |
|----|--------|------|------|
| 浮点字面量截断为 SingleLongTerm | TermStack.Add(double) → new SingleLongTerm((long)d) | ✅ 已修复 | B.3-7 |
| #DIMF 变量路由到 String token | TypeIsFloat → Str token 临时路由 | ✅ 已修复 | B.3-11 |
| 无 Float-Float / Int-Float 运算符 | ReduceBinaryTerm 只处理 IntInt/StrStr | ✅ 已修复 | B.3-8 |
| StrForm {} 拒绝 Float | operand.GetEraType() != EraType.Integer | ✅ 已修复 | B.3-9 |
| TOINT 只接受 String | argumentTypeArray = [typeof(string)] | ✅ 已修复 | B.3-10 |
| VariableToken.VariableType 对 Float 返回 String | 基类构造函数按 varCode 位判断 | ✅ 已修复 | B.3-12a |
| 翻译 XML 缺少 Float 类型条目 | 无 FloatType / CallNonFloatAsFloat 等 | ✅ 已修复 | B.3-12b |
| OperatorMethod 算子子类 ReturnType | 61处（48 long + 3 string + 10 double），仍用 Type | 🔒 有意延迟（待 B.3-4 基类 ReturnType→EraType 迁移） |
| Creator.Method.cs ReturnType | 243处，仍用 Type | 🔒 有意延迟（同上） |
| Creator.Method.cs argumentTypeArray | 123处，仍用 Type[] | 🔒 有意延迟（同上） |
| ArgumentBuilder.cs typeof | 98处（含 32 处 argumentTypeArray），仍用 Type | 🔒 有意延迟（同上） |
| FunctionMethod.CheckArgumentType Type[] | 仍用 Type[] 做类型检查 | 🔒 有意延迟（下一级优化） |
| LogicalLineParser MethodType 6 处 | 仍用 Type 属性 | 🔒 有意延迟（需 FunctionLabelLine 迁移） |
| SparseArray.cs typeof(T) 6 处 | 泛型模式，含 double 分支 | 🔒 无需替换 |
| SqlManager.cs typeof 2 处 | SQLite 列类型映射，非 EraType 范畴 | 🔒 无需替换 |

**B.3 当前状态：B.3-0~11 已完成，B.3-12a/b 已完成，B.3-12 待手动运行引擎验证。B.3-13~16 为二元判定抽象后续任务。**

**最新 typeof 统计（2026-05-03 第二次盘点）**：
```
typeof(long)  : 439
typeof(string): 220
typeof(double):  23
typeof(void)  :  21
GetOperandType:  93
总计待处理: 796 项 / 涉及 16 文件
```
> 注：以上包含 🔒 有意延迟项（ReturnType/argumentTypeArray ~527 处 + SparseArray/SqlManager ~8 处 + 桥接保留 ~4 处）。
> 实际需关注的核心 typeof（运算分派/类型检查）已在 B.3-1~3 中清除。
> ReturnType/argumentTypeArray 待 FunctionMethod 基类 ReturnType 从 Type 迁移到 EraType 后批量替换。

### 3.3 二元判定全体审查与抽象方案（2026-05-03 初审，2026-05-03 重整）

> **背景**：FVAL 被识别为 String 的根因是 VariableToken 基类构造函数按 varCode 位标志判断类型——
> `__INTEGER__` 位 → typeof(long)，否则 → typeof(string)。Float token 无 `__INTEGER__` 位，被错误识别。
> 这暴露了原类型系统的**6 大二元判定模式**，全部需要抽象为 N 元（EraType 驱动）。
>
> **已完成**：B.3-13（LoadVariableBinary Float 段 bug 修复）、B.3-14（VariableType→EraType 直接查询）。
> **待完成**：B.3-15（位标志 switch 消灭）、B.3-16（FunctionMethod ReturnType 迁移）。

#### 6 大二元判定模式（2026-05-03 精确统计）

| 模式 | 标识 | 处数 | 危害 | 抽象方案 | 状态 |
|------|------|------|------|----------|------|
| A. `VariableCode.__INTEGER__/__STRING__` 位标志判定 | 位标志 switch/if | ~85 | 🔴 Float 无位标志 | → `Descriptor.Kind` + `Descriptor.Dimension` | ⬜ B.3-15 |
| B. `IsInteger`/`IsString` 属性判定 | if-else 无 Float | 125 | 🔴 LoadVariableBinary 数据丢失（已修复） | → `switch (GetEraType())` | 🔶 B.3-13✅ B.3-15⬜ |
| C. `VariableType == typeof(...)` 比较 | Type 比较 | 0 | ✅ 已清除 | VariableToken.GetEraType() | ✅ B.3-14 |
| D. `ReturnType = typeof()` / `argumentTypeArray` | Type 声明 | ~527 | ⚪ 有意延迟 | FunctionMethod ReturnType Type→EraType | ⬜ B.3-16 |
| E. `VariableCode` 位标志 switch | 位标志 switch | ~37 | 🟡 扩展性障碍 | → Descriptor 查询 | ⬜ B.3-15 |
| F. 隐式二元判定 | if-else 无 Float | ~107 | 🟡 静默遗漏 | → `switch (GetEraType())` | ⬜ B.3-15 |

> **统计方法**：模式 B=125 处 `.IsInteger`/`.IsString` 调用（ripgrep 精确计数：IsInteger 77 + IsString 48，16 文件）；
> 模式 F=107 处 `if (.IsInteger)`/`if (.IsString)` 条件判定（含注释中 ~15 处，实际活跃 ~92 处）；
> 模式 A=271 处 `__INTEGER__`/`__STRING__` 引用（含 VariableCode.cs 枚举定义 193 处，活跃代码 ~78 处）；
> 模式 C=0 处（B.3-14 已清除所有 `VariableType == typeof(...)` 比较）。
> 注：PowerShell Select-String 因 `\b` 边界差异，计数可能比 ripgrep 少 ~6 处，以 ripgrep 为准。

#### 抽象的统一原则

```
之前（二元）：                    之后（N元）：
  if (IsInteger) { ... }           switch (GetEraType()) {
  else if (IsString) { ... }         case EraType.Integer: ...; break;
  // Float 被遗漏                    case EraType.String:  ...; break;
                                     case EraType.Float:   ...; break;
                                     default: throw new NotSupportedException();
                                   }
```

新增类型时的工作量：`VariableKind` 加标志 → `VariableDescriptorTable` 注册 → `EraType` 加枚举值 → switch 加 case。**O(1) 而非 O(n)**。

#### 逐文件审阅清单（2026-05-03 精确扫描）

> 🔶 = 部分完成（B.3-13/14 已修复核心 bug），⬜ = 待处理，✅ = 已完成，🔒 = 有意延迟

##### 模式 A+E：位标志判定 + 位标志 switch（~85+37=~122 处活跃代码）

| 文件 | 处数 | 当前代码 | 危害 | 目标 | 状态 |
|------|------|---------|------|------|------|
| **VariableCode.cs** | 193 | 枚举定义 `__INTEGER__`/`__STRING__` 位标志 | 🔴 Float 无位标志 | 🔒 保留（枚举定义不可删除，仅停止新增使用） | 🔒 |
| **VariableIdentifier.cs** | 28 | `IsInteger`/`IsString` 从位标志计算 + `extSaveListDic` 位标志 key + 断言 | 🟡 Float 无标志→断言失败 | B.3-15: 从 Descriptor 查询 | ⬜ |
| **VariableToken.cs** | 10 | L22 VariableType 从位标志 + L57-88 IsSavedata 位标志 switch | 🟡 无 Float case | B.3-15: Descriptor.Kind+Dimension | 🔶 L22✅ L57-88⬜ |
| **VariableData.cs** | 16 | `GetExtSaveList(__STRING__/__INTEGER__|__ARRAY_*)` 位标志查询 | 🟡 无 Float 存档段 | B.3-15: Descriptor 查询 | ⬜ |
| **CharacterData.cs** | 36 | L178-211/L471-496 位标志 switch + L343-455 GetExtSaveList | 🟡 无 Float case | B.3-15: Descriptor.Kind+Dimension | ⬜ |
| **VariableDescriptor.cs** | 1 | `FromCode()` 中 `if ((code & __STRING__) != 0) kind = String` | 🟡 Float 无位标志→默认 Integer | B.3-15: Float 走注册表 | ⬜ |

##### 模式 B：IsInteger/IsString 属性判定（125 处，16 文件）

| 文件 | 处数 | 用途 | 危害 | 目标 | 状态 |
|------|------|------|------|------|------|
| **VariableData.cs** | 9 | LoadVariableBinary 类型匹配 | � Float 段 bug（已修复） | B.3-13 修复 Float 段 | 🔶 Float段✅ 其余⬜ |
| **CharacterData.cs** | 10 | LoadVariableBinary + 排序 | 🟡 无 Float 段处理 | B.3-15: 三路 | ⬜ |
| **Creator.Method.cs** | 31 | 变量类型分派 + REF 参数检查 | 🟡 Float 进错分支 | B.3-15: switch(GetEraType()) | ⬜ |
| **Instraction.Child.cs** | 11 | 指令执行时类型分派 | 🟡 Float 进错分支 | B.3-15: 三路 | ⬜ |
| **ArgumentBuilder.cs** | 17 | 参数类型检查 | 🟡 Float 进错分支 | B.3-15: 三路 | ⬜ |
| **EmueraConsole.cs** | 8 | 按钮输入类型判断 | ⚪ UI 层 IsInteger=按钮有整数值（非变量类型判定） | 🔒 无需修改 | 🔒 |
| **VariableEvaluator.cs** | 8 | 变量求值/赋值类型分派 | 🟡 Float 进错分支 | B.3-15: 三路 | ⬜ |
| **ConstantData.cs** | 9 | CSV 加载时变量类型分派 | 🟡 Float 变量 CSV 加载 | B.3-15: 三路 | ⬜ |
| **Process.ScriptProc.cs** | 9 | SWAP/CVARSET 类型检查 | 🟡 Float 进错分支 | B.3-15: 三路 | ⬜ |
| **VariableToken.cs** | 5 | IsInteger/IsString 属性定义 + 比较 | ✅ 已走 Descriptor | — | ✅ |
| **UserDefinedRefMethod.cs** | 3 | REF 参数类型匹配 | 🟡 Float 无匹配 | B.3-15: 三路 | ⬜ |
| **UserDefinedVariable.cs** | 1 | 维度类型检查 | 🟡 Float 类型不匹配 | B.3-15: 三路 | ⬜ |
| **VariableParser.cs** | 1 | 参数类型检查 | 🟡 Float 进错分支 | B.3-15: 三路 | ⬜ |
| **PluginMethodParameter.cs** | 1 | 插件参数类型检查 | 🟡 Float 进错分支 | B.3-15: 三路 | ⬜ |
| **Process.CalledFunction.cs** | 1 | 参数类型转换 | 🟡 Float 进错分支 | B.3-15: 三路 | ⬜ |
| **ConsoleButtonString.cs** | 1 | 按钮输入类型 | ⚪ UI 层 | 🔒 无需修改 | 🔒 |

##### 模式 C：VariableType == typeof 比较 — ✅ 已全部清除

B.3-14 已完成：VariableToken.GetEraType() + VariableTerm 构造函数/GetValue/SetValue 全部改为 EraType 驱动。
VariableToken 基类构造函数 L22 从位标志→Descriptor 查询。VariableType 属性仍保留（3 处引用），待 B.3-15 进一步清理。

##### 模式 D：ReturnType/argumentTypeArray Type 声明 — 🔒 有意延迟 → B.3-16

| 文件 | 处数 | 说明 |
|------|------|------|
| **Creator.Method.cs** | ~480 | ReturnType=typeof + argumentTypeArray=typeof + GetOperandType 注释残留 |
| **ArgumentBuilder.cs** | ~82 | argumentTypeArray 赋值 + typeof 检查 |
| **FunctionMethod.cs** | ~6 | ReturnType/argumentTypeArray 属性定义 + CheckArgumentType |
| **OperatorMethod.cs** | ~63 | ReturnType=typeof（算子子类） |
| **其他** | ~44 | LogicalLineParser/Instraction.Child/UserDefinedMethodTerm/ExpressionParser/AExpression/StrForm/UserDefinedRefMethod/EvilMask.Utils/SqlManager/SparseArray |

##### 模式 F：隐式二元判定（if-else 无 Float 分支，~92 处活跃代码）

> 以下仅列出**需要修改**的文件（排除 🔒 UI 层和已正确处理的部分）

| 文件 | 活跃处数 | 典型代码 | 危害 | 目标 |
|------|---------|---------|------|------|
| **Creator.Method.cs** | ~20 | `if (!var.IsInteger)` / `if (token.IsInteger) res|=1; if (token.IsString) res|=2` | 🟡 Float 进错分支/无 bit | B.3-15: 三路 |
| **ArgumentBuilder.cs** | ~14 | `if (varTerm.IsString)` / `if (!term.IsInteger)` | 🟡 Float 进错分支 | B.3-15: 三路 |
| **Instraction.Child.cs** | ~7 | `if (arg.VariableDest.IsInteger)` / `if (var.IsString)` | 🟡 Float 进错分支 | B.3-15: 三路 |
| **VariableEvaluator.cs** | ~6 | `if (!p.Identifier.IsInteger)` / `if (p.IsString)` | 🟡 Float 进错分支 | B.3-15: 三路 |
| **ConstantData.cs** | ~9 | `if (id.IsInteger)` / `else if (id.IsString)` | 🟡 Float CSV 加载 | B.3-15: 三路 |
| **Process.ScriptProc.cs** | ~5 | `if (dest.Identifier.IsInteger)` / `IsInteger&&IsString` | 🟡 Float 进错分支 | B.3-15: 三路 |
| **UserDefinedRefMethod.cs** | ~3 | `if (vToken.IsInteger)` / `if (vToken.IsString)` | 🟡 Float 无匹配 | B.3-15: 三路 |
| **UserDefinedVariable.cs** | 1 | `else if (dims != sTerm.IsString)` | 🟡 Float 类型不匹配 | B.3-15: 三路 |
| **VariableParser.cs** | 1 | `if (terms[i].IsString)` | 🟡 Float 进错分支 | B.3-15: 三路 |
| **PluginMethodParameter.cs** | 1 | `if (term.IsString)` | 🟡 Float 进错分支 | B.3-15: 三路 |
| **CharacterData.cs** | ~3 | `if (var.IsString)` / `if (sortkey.IsString)` | 🟡 Float 进错分支 | B.3-15: 三路 |
| **VariableData.cs** | 1 | `if (!ret.IsString) type++` | 🟡 Float 变量进错列表 | B.3-15: 三路 |

#### 已完成任务

**B.3-13 ✅**：修复 LoadVariableBinary Float 段 bug
- VariableData.cs：`!vToken.IsInteger` → `!vToken.IsFloat`（4 处）；`(long)ReadDouble()` → `ReadDouble()`；`SparseArray<long>` → `SparseArray<double>`；补全 FloatArray2D/3D 加载逻辑
- CharacterData.cs：补全 Float/FloatArray/FloatArray2D/FloatArray3D 丢弃读取 case

**B.3-14 ✅**：VariableType 消灭 → EraType 直接查询
- VariableToken 新增 `public EraType GetEraType()` 方法
- VariableTerm 构造函数：`token.VariableType == typeof(long) ? ...` → `token.GetEraType()`
- VariableTerm.GetValue()：`Identifier.VariableType == typeof(long)` → `switch (Identifier.GetEraType())`
- VariableTerm.SetValue(AExpression)：同上
- VariableToken 基类构造函数 L22：从位标志→Descriptor 查询
- **残留**：VariableType 属性仍保留（3 处引用），待 B.3-15 进一步清理

#### 待完成任务

##### B.3-15：位标志 switch 消灭 → Descriptor 查询

**P2 优先级 — 扩展性障碍消除**

**范围**：~122 处位标志代码 + ~92 处隐式二元判定 = ~214 处需修改

**分批计划**（每批 1-3 文件，独立可编译）：

| 批次 | 文件 | 变更 | 处数 | 风险 |
|------|------|------|------|------|
| 15a | VariableIdentifier.cs | IsInteger/IsString→Descriptor 查询；断言改为 Kind 检查；extSaveListDic key 改为 (Kind,Dimension) | ~28 | 🟡 extSaveListDic key 变更影响存档 |
| 15b | VariableToken.cs | IsSavedata 位标志 switch→Descriptor 查询；VariableType 属性标记 Obsolete | ~10 | 🟢 低风险 |
| 15c | VariableData.cs | GetExtSaveList 位标志查询→Descriptor 查询；userDefinedSaveVarList 扩展为 9 槽位；`!ret.IsString → type++` 三路分派 | ~17 | 🔴 userDefinedSaveVarList 扩展影响存档格式 |
| 15d | CharacterData.cs | 位标志 switch→Descriptor 查询；GetExtSaveList 改造；LoadVariableBinary Float 段处理 | ~46 | 🟡 大文件，需仔细 |
| 15e | VariableDescriptor.cs | FromCode() Float 变量走注册表而非位标志回退 | ~1 | 🟢 低风险 |
| 15f | Creator.Method.cs | IsInteger/IsString 二元判定→三路 | ~20 | 🟡 最大文件，需分批 |
| 15g | ArgumentBuilder.cs | IsInteger/IsString 二元判定→三路 | ~14 | 🟡 |
| 15h | Instraction.Child.cs | IsInteger/IsString 二元判定→三路 | ~7 | 🟡 |
| 15i | VariableEvaluator.cs | IsInteger/IsString 二元判定→三路 | ~6 | 🟡 |
| 15j | ConstantData.cs | IsInteger/IsString 二元判定→三路 | ~9 | 🟡 |
| 15k | Process.ScriptProc.cs + UserDefinedRefMethod.cs + 零散文件 | IsInteger/IsString 二元判定→三路 | ~10 | 🟢 |

**验证**：每批编译通过 + 位标志 switch 残留统计 + IsInteger/IsString 残留统计

##### B.3-16：FunctionMethod ReturnType 迁移

**P3 优先级 — ~675 处 typeof 批量替换**

| 批次 | 文件 | 变更 | 处数 | 风险 |
|------|------|------|------|------|
| 16a | FunctionMethod.cs | ReturnType Type→EraType；argumentTypeArray Type[]→EraType[]；CheckArgumentType 重写 | ~6 | 🔴 基类变更，影响所有子类 |
| 16b | Creator.Method.cs | ReturnType=typeof→EraType；argumentTypeArray=typeof→EraType | ~480 | 🟡 机械替换，量大 |
| 16c | ArgumentBuilder.cs | argumentTypeArray 赋值 typeof→EraType | ~82 | 🟡 |
| 16d | OperatorMethod.cs | ReturnType=typeof→EraType（算子子类） | ~63 | 🟡 |
| 16e | 其他文件 | 零散 typeof 替换 | ~44 | 🟢 |
| 16f | AExpression.cs | GetOperandType() 桥接移除 | ~1 | 🟢 |

**验证**：编译通过 + typeof 残留统计 + 全量回归

### 3.1 每次子任务完成后的验证流水线

```powershell
# B.3-15 批次验证
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 15a -Mode B15

# B.3-16 批次验证
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 16a -Mode B16

# 全量查询（所有指标）
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1

# 仅 B.3-15 指标（IsInteger/IsString/bit flags）
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Mode B15

# 仅 B.3-16 指标（typeof/GetOperandType）
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Mode B16

# 详细行号输出
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Detail -Mode B15
```

### 3.2 继承 AExpression 的子类——不遗漏调用点

B.3-1a 修改 `AExpression` 构造函数后，以下子类必须同步适配：

```
- ConstantTerm (Term.cs)
- SingleTerm (Term.cs)  
- SingleLongTerm (Term.cs)
- SingleStrTerm (Term.cs)
- VariableTerm (VariableTerm.cs)
- VariableArgTerm (VariableStrArgTerm.cs)
- StrFormTerm (StrForm.cs)
- FunctionMethodTerm (FunctionMethodTerm.cs)
- ElementTerm, Array2DElementTerm, Array3DElementTerm (Term.cs)
- CaseStrTerm, ConstantStrTerm, FormStrTerm (Term.cs)
- ... 任何 `new XXXTerm(typeof(...), ...)` 调用点
```

---

## 4. 来源引用索引

| 内容 | 位置 |
|------|------|
| B.3 重构方案（完整） | `../m-emuera/REFACTOR_HANDBOOK.md` B.3 节 |
| 路线图 | `../m-emuera/WORKLOG.md` 开发路线图 Phase 1.3-1.6 |
| 已完成 Phase 0-1 | `../m-emuera/WORKLOG.md` 第十轮 + 第十一轮 |
| B.3-0 日志 | `../m-emuera/WORKLOG.md` 第十二轮 |
| B.3-13/14 日志 + 手册重整 | `../m-emuera/WORKLOG.md` 第二十二轮 |
| 二元判定审查（权威版本） | 本文件 §3.3 |
| EraType.cs | `Emuera/Runtime/Script/EraType.cs` |
| VariableDescriptor.cs | `Emuera/Runtime/Script/VariableDescriptor.cs` |
| VariableToken.GetEraType() | `Emuera/Runtime/Script/Statements/Variable/VariableToken.cs` L209 |
| OperatorMethodManager | `Emuera/Runtime/Script/Statements/Expression/OperatorMethod.cs` L24 |
| SparseArray.cs | `Emuera/Runtime/Script/SparseArray.cs` |
| AExpression.cs | `Emuera/Runtime/Script/Statements/Expression/AExpression.cs` |
| 查询工具 | `tools/b3_query.ps1`（-Mode B15/B16/All -Detail） |
| 验证工具 | `tools/b3_verify.ps1`（-Batch xx -Mode B15/B16/All） |
