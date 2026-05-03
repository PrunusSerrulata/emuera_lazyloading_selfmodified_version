# B.3 引入浮点数类型与废弃位编码 —— 任务追踪文档

> 本文件是 B.3 子任务（手册 B.3.1-B.3.7 / 路线图 Phase 1.3-1.6）的**总上下文**和**逐项检查清单**。
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

### 0.3 影响范围实测（2026-05-04 第三次盘点）

| 指标 | B.3-15 范围 | B.3-16 范围 |
|------|-------------|-------------|
| `.IsInteger`/`.IsString` 调用 | **125** 处（16 文件） | — |
| `__INTEGER__`/`__STRING__` 位标志（活跃代码） | **~78** 处（5 文件） | — |
| `typeof(long)` | — | **336** 处（14 文件） |
| `typeof(string)` | — | **154** 处（13 文件） |
| `typeof(double)` | — | **18** 处（8 文件） |
| `typeof(void)` | — | **14** 处（4 文件） |
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

### 1.4 批量替换脚本 — `tools/b3_16_replace.py`

> B.3-16 专用辅助工具：typeof → EraType 机械替换 + 对照清单 + 比对验证。

```powershell
# 预览替换（不修改文件）
python tools/b3_16_replace.py --dry-run

# 预览所有 B.3-16 文件（含 16d~16f）
python tools/b3_16_replace.py --dry-run --all-files

# 生成 CSV 对照清单（394~495 条，含 Batch/File/Line/Pattern/Original/Replaced 列）
python tools/b3_16_replace.py --checklist

# 应用替换到文件（16b + 16c 范围）
python tools/b3_16_replace.py --apply

# 应用替换到所有 B.3-16 文件
python tools/b3_16_replace.py --apply --all-files

# 显示 unified diff
python tools/b3_16_replace.py --diff

# 统计 typeof 残留
python tools/b3_16_replace.py --verify --all-files
```

**替换规则**：
| 原始 | 替换 |
|------|------|
| `typeof(long)` | `EraType.Integer` |
| `typeof(string)` | `EraType.String` |
| `typeof(double)` | `EraType.Float` |
| `typeof(void)` | `EraType.Void` |

**文件范围**：
| 模式 | 文件 |
|------|------|
| 16b | `Creator.Method.cs` |
| 16c | `ArgumentBuilder.cs` |
| 16d | `OperatorMethod.cs` |
| 16e | `VariableToken.cs`, `UserDefinedMethodTerm.cs`, `FunctionMethodTerm.cs`, `UserDefinedRefMethod.cs`, `Instraction.Child.cs`, `AExpression.cs`, `LogicalLineParser.cs`, `StrForm.cs`, `ExpressionParser.cs`, `LogicalLine.cs`, `CaseExpression.cs`, `EvilMask/Utils.cs` |
| 16f | `FunctionMethod.cs` |

**注意**：脚本仅做 `typeof(T)` → `EraType.X` 的文本替换。`Type[]` → `EraType[]` 声明变更、`_ArgType.Type` 属性变更、`CheckArgumentType` 逻辑重写等需在 16a 手动完成后再运行脚本。

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

**B.3 当前状态：B.3-0~15 ✅ 全部完成（含 B.3-13~15 二元判定抽象 + 3 个遗漏项目 B.3.4c/5c/5d）。仅剩 B.3-16（typeof→EraType 批量替换）。**

**最新 typeof 统计（2026-05-04，b3_16_replace.py --verify --all-files）**：
```
typeof(long): 336  (Creator.Method.cs=246, ArgumentBuilder.cs=23, OperatorMethod.cs=48, 其他=19)
typeof(string): 154  (Creator.Method.cs=116, ArgumentBuilder.cs=17, OperatorMethod.cs=4, 其他=17)
typeof(double):  18  (OperatorMethod.cs=10, VariableToken.cs=2, FunctionMethod.cs=1, 其他=5)
typeof(void)  :  14  (ArgumentBuilder.cs=10, AExpression.cs=1, LogicalLine.cs=2, CaseExpression.cs=1)
总计: 522 处（不含 SparseArray.cs 6处🔒 + SqlManager.cs 2处🔒）
```
> 以上均为 B.3-16 范围（ReturnType/argumentTypeArray 的 Type 声明 + 零散 typeof 比较）。

### 3.3 B.3-15 完成总结（原二元判定抽象，2026-05-04 终审全部通过）

> **B.3-13~15 已全部完成，以下仅保留摘要供 B.3-16 上下文参考。**

6 大二元判定模式均已消灭：
| 模式 | 原状 | 处理 | 状态 |
|------|------|------|------|
| A. 位标志 `__INTEGER__`/`__STRING__` 判定 | ~78 处活跃 | → `Descriptor.Kind` | ✅ B.3-15 |
| B. `IsInteger`/`IsString` 属性判定 | 50+28 处 | → Float 分支 / Descriptor 驱动 | ✅ B.3-15 |
| C. `VariableType == typeof(...)` | 0 处 | ✅ 已在 B.3-14 清除 | ✅ |
| D. `ReturnType`/`argumentTypeArray` Type | 522 处 | → B.3-16（下一任务） | ⬜ B.3-16 |
| E. `VariableCode` 位标志 switch | 2 处（必要回退） | → Descriptor 查询 | ✅ B.3-15 |
| F. 隐式二元判定 | ~92 处 | → 全部补 Float 分支 | ✅ B.3-15 |

B.3-15 分批（15a-15k）：全部 ✅。
遗漏项目（B.3.4c/5c/5d）：全部 ✅（2026-05-04 完成）。

---

## 4. B.3-16 实施方案：typeof→EraType 批量替换

### 4.1 执行策略

| 批次 | 文件 | 处数 | 策略 | 风险 |
|------|------|------|------|------|
| **16a** | FunctionMethod.cs | ~6 | **手动** — ReturnType/argumentTypeArray/EraType 属性 + CheckArgumentType/GetReturnValue | 🔴 基类 | ✅ |
| **16b** | Creator.Method.cs | 364 | **脚本** `b3_16_replace.py --apply` | 🟡 量大机械 |
| **16c** | ArgumentBuilder.cs | 51 | **脚本** + 手动加 `'F'` 分支 | 🟡 |
| **16d** | OperatorMethod.cs | 62 | **脚本** `--apply --all-files` | 🟡 |
| **16e** | 零散 12 文件 | 39 | **手动**（每文件 ≤8 处） | 🟢 |
| **16f** | AExpression + 桥接 | 5 | **手动** — 评估可移除性 | 🟢 |

### 4.2 16a 基类变更详解（FunctionMethod.cs）

| 行号 | 当前 | 目标 |
|------|------|------|
| L13 | `public Type ReturnType` | `public EraType ReturnType` |
| L14 | `protected Type[] argumentTypeArray` | `protected EraType[] argumentTypeArray` |
| L56 | `_ArgType.Type` → `typeof(long/string)` | `_ArgType.EraType` → `EraType.Integer/String/Float` |
| L224 | `type == typeof(string)` | `type == EraType.String` |
| L287 | `argumentTypeArray[i] == typeof(string)` | `argumentTypeArray[i] == EraType.String` |
| L307-309 | `if (ReturnType == typeof(long)) ... else if (ReturnType == typeof(double))` | `switch (ReturnType) { case EraType.Integer: ... case EraType.Float: ... }` |

### 4.3 16c 动态构建路径（ArgumentBuilder.cs）

```csharp
// 当前 → 目标
if (argstr[i] == 'I') types[i] = typeof(long);  → EraType.Integer;
else if (argstr[i] == 'S') types[i] = typeof(string); → EraType.String;
// 新增
else if (argstr[i] == 'F') types[i] = EraType.Float;
```

### 4.4 16f 桥接评估（AExpression.GetOperandType()）

当前 82 处 `GetOperandType()` 调用中 59 处在 Creator.Method.cs 注释，23 处在 11 个文件中。
16e 完成后评估是否移除桥接。

### 4.5 工具与验证

```powershell
python tools/b3_16_replace.py --dry-run --all-files  # 预览
python tools/b3_16_replace.py --apply                 # 16b+16c
python tools/b3_16_replace.py --apply --all-files     # 全部
python tools/b3_16_replace.py --verify --all-files    # 残留统计
```

### 3.1 B.3-16 验证流水线

```powershell
# B.3-15 批次验证
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 15a -Mode B15

# B.3-16 批次验证
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 16a -Mode B16

# B.3-16 typeof 残留统计（Python 版，更精确）
python tools/b3_16_replace.py --verify --all-files

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

## 5. 来源引用索引

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
| 批量替换工具 | `tools/b3_16_replace.py`（--dry-run/--checklist/--apply/--verify/--diff） |

---

## 6. m-emuera 迁移上下文

> 本节记录 B.3 各子任务在 emuera-lazyloading 完成后，迁移到 m-emuera 时需要的注意事项。
> m-emuera 的代码结构、命名空间、项目配置与 emuera-lazyloading 有差异，迁移时需逐项确认。

### 6.1 B.3-15a 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/EraType.cs` | `src/MEmuera.Core/Runtime/Script/EraType.cs` | ⚠️ m-emuera 已有误创建版本 | m-emuera 版本内容相同，但需确认是否与 B.3-1~14 的其他变更一起同步 |
| `Emuera/Runtime/Script/VariableDescriptor.cs` | `src/MEmuera.Core/Runtime/Script/VariableDescriptor.cs` | ⚠️ m-emuera 已有误创建版本 | m-emuera 版本缺少部分 Register 条目（JUEL 之后的变量），需从 lazyloading 同步完整版 |
| `Emuera/Runtime/Script/Statements/Variable/VariableIdentifier.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Variable/VariableIdentifier.cs` | ⚠️ m-emuera 已有误创建版本 | m-emuera 版本已应用 B.3-15a 修改，但基于未同步 B.3-1~14 的旧代码 |

**迁移注意事项**:

1. **m-emuera 的 AExpression 仍使用 `Type` 而非 `EraType`**：B.3-1~14 的修改尚未同步到 m-emuera。迁移 B.3-15a 之前，应先将 B.3-0~14 整体同步到 m-emuera，否则 VariableIdentifier 的 Descriptor 查询可能与其他文件的旧 API 不兼容。

2. **VariableDescriptorTable.Register 条目不完整**：m-emuera 误创建的 VariableDescriptor.cs 只注册了部分变量（到 JUEL 为止），缺少 RELATION/EQUIP/TEQUIP/STAIN/GOTJUEL/NOWEX/DOWNBASE/CUP/CDOWN/TCVAR 等 CharacterData 变量，以及 NAME/CALLNAME/NICKNAME/MASTERNAME/CSTR/CDFLAG/DITEMTYPE/DA~DE/TA~TB 等。迁移时必须从 lazyloading 同步完整版。

3. **GetDescriptorByCode() 方法**：B.3-15a 新增的 `VariableDescriptorTable.GetDescriptorByCode()` 方法在 m-emuera 误创建版本中已包含，但该方法遍历全字典做线性查找，性能不佳。未来应改为 `Dictionary<VariableCode, VariableDescriptor>` 反向索引。

4. **extSaveListDic 键变更**：从 `Dictionary<VariableCode, List<VariableCode>>` 改为 `Dictionary<(VariableKind, VariableDimension), List<VariableCode>>`。m-emuera 的 VariableData.cs 和 CharacterData.cs 仍使用 `GetExtSaveList(VariableCode)` 旧 API 调用，该重载已保留为向后兼容层（内部转换为 Descriptor 查询），无需立即修改。

5. **Config.StrComper 依赖**：VariableDescriptorTable 使用 `new Config.Config.StrComper)` 作为字典比较器。m-emuera 的 Config 命名空间可能不同，需确认。

6. **命名空间差异**：emuera-lazyloading 使用 `MinorShift.Emuera.Runtime.Script`，m-emuera 使用相同命名空间但项目结构不同（`src/MEmuera.Core/` 子目录）。using 语句无需修改。

### 5.2 B.3-15b 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/Statements/Variable/VariableToken.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Variable/VariableToken.cs` | ⬜ 待迁移 | IsSavedata 位标志 switch→Descriptor 查询 + VariableType [Obsolete] |

**迁移注意事项**:

1. **IsSavedata 逻辑等价性**：新代码将位标志操作（`Code & VariableCode.__EXTENDED__` 等）替换为 Descriptor 属性查询（`_descriptor.Attributes.HasFlag(VariableAttribute.Extended)` 等），语义完全等价。VarCodeInt 计数阈值比较保留不变。

2. **VariableType [Obsolete]**：标记为 `[Obsolete("Use Descriptor.IsInteger/IsString/IsFloat or GetEraType() instead")]`。m-emuera 中无外部 `.VariableType` 引用（已全局搜索确认），标记 Obsolete 不会产生编译警告。

3. **依赖 B.3-1~14**：VariableToken 构造函数中 `_descriptor` 字段在 B.3-1b 中引入。迁移 B.3-15b 前需确保 B.3-1b（VariableToken 位标志→Descriptor 查询）已同步到 m-emuera。

4. **CharaVariableToken / UserDefinedVariableToken 子类**：这两个子类的构造函数调用 `base(varCode, varData)`，IsSavedata 逻辑在基类构造函数中执行，子类无需修改。

### 5.3 推荐迁移顺序

1. 先将 B.3-0~14 整体同步到 m-emuera（AExpression Type→EraType、OperatorMethod typeof→EraType、Creator.Method GetOperandType→EraType 等）
2. 同步完整的 VariableDescriptor.cs（含所有 Register 条目 + GetDescriptorByCode）
3. 同步 VariableIdentifier.cs（B.3-15a 修改）
4. 编译验证 + 运行时验证

### 5.3 B.3-15c 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/Statements/Variable/VariableData.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Variable/VariableData.cs` | ⬜ 待迁移 | GetExtSaveList 位标志→Descriptor + userDefinedSaveVarList 6→9 + type++ 三路分派 |
| `Emuera/Runtime/Utils/EraDataStream.cs` | `src/MEmuera.Core/Runtime/Utils/EraDataStream.cs` | ⬜ 待迁移 | 新增 ReadDoubleArrayExtended/ReadDoubleArray2DExtended/ReadDoubleArray3DExtended + WriteExtended(double[]) 等 |

**迁移注意事项**:

1. **EraDataStream 新增方法**：ReadDoubleArrayExtended/ReadDoubleArray2DExtended/ReadDoubleArray3DExtended 及对应的 WriteExtended 重载（double[]/double[,]/double[,,]）。这些方法遵循现有 String/Int64 数组方法的相同模式，迁移时直接复制即可。

2. **userDefinedSaveVarList 槽位布局变更**：
   - 旧布局（6 槽）：0=String1D, 1=Integer1D, 2=String2D, 3=Integer2D, 4=String3D, 5=Integer3D
   - 新布局（9 槽）：0=String1D, 1=Integer1D, 2=Float1D, 3=String2D, 4=Integer2D, 5=Float2D, 6=String3D, 7=Integer3D, 8=Float3D
   - 迁移时需同步更新 SaveToStreamExtended/LoadFromStreamExtended/SaveGlobalToStream1808/LoadGlobalFromStream1808 四个方法

3. **type 计算三路分派**：`ret.Dimension * 2 - 2; if (!ret.IsString) type++` → `(ret.Dimension - 1) * 3 + eraTypeOffset`（String=0, Integer=1, Float=2）

4. **GetExtSaveList 调用全部替换**：所有 `GetExtSaveList(VariableCode.__XXX__)` 和 `GetExtSaveList(VariableCode.__ARRAY_XD__ | VariableCode.__XXX__)` 调用已替换为 `GetExtSaveList(VariableKind.XXX, VariableDimension.XXX)`。旧 API 重载保留为向后兼容层。

5. **依赖 B.3-1~14**：VariableData 中 `ret.IsString`/`ret.IsInteger`/`ret.IsFloat` 属性在 B.3-1b 中引入。迁移 B.3-15c 前需确保 B.3-1b 已同步到 m-emuera。

### 5.4 B.3-15d 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/Statements/Variable/CharacterData.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Variable/CharacterData.cs` | ⬜ 待迁移 | 位标志 switch→Descriptor + GetExtSaveList 改造 + IsInteger/IsString→GetEraType() 三路 |

**迁移注意事项**:

1. **CharacterVarLength 位标志 switch 消灭**：原 `code & (__ARRAY_1D__ | __ARRAY_2D__ | __ARRAY_3D__ | __INTEGER__ | __STRING__)` 位标志 switch 替换为 `VariableDescriptor.FromCode(code, "")` + `desc.IsInteger`/`desc.IsString` + `desc.Dimension` 枚举 switch。

2. **GetExtSaveList 全量替换**：所有 `GetExtSaveList(VariableCode.__CHARACTER_DATA__ | VariableCode.__XXX__)` 调用替换为 `GetExtSaveList(VariableKind.XXX, VariableDimension.XXX)`。注意 `__CHARACTER_DATA__` 标志在 extSaveListDic 中不参与键计算（键为 `(Kind, Dimension)`），因此 CharacterData 和 VariableData 的 GetExtSaveList 调用返回相同列表。

3. **SaveToStreamBinary 位标志 switch 消灭**：原 `code & (__ARRAY_1D__ | __ARRAY_2D__ | __ARRAY_3D__ | __STRING__ | __INTEGER__)` 位标志 switch 替换为 `VariableDescriptor.FromCode(code, "")` + `desc.IsInteger`/`desc.IsString` + `desc.Dimension` 枚举 switch。

4. **LoadFromStreamBinary IsInteger/IsString→GetEraType() 三路**：所有 `vToken.IsInteger`/`vToken.IsString` 替换为 `vToken.GetEraType() != EraType.Integer`/`vToken.GetEraType() != EraType.String`。CopyTo 和 SetSortKey 中的 `var.IsString`/`sortkey.IsString` 也替换为 `GetEraType()` 三路判定。

5. **Float 段处理现状**：LoadFromStreamBinary 中 Float/FloatArray/FloatArray2D/FloatArray3D 段目前仅跳过读取（因 CharacterData 尚无 Float 数据字段和 `__COUNT_CHARACTER_FLOAT__` 常量）。完整 Float 支持需等 B.3.4c（CharacterData 角色浮点变量支持）实现后补充。

6. **依赖 B.3-15a/15b**：CharacterData 使用 `VariableIdentifier.GetExtSaveList(VariableKind, VariableDimension)` 新重载（15a 引入）和 `VariableToken.GetEraType()`（15b 引入）。迁移 B.3-15d 前需确保 15a/15b 已同步到 m-emuera。

### 5.5 B.3-15e 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/VariableDescriptor.cs` | `src/MEmuera.Core/Runtime/Script/VariableDescriptor.cs` | ⬜ 待迁移 | FromCode() 先查注册表 + _codeIndex 反向索引 |

**变更明细**:

1. **`VariableDescriptor.FromCode()` 注册表优先**：在位标志推断之前，先调用 `VariableDescriptorTable.TryGetDescriptorByCode(code, out var registered)` 查注册表。如果注册表中有匹配的 `VariableCode`，直接返回注册表中的 Descriptor（精确注册，不受位标志限制）；找不到才回退到位标志推断。

2. **`VariableDescriptorTable._codeIndex` 反向索引**：新增 `Dictionary<VariableCode, VariableDescriptor>` 字段，在 `Register()` 中同步填充。替代原 `GetDescriptorByCode()` 的线性查找（O(n)→O(1)）。

3. **`VariableDescriptorTable.TryGetDescriptorByCode()` 新方法**：`_codeIndex.TryGetValue(code, out descriptor)`，供 `FromCode()` 调用。

4. **`VariableDescriptorTable.GetDescriptorByCode()` 优化**：从线性查找改为 `_codeIndex.TryGetValue()`，性能从 O(n) 提升到 O(1)。

**迁移注意事项**:

1. **无外部行为变更**：对于当前已注册的变量（全部为 Integer/String），`FromCode()` 的返回值与修改前完全一致（因为注册表中的 Descriptor 与位标志推断结果相同）。只有未来注册 Float 变量时，`FromCode()` 才会返回正确的 `Kind = Float` 而非位标志推断的 `Kind = String/Integer`。

2. **`FromCode()` 回退路径保留**：位标志推断代码仍然保留在 `FromCode()` 中，作为注册表未覆盖的 VariableCode 的回退。VariableDescriptor.cs 中仍有 1 处 `__STRING__` 位标志引用，这是预期行为。

3. **依赖 B.3-0**：`VariableDescriptorTable` 和 `VariableDescriptor` 在 B.3-0 中引入。迁移 B.3-15e 前需确保 B.3-0 已同步到 m-emuera。

4. **m-emuera 误创建版本**：m-emuera 的 VariableDescriptor.cs 已有误创建版本（缺少部分 Register 条目），迁移时必须从 lazyloading 同步完整版。

### 5.6 B.3-15f 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Function/Creator.Method.cs` | ⬜ 待迁移 | 20 处 IsInteger/IsString→GetEraType() 三路 + Float 分支 |
| `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Variable/VariableEvaluator.cs` | ⬜ 待迁移 | 新增 FindChara(double) 重载 |

**变更明细**:

1. **GetVarMethod**：`!var.IsInteger` → `var.GetEraType() != EraType.Integer`
2. **GetVarsMethod**：`!var.IsString` → `var.GetEraType() != EraType.String`
3. **ExistVarMethod**：`IsInteger res|=1; IsString res|=2` → `switch GetEraType()` + Float bit=32
4. **GetSortedIndices**：`if (baseVar.IsInteger)...else...` → `switch GetEraType()` + Float 分支（SparseArray\<double\>）
5. **SetVarMethod**：`if (var.IsString)...else...` → `switch GetEraType()` + Float 分支（Int→Float 隐式提升）
6. **VarSetExMethod**：`if (var.IsString)...else...` → `switch GetEraType()` + Float 分支（1D/2D/3D 数组全支持）
7. **FindCharaMethod**：`if (varID.IsString)...else...` → `switch GetEraType()` + Float 分支 + VariableEvaluator.FindChara(double) 新重载
8. **ArrayMultiSortMethod**：`varTerm.Identifier.IsInteger` + `term.IsInteger`（4 处）→ `switch GetEraType()` + Float 分支（1D/2D/3D 排序全支持）
9. **RegexMethod**：`!varTerm.Identifier.IsString` → `varTerm.Identifier.GetEraType() != EraType.String`
10. **GetMethMethod**：`!term.IsInteger` → `term.GetEraType() != EraType.Integer`
11. **GetMethsMethod**：`!term.IsString` → `term.GetEraType() != EraType.String`
12. **ExistMethMethod**：`IsInteger res|=1; IsString res|=2` → `switch GetEraType()` + Float bit=32
13. **EvalMethod**：`term.IsInteger` → `term.GetEraType() == EraType.Integer` + Float→long 截断
14. **EvalSMethod**：`term.IsString` → `term.GetEraType() == EraType.String` + Float→string 格式化

**未修改（UI 层）**:
- L8111 `ConsoleButtonString.IsInteger`：UI 层按钮输入类型，与 VariableDescriptor 无关，不属于 B.3-15 范围

**未修改（注释）**:
- 11 处 `IsInteger`/`IsString` 在注释中（旧 CheckArgumentType 代码），无需修改

**VariableEvaluator.FindChara(double) 新增**:
- 新增 `FindChara(VariableToken, long, double, long, long, bool)` 重载
- 与 string/long 重载结构一致，使用 `fvp.GetFloatValue(null)` 比较

### 5.7 B.3-15g 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/Statements/ArgumentBuilder.cs` | `src/MEmuera.Core/Runtime/Script/Statements/ArgumentBuilder.cs` | ⬜ 待迁移 | 14 处 IsInteger/IsString→GetEraType() 三路 + Float 分支 |
| `Emuera/Runtime/Script/Statements/Argument.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Argument.cs` | ⬜ 待迁移 | 新增 ConstFloat 字段 + SpSetArrayArgument(double[]) 构造函数 |

**变更明细（ArgumentBuilder.cs — 14 处修改）**:

1. **SP_TIMES**：`varTerm.IsString` → `varTerm.GetEraType() == EraType.String`（拒绝字符串变量，允许 Float）
2. **SP_ARRAYSORT**：`!term3.IsInteger` → `term3.GetEraType() != EraType.Integer`（索引必须整数）
3. **SP_ARRAYSORT**：`!term4.IsInteger` → `term4.GetEraType() != EraType.Integer`（索引必须整数）
4. **SP_SET（赋值主分支）**：`if (varTerm.IsInteger)...else...` → `switch (varTerm.GetEraType())` 三路：
   - Integer+Float 共享数值赋值逻辑，Float 使用 ConstFloat/ConstFloatList
   - String 保持原有字符串赋值逻辑
   - Float 的 `++`/`--` 使用 `ConstFloat = ±1.0`
   - Float 的 RHS 类型检查：拒绝 String，允许 Integer/Float
5. **SP_SET（多值赋值）**：`!srcTerms[i].IsInteger` → `srcTerms[i].GetEraType() == EraType.String`（拒绝字符串到数值变量）
6. **SP_SET（单值赋值）**：`!srcTerms[0].IsInteger` → `srcTerms[0].GetEraType() == EraType.String`
7. **SP_SET（字符串赋值）**：`srcTerms[0].IsInteger` → `srcTerms[0].GetEraType() != EraType.String`（拒绝数值到字符串变量）
8. **SP_SET（字符串多值赋值）**：`srcTerms[i].IsInteger` → `srcTerms[i].GetEraType() != EraType.String`
9. **SP_INPUTS**：`!terms[0].IsInteger` → `terms[0].GetEraType() != EraType.Integer`（超时值必须整数）
10. **SP_FOR**：`!start.IsInteger` → `start.GetEraType() != EraType.Integer`（循环起始必须整数）
11. **SP_VARSET（1D）**：`varTerm.IsString` → `switch (varTerm.GetEraType())` + Float 默认值 `SingleFloatTerm(0.0)`
12. **SP_VARSET（2D）**：`varTerm.IsString` → `switch (varTerm.GetEraType())` + Float 默认值 `SingleFloatTerm(0.0)`
13. **SP_REF（REF方法名）**：`name.IsInteger` → `name.GetEraType() != EraType.String`（方法名必须字符串）
14. **SP_COPYARRAY**：`(vars[0].IsInteger && vars[1].IsString) || (vars[0].IsString && vars[1].IsInteger)` → `vars[0].GetEraType() != vars[1].GetEraType()`（类型必须一致）

**变更明细（Argument.cs）**:
- `Argument` 基类新增 `public double ConstFloat` 字段
- `SpSetArrayArgument` 新增 `SpSetArrayArgument(VariableTerm, List<AExpression>, double[])` 构造函数 + `ConstFloatList` 字段

### 5.8 B.3-15i 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Variable/VariableEvaluator.cs` | ⬜ 待迁移 | 6 处 IsInteger/IsString→三路 + Float 数组操作 |

**变更明细（VariableEvaluator.cs — 6 处修改）**:

1. **SetValueAllEachChara(long)**：`if (!p.Identifier.IsInteger)` → `if (!p.Identifier.IsInteger && !p.Identifier.IsFloat)`（允许 Float 变量接收 long 值，隐式转换）
2. **SetValueAllEachChara(string)**：`if (!p.Identifier.IsString)` — 无修改（Float 变量不应接收字符串值，现有检查已正确拒绝）
3. **GetJoinedStr**：`if (p.IsString)...else...` → `if (p.IsString)...else if (p.IsFloat)...else...` 三路：
   - Float 分支使用 `GetFloatValue()` + `.ToString()` 格式化
   - 支持 1D/2D/3D 数组维度
4. **RemoveArray**：`if (p.Identifier.IsInteger)` → `if (p.Identifier.IsInteger || p.Identifier.IsFloat)`：
   - 新增 `SparseArray<double>` 分支（RemoveRange）
   - 新增 `double[]` 分支（Buffer.BlockCopy，与 long[] 同理）
5. **SortArray**：`if (p.Identifier.IsInteger)` → `if (p.Identifier.IsInteger || p.Identifier.IsFloat)`：
   - 新增 `SparseArray<double>` 分支（Sort）
   - 新增 `double[]` 分支（AsSpan + Sort + Reverse）
6. **CopyArray**：`if (var1.IsInteger)` → `if (var1.IsInteger || var1.IsFloat)`：
   - 1D：新增 `SparseArray<double>`×2 / `SparseArray<double>`×`double[]` / `double[]`×2 分支
   - 2D：新增 `var1.IsFloat` 分支（`double[,]` 复制）
   - 3D：新增 `var1.IsFloat` 分支（`double[,,]` 复制）

**设计决策**：
- Integer 和 Float 共享"数值"分支（`IsInteger || IsFloat`），因为数组操作逻辑结构相同
- Float 数组操作使用 `Buffer.BlockCopy`（与 long[] 相同，8 字节对齐）
- `SparseArray<double>` 分支在 `is` 模式匹配中优先于 `double[]` 检查

### 5.9 B.3-15j 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/Data/ConstantData.cs` | `src/MEmuera.Core/Runtime/Script/Data/ConstantData.cs` | ⬜ 待迁移 | 9 处 IsInteger/IsString→三路 + Float 长度数组 |

**变更明细（ConstantData.cs — 9 处修改 + 5 个新字段）**:

**新增字段**（5 个 Float 长度数组）:
- `public int[] VariableFloatArrayLength`
- `public long[] VariableFloatArray2DLength`
- `public long[] VariableFloatArray3DLength`
- `public int[] CharacterFloatArrayLength`
- `public long[] CharacterFloatArray2DLength`

**初始化**（`setDefaultArrayLength()`）:
- 5 个 Float 长度数组均初始化为空数组 `[]`（当前无 Float 内置变量，无需预分配）

**SetVariableSize 三路分派**（9 处 `if (id.IsInteger)...else if (id.IsString)...` → 加 `else if (id.IsFloat)` 分支）:

1. CharacterData + Array2D：`id.IsFloat` → `CharacterFloatArray2DLength[id.CodeInt] = length64`
2. CharacterData + 1D：`id.IsFloat` → `CharacterFloatArrayLength[id.CodeInt] = length`
3. Non-Character + Array2D：`id.IsFloat` → `VariableFloatArray2DLength[id.CodeInt] = length64`
4. Non-Character + Array3D：`id.IsFloat` → `VariableFloatArray3DLength[id.CodeInt] = length3d`
5. Non-Character + 1D：`id.IsFloat` → `VariableFloatArrayLength[id.CodeInt] = length`

**设计决策**：
- Float 长度数组初始化为空数组而非与 Integer 相同大小，因为当前无 Float 内置变量
- 若未来添加 Float 内置变量（VariableCode 增加 `__FLOAT__` 位标志 + `__COUNT_FLOAT_*` 常量），需同步更新初始化代码
- `id.IsFloat` 检查位于 `id.IsInteger` 之后、`id.IsString` 之前，保持 Integer→Float→String 的优先级顺序

### 5.10 B.3-15k 迁移记录

**已完成文件（emuera-lazyloading → m-emuera 映射）**:

| emuera-lazyloading 文件 | m-emuera 对应文件 | 迁移状态 | 注意事项 |
|---|---|---|---|
| `Emuera/Runtime/Script/Process.ScriptProc.cs` | `src/MEmuera.Core/Runtime/Script/Process.ScriptProc.cs` | ⬜ 待迁移 | 3 处 IsInteger/IsString→三路 |
| `Emuera/Runtime/Script/Statements/Function/UserDefinedRefMethod.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Function/UserDefinedRefMethod.cs` | ⬜ 待迁移 | 3 处 IsInteger/IsString→三路 |
| `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs` | `src/MEmuera.Core/Runtime/Script/Statements/Variable/VariableEvaluator.cs` | ⬜ 待迁移 | 新增 ShiftArray(double) 重载 |

**变更明细（Process.ScriptProc.cs — 3 处修改）**:

1. **ARRAYSHIFT**：`if (dest.Identifier.IsInteger)...else...` → `if (dest.Identifier.IsFloat)...else if (dest.Identifier.IsString)...else...` 三路：
   - Float 分支使用 `GetFloatValue()` + `ShiftArray(dest, shift, def, start, num)` (double 重载)
   - Integer 分支作为 default（else），保持向后兼容
2. **ARRAYCOPY（变量名路径）**：`(vars[0].IsInteger && vars[1].IsString) || (vars[0].IsString && vars[1].IsInteger)` → `vars[0].GetEraType() != vars[1].GetEraType()`（统一类型不匹配检测，覆盖 Float↔String、Float↔Integer）
3. **ARRAYCOPY（字符串名路径）**：同上

**变更明细（UserDefinedRefMethod.cs — 3 处修改）**:

1. **REF 参数类型标记**：`if (vToken.IsInteger)` → `if (vToken.IsInteger || vToken.IsFloat)`（Float 映射到 Int arg 类型，因为 UserDifinedFunctionDataArgType 无单独 Float 枚举值）
2. **非 REF 参数类型校验（Int）**：`vToken.IsInteger && ...` → `(vToken.IsInteger || vToken.IsFloat) && ...`
3. **非 REF 参数类型校验（Str）**：无修改（`vToken.IsString` 正确拒绝 Float）

**变更明细（VariableEvaluator.cs — 新增 1 个重载）**:

- **ShiftArray(double)**：与 `ShiftArray(long)` 逻辑完全对称，新增 `SparseArray<double>` + `double[]` 分支，使用 `Buffer.BlockCopy`（8 字节对齐）

**设计决策**：
- Float 变量在 REF 函数签名中视为"数值型"（映射到 Int arg 类型），因为 `UserDifinedFunctionDataArgType` 枚举尚未添加 Float 变体
- ARRAYCOPY 类型检测从具体类型比较改为 `GetEraType()` 统一判等，可覆盖未来新增类型
- ShiftArray(double) 与 ShiftArray(long) 共享相同算法框架，仅数据类型不同
