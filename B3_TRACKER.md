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

### 0.3 影响范围实测（2026-05-03）

| 指标 | 数量 |
|------|------|
| `typeof(long)`/`typeof(string)` 总调用 | **746** 处 |
| 涉及文件 | **23** 个 |
| `GetOperandType()` 总调用 | **194** 处 |
| 涉及文件 | **16** 个 |

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

B.3-4「存储层」← 1.5~1.6
  ├─ VariableData: dataFloat/dataFloatArray + 存档
  └─ CharacterData: 角色浮点变量

B.3-5「语法层」← 1.5 续
  ├─ 词法分析器: 浮点字面量
  └─ #DIMF 浮点变量声明

B.3-6「验证」
  └─ 端到端测试: 浮点运算 + 存档兼容 + 类型转换
```

---

## 1. 工具脚本

### 1.1 查询脚本 — `tools/b3_query.ps1`

```powershell
# 运行: powershell -File tools/b3_query.ps1
# 输出: typeof(long)/typeof(string) + GetOperandType() 按文件统计
```

### 1.2 验证脚本 — `tools/b3_verify.ps1`

```powershell
# 运行: powershell -File tools/b3_verify.ps1 [-Batch 1a]
# 功能: 编译 + typeof 残留统计 + GetOperandType 残留统计
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
| 6 | 算子子类 `ReturnType = typeof(long/string)` | ~45 处保留（FunctionMethod.Type 属性，待 B.3-3a） | ⬜ |

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
| 批次1 | `ReturnType = typeof(long/string)` | ~246 | 保留（FunctionMethod.ReturnType 仍为 Type） | ⬜ 待 B.3-4 |
| 批次2 | `argumentTypeArray = [typeof(long/string), ...]` | ~124 | 保留（Type[] 字段） | ⬜ 待 B.3-4 |

#### B.3-3b — ArgumentBuilder.cs

**文件**: `Emuera/Runtime/Script/Statements/ArgumentBuilder.cs`

| # | 变更点 | 数量 | 完成 |
|---|--------|------|------|
| 1 | `GetOperandType()` → `GetEraType()` | 17→ 8 处（ExpressionParser/ArgumentBuilder 等已缩减） | ✅ |
| 2 | `typeof(long)` → 保留 | 36 处（ReturnType/argumentTypeArray Type 赋值） | ⬜ 待 B.3-4 |

#### B.3-3c — 零散文件（每文件 ≤8 处 typeof）

| 文件 | typeof | GetOpType | 完成 |
|------|--------|-----------|------|
| `Instraction.Child.cs` | 5 | 0 | ✅ |
| `Utils.cs` (EvilMask) | 8 | 0 | ✅ |
| `Term.cs` | 0 | 0 | ✅ |
| `FunctionMethod.cs` | 5 | 4 | ✅ (GetOpType 已替换) |
| `Process.ScriptProc.cs` | 0 | 1 | ✅ |
| `SparseArray.cs` | 4 | 0 | ⬜ (typeof(T) 泛用保留) |
| `StrForm.cs` | 1 | 0 | ✅ |
| `ExpressionParser.cs` | 1 | 2 | ✅ |
| `Process.CalledFunction.cs` | 0 | 1 | ✅ |
| `VariableToken.cs` | 2 | 0 | ✅ |
| `VariableTerm.cs` | 4 | 0 | ✅ |
| `UserDefinedRefMethod.cs` | 2 | 0 | ✅ |
| `SqlManager.cs` | 2 | 0 | ⬜ |
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
| 1 | 新增 `dataFloat` | `double[]` 标量浮点 | ⬜ |
| 2 | 新增 `dataFloatArray` | `double[][]` 1D 浮点数组 | ⬜ |
| 3 | 新增 `dataFloatArray2D` | `double[][,]` 2D 浮点数组 | ⬜ |
| 4 | 新增 `dataFloatArray3D` | `double[][,,]` 3D 浮点数组 | ⬜ |

#### B.3-4b — 存档序列化扩展

**文件**: `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | `EraSaveDataType.Float` / `.FloatArray` / `.FloatArray2D` / `.FloatArray3D` | 新枚举值 | ⬜ |
| 2 | 保存时写入 Float 段 | 与 Int/Str 段格式一致 | ⬜ |
| 3 | 加载时识别 Float 段 | 旧存档无 Float 段，自动跳过 | ⬜ |

### 2.5 B.3-5「语法层」

#### B.3-5a — 词法分析器浮点字面量

**文件**: `Emuera/Runtime/Script/Parser/LexicalAnalyzer.cs`

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | `ReadDouble(chars)` | 浮点字面量扫描 | ⬜ |
| 2 | `LiteralFloatWord` | Word 子类，`Type = 'F'` | ⬜ |

#### B.3-5b — `#DIMF` 浮点变量声明

| # | 变更点 | 说明 | 完成 |
|---|--------|------|------|
| 1 | ErhLoader 支持 `#DIMF` | 解析浮点变量声明 | ⬜ |
| 2 | ErbLoader 支持 `#DIMF` | 行内声明 | ⬜ |
| 3 | VariableParser 支持 `#DIMF` | 语法解析 | ⬜ |

### 2.6 B.3-6「验证」

| # | 测试 | 验证点 | 完成 |
|---|------|--------|------|
| 1 | 基本浮点运算 | `#DIMF a=3.14; a*2 → 6.28` | ⬜ |
| 2 | Int↔Float 类型转换 | `TOINT(3.14) → 3` | ⬜ |
| 3 | 存档兼容 | 旧存档正常加载 | ⬜ |
| 4 | 现有脚本零影响 | Phase01Bugfix 全 PASS | ⬜ |
| 5 | Creator.Method 全量回归 | 515+ 内置函数无退化 | ⬜ |

---

## 3. 工程规则

### 3.1 每次子任务完成后的验证流水线

```powershell
# 运行: powershell -File tools/b3_verify.ps1 -Batch 1a

# 手动步骤：
# 1. 编译
dotnet build "Emuera\Emuera.csproj" 2>&1

# 2. 查询 typeof 残留
powershell -File tools/b3_query.ps1
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
| EraType.cs | `Emuera/Runtime/Script/EraType.cs` |
| VariableDescriptor.cs | `Emuera/Runtime/Script/VariableDescriptor.cs` |
| OperatorMethodManager | `Emuera/Runtime/Script/Statements/Expression/OperatorMethod.cs` L24 |
| SparseArray.cs | `Emuera/Runtime/Script/SparseArray.cs` |
| AExpression.cs | `Emuera/Runtime/Script/Statements/Expression/AExpression.cs` |
