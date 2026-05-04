# B.3 引入浮点数类型与废弃位编码 —— 任务追踪文档

> 本文件是 B.3 子任务（手册 B.3.1-B.3.7 / 路线图 Phase 1.3-1.6）的**总上下文**和**实施状态**。
> 来源：`../m-emuera/REFACTOR_HANDBOOK.md` B.3 节 + `../m-emuera/WORKLOG.md` 路线图 + 源码实测盘点。
> 配套工具：`tools/b3_query.ps1`（查询）、`tools/b3_verify.ps1`（验证）、`tools/b3_16_replace.py`（B.3-16 批量替换）

---

## 目录

- [B.3 引入浮点数类型与废弃位编码 —— 任务追踪文档](#b3-引入浮点数类型与废弃位编码--任务追踪文档)
  - [目录](#目录)
  - [0. 总览](#0-总览)
    - [0.1 目标](#01-目标)
    - [0.2 战略决策（摘自手册）](#02-战略决策摘自手册)
    - [0.3 实施完成状态总表](#03-实施完成状态总表)
    - [0.4 最终残留 typeof 统计（全部必要保留）](#04-最终残留-typeof-统计全部必要保留)
    - [0.5 6 大二元判定模式——全部消灭](#05-6-大二元判定模式全部消灭)
  - [1. 冒烟测试结果（2026-05-04）](#1-冒烟测试结果2026-05-04)
    - [1.1 测试覆盖](#11-测试覆盖)
    - [1.2 已验证的核心能力](#12-已验证的核心能力)
  - [2. 全量审计摘要（归纳自 B3\_FLOAT\_AUDIT.md）](#2-全量审计摘要归纳自-b3_float_auditmd)
    - [2.1 审计分类汇总](#21-审计分类汇总)
    - [2.2 已确认 Float 支持的函数/指令](#22-已确认-float-支持的函数指令)
    - [2.3 不涉及 Float 的函数类别（N/A）](#23-不涉及-float-的函数类别na)
  - [3. 工具脚本](#3-工具脚本)
    - [3.1 查询脚本 — `tools/b3_query.ps1`](#31-查询脚本--toolsb3_queryps1)
    - [3.2 验证脚本 — `tools/b3_verify.ps1`](#32-验证脚本--toolsb3_verifyps1)
    - [3.3 批量替换脚本 — `tools/b3_16_replace.py`](#33-批量替换脚本--toolsb3_16_replacepy)
  - [4. 浮点数功能补全（Batch 1-6）](#4-浮点数功能补全batch-1-6)
    - [4.0 战略决策：LOCAL/ARG/GLOBAL 的 Float 版本](#40-战略决策localargglobal-的-float-版本)
    - [4.1 内置数学函数 Float 重载 ✅ 已完成](#41-内置数学函数-float-重载--已完成)
    - [4.2 数组操作函数 Float 支持](#42-数组操作函数-float-支持)
    - [4.3 反射/动态调用函数 Float 支持 ✅ 已完成](#43-反射动态调用函数-float-支持--已完成)
    - [4.4 DT (DataTable) Float 支持 ✅ 已完成](#44-dt-datatable-float-支持--已完成)
    - [4.5 SQL Float 支持 ✅ 已完成](#45-sql-float-支持--已完成)
    - [4.6 BAR/BARL Float 参数重载 ✅ 已完成](#46-barbarl-float-参数重载--已完成)
    - [4.7 SAVEDATA/CHARADATA Float 支持 ✅ 已完成](#47-savedatacharadata-float-支持--已完成)
    - [4.8 函数参数 Float 支持（ARGF）✅ 已完成](#48-函数参数-float-支持argf-已完成)
    - [4.9 函数返回值 Float 支持（RESULTF）✅ 已完成](#49-函数返回值-float-支持resultf-已完成)
    - [4.10 #FUNCTIONF 用户自定义 Float 返回函数 ✅ 已完成](#410-functionf-用户自定义-float-返回函数--已完成)
    - [4.11 任务依赖关系](#411-任务依赖关系)
  - [5. m-emuera 迁移上下文](#5-m-emuera-迁移上下文)
    - [5.1 迁移状态总表](#51-迁移状态总表)
    - [5.2 推荐迁移顺序](#52-推荐迁移顺序)
    - [5.3 关键差异点](#53-关键差异点)
  - [6. 来源引用索引](#6-来源引用索引)

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
> 引擎版本：Emuera.NET SkiaSharp 1824+v24+EMv18+EEv55 Lazyloadingv4.1

### 1.1 测试覆盖

**第一轮（基础 + 回归）** — 日志：`d:\eratw-chs\20260504-043622.log`

| 测试模块 | 测试项数 | 结果 |
|----------|---------|------|
| B.3-1 类型系统基础设施 | 10 | **全部 PASS** |
| B.3-5 浮点变量基本运算 | 5 | **全部 PASS** |
| B.3-REGRESSION 踩坑回归 | 22 | **全部 PASS** |
| Phase01Bugfix 回归验证 | 18 | **全部 PASS** |
| **合计** | **55** | **0 FAIL, 0 ERROR** |

**第二轮（全量端到端）** — 日志：`d:\eratw-chs\20260504-230148.log`

| 测试模块 | 测试项数 | 结果 |
|----------|---------|------|
| B.3-6 全量端到端测试 | — | **全部 PASS** |
| B.3-1 类型系统基础设施 | 10 | **全部 PASS** |
| B.3-5 浮点变量基本运算 | 5 | **全部 PASS** |
| B.3-REGRESSION 踩坑回归 | 22 | **全部 PASS** |
| B.3-8/9/10 浮点数数组/函数参数/返回值 | 8 | **全部 PASS** |
| Phase01Bugfix 回归验证 | 18 | **全部 PASS** |
| **合计** | **63+** | **0 FAIL, 0 ERROR** |

**第二轮新增验证项**：
- LOCALF:0/1 读写正常
- LOCALF 数组运算正常
- RESULTF 读写正常
- ARGF 函数参数传递正常
- Int→Float 参数自动提升正常
- #FUNCTIONF 返回 Float 正常
- #FUNCTIONF 返回 Int→Float 正常
- VARSETEX Float 数组填充正常

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

## 2. 全量审计摘要（归纳自 B3_FLOAT_AUDIT.md）

> **B3_FLOAT_AUDIT.md 已标记为可删除**，其全部内容已归纳至本节。
> 审计范围：`Creator.cs`（~200+ 函数）、`Creator.Method.cs`（~200+ 函数）、`FunctionIdentifier.cs`（~100+ 指令）、`Instraction.Child.cs`（~100+ 指令）。

### 2.1 审计分类汇总

| 类别 | 函数/指令数 | 需 Float 支持 | 已实现 | N/A | 备注 |
|------|-----------|-------------|--------|-----|------|
| 1. 数学函数 | 16 | 12 | 12 ✅ | 4 | 4 个 UNCHECKED_* 仅整数有意义 |
| 2. 数组操作函数 | 22 | 16 | 16 ✅ | 6 | 6 个 GETBIT/GETNUM/ARRAYMSORT 等仅整数 |
| 3. 反射/动态调用 | 10 | 3 | 3 ✅ | 7 | GETVARF/GETMETHF/EVALF 新增；SETVAR/EXISTMETH 已有 |
| 4. 类型转换 | 5 | 0 | — | 5 | TOINT/TOFLOAT/TOSTR/TOSTRF/CONVERT 均已支持 |
| 5. 字符串函数 | 23 | 0 | — | 23 | 全部 N/A |
| 6. 角色数据函数 | 19 | 0 | — | 19 | 角色系统不支持 Float 字段 |
| 7. MAP 数据集 | 18 | 0 | — | 18 | 纯 String-String 字典 |
| 8. DT (DataTable) | 21 | 2 | 2 ✅ | 19 | DT_CELL_GETF/DT_CELL_SETF 新增 |
| 9. SQL | 21 | 3 | 3 ✅ | 18 | 3 个 Float 读取函数新增 |
| 10. 位操作 | 4 | 0 | — | 4 | 仅整数 |
| 11. 通用/系统 | 40+ | 0 | — | 40+ | 全部 N/A |
| 12-16. 图形/HTML/XML/声音/热键 | 50+ | 0 | — | 50+ | 全部 N/A |
| 17. 指令级函数 | 100+ | 2 | 2 ✅ | 100+ | BAR/BARL Float 参数重载 |
| **总计** | **~350+** | **38** | **38 ✅** | **~312** | **全部完成** |

### 2.2 已确认 Float 支持的函数/指令

| 函数/指令 | 支持方式 | 来源 |
|----------|---------|------|
| SETVAR | 已有 case EraType.Float | 原始引擎 |
| EXISTMETH | 已有 case EraType.Float | 原始引擎 |
| VARSET / CVARSET | 已有 case EraType.Float | 原始引擎 |
| ARRAYSHIFT/REMOVE/SORT/COPY | 已有 Float 支持 | 原始引擎 |
| SAVEDATA/LOADDATA/SAVEGLOBAL/LOADGLOBAL | 序列化已支持 Float | B.3-4 |
| TIMES | 已修改支持变量第二参数 | B.3-10 |
| TOINT / TOFLOAT / TOSTR / TOSTRF | 类型转换已支持 | B.3-10 |
| RETURNF | Float 返回值 | B.3-10 |
| RANDF~LIMITF (12个) | Float 重载函数 | Batch 1 |
| SINF~ROUNDF (9个) | 新增三角/取整函数 | Batch 1 |
| SUMARRAY~INRANGEARRAY (16个) | 同名重载 Float 参数 | Batch 2 |
| GETVARF / GETMETHF / EVALF | 新增 Float 反射函数 | Batch 3 |
| DT_CELL_GETF / DT_CELL_SETF | 新增 Float DT 操作 | Batch 4 |
| SQL_READER_GET_FLOAT / SQL_EXECUTE_SCALAR_FLOAT / SQL_P_EXECUTE_SCALAR_FLOAT | 新增 Float SQL 操作 | Batch 5 |
| BAR / BARL | Float 参数自动转 long | Batch 6 |
| LOCALF / ARGF / RESULTF | 新增 Float 局部/参数/返回变量 | §4.8-4.9 |
| #FUNCTIONF / #LOCALFSIZE | 新增语法指令 | §4.10 |

### 2.3 不涉及 Float 的函数类别（N/A）

以下类别经审计确认**不需要** Float 支持：

- **字符串函数**（STRLENS/SUBSTRING/REPLACE 等）：操作字符串，不涉及数值
- **角色数据函数**（GETCHARA/CSVNAME/FINDCHARA 等）：角色系统本身不支持 Float 字段
- **MAP 数据集**（MAP_CREATE/MAP_GET 等）：内部 `Dictionary<string, string>`，用户应使用 TOSTRF()/TOFLOAT() 转换
- **位操作**（BITSET/BITGET/BITTOGGLE/BITINDEXOFFIRST）：仅整数有意义
- **UNCHECKED_* 运算**（UNCHECKED_ADD/SUB/MUL/NEG）：溢出控制，仅整数有意义
- **图形/HTML/XML/声音/热键**：不涉及数值计算
- **流程控制/输入/输出**：控制流和 I/O，条件表达式本身已支持 Float

---

## 3. 工具脚本

### 3.1 查询脚本 — `tools/b3_query.ps1`

```powershell
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1                  # 全量查询
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Mode B15        # 仅 B.3-15 指标
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Mode B16        # 仅 B.3-16 指标
powershell -ExecutionPolicy Bypass -File tools/b3_query.ps1 -Detail -Mode B15 # 详细行号
```

### 3.2 验证脚本 — `tools/b3_verify.ps1`

```powershell
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 15a -Mode B15
powershell -ExecutionPolicy Bypass -File tools/b3_verify.ps1 -Batch 16a -Mode B16
```

### 3.3 批量替换脚本 — `tools/b3_16_replace.py`

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

## 4. 浮点数功能补全（Batch 1-6）

> 以下任务在 B.3-0~16 类型系统重构中**有意延迟**，属于浮点数运行时功能的补全。
> 2026-05-04 终审：对 `BuiltInFunctionCode.cs`、`Creator.Method.cs`、`VariableEvaluator.cs`、`VariableData.cs`、`CharacterData.cs` 进行了全面盘点。
> **Batch 1-6 + LOCALF/ARGF/RESULTF/#FUNCTIONF 全部完成于 2026-05-04。**

### 4.0 战略决策：LOCAL/ARG/GLOBAL 的 Float 版本

**决策**：引擎新增 `LOCALF` / `ARGF` 作为 Float 版本的局部/参数变量，以及 `RESULTF` 作为 Float 返回值变量。

- `LOCAL` / `ARG` / `GLOBAL` 保持 Int 类型不变
- 新增 `LOCALF`（Float 一维数组局部变量）、`ARGF`（Float 一维数组参数变量）
- 新增 `RESULTF`（Float 标量返回值）
- 新增 `#FUNCTIONF` 语法支持用户自定义 Float 返回函数
- 新增 `#LOCALFSIZE` 指令控制 Float 局部数组大小（ARGF 由参数动态分配，无需指令）
- Float 变量通过 `#DIMF` 声明使用，或通过 `LOCALF`/`ARGF`/`RESULTF` 内置变量

**理由**：Float 变量使用频率低于 Int，但函数参数和返回值的 Float 支持是必要功能，不能仅靠 `#DIMF` 绕过。

### 4.1 内置数学函数 Float 重载 ✅ 已完成

**实施状态**：✅ **已完成（2026-05-04）** — 12 Float 重载 + 18 新函数全部实现并编译通过。

**Float 重载（12 个）**：RANDF, MINF, MAXF, ABSF, POWERF, SQRTF, CBRTF, LOGF, LOG10F, EXPF, SIGNF, LIMITF

**新增函数（18 个）**：

| Int 版 | Float 版 | 说明 |
|--------|---------|------|
| SIN | SINF | 正弦（弧度） |
| COS | COSF | 余弦（弧度） |
| TAN | TANF | 正切（弧度） |
| ASIN | ASINF | 反正弦（[-1,1]） |
| ACOS | ACOSF | 反余弦（[-1,1]） |
| ATAN | ATANF | 反正切 |
| FLOOR | FLOORF | 向下取整 |
| CEIL | CEILF | 向上取整 |
| ROUND | ROUNDF | 四舍五入 |

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs` — 新增 30 个方法类
- `Emuera/Runtime/Script/Statements/Function/Creator.cs` — 注册 30 个新函数名
- `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs` — 新增 `GetNextRandDouble()`

**优先级**：P1（高）
**状态**：✅ 完成

### 4.2 数组操作函数 Float 支持

**状态：✅ 已完成（2026-05-04）** — 所有数组函数已通过同名重载支持 Float 参数。

**现状（2026-05-04 源码盘点）**：

| 函数 | 当前状态 | Int | String | Float | 备注 |
|------|---------|-----|--------|-------|------|
| SUMARRAY/SUMCARRAY | **Int+Float** | ✅ | ❌ | ✅ | `GetReturnValue` 根据数组类型自动选择 Int/Float 返回 |
| MATCH/CMATCH | **Int+String+Float** | ✅ | ✅ | ✅ | 新增 Float 分支 |
| MAXARRAY/MAXCARRAY | **Int+Float** | ✅ | ❌ | ✅ | `RefAny1D` + Float 分支（返回值保持 Int） |
| MINARRAY/MINCARRAY | **Int+Float** | ✅ | ❌ | ✅ | 同上 |
| GROUPMATCH | **Int+String+Float** | ✅ | ✅ | ✅ | 新增 Float 分支 |
| NOSAMES | **Int+String+Float** | ✅ | ✅ | ✅ | 新增 Float 分支 |
| ALLSAMES | **Int+String+Float** | ✅ | ✅ | ✅ | 新增 Float 分支 |
| INRANGEARRAY/INRANGECARRAY | **Int+Float** | ✅ | ❌ | ✅ | `RefAny1D` + Float 分支（返回值保持 Int） |
| ARRAYMSORT | **Int+String+Float** | ✅ | ✅ | ✅ | 已有 `GetEraType()` 分支，无需修改 |

**实施详情**：
- `VariableEvaluator.cs` 新增方法：
  - `GetArraySumDouble` / `GetArraySumCharaDouble` — Float 数组求和
  - `GetMatch(FixedVariableTerm, double, ...)` / `GetMatchChara(FixedVariableTerm, double, ...)` — Float 匹配计数
  - `GetMaxArrayDouble` / `GetMaxArrayCharaDouble` — Float 数组最大/最小值
  - `GetInRangeArrayDouble` / `GetInRangeArrayCharaDouble` — Float 数组范围计数
- `Creator.Method.cs` 修改：
  - `SumArrayMethod`: `RefIntArray` → `RefAnyArray`，新增 `GetFloatValue` + `GetReturnValue`（自动根据数组类型返回 Int/Float）
  - `MatchMethod`: 新增 `EraType.Float` 分支
  - `MaxArrayMethod`: `RefInt1D` → `RefAny1D`，新增 Float 分支（返回值保持 Int）
  - `GroupMatchMethod` / `NosamesMethod` / `AllsamesMethod`: 新增 Float 分支
  - `InRangeArrayMethod`: `RefInt1D` → `RefAny1D`，新增 Float 分支（返回值保持 Int）
  - `ArrayMultiSortMethod`: 无需修改（已有 `GetEraType()` 分支处理 Float）
- **设计原则**：同名重载，不新增 F 变体函数。除 SUMARRAY 根据数组类型自动改变返回值类型外，其余函数返回值类型不变。

### 4.3 反射/动态调用函数 Float 支持 ✅ 已完成

**实施状态**：✅ **已完成（2026-05-04）** — 3 个新函数全部实现并编译通过。

> GETVAR、EVAL、GETMETH 等允许通过字符串名动态操作变量/表达式的"反射"函数。

**新增函数**：

| 新函数 | 对应现有 | 返回类型 | 用途 |
|--------|---------|---------|------|
| **GETVARF** | GETVAR | `EraType.Float` | 通过字符串名读取 Float 变量值 |
| **GETMETHF** | GETMETH | `EraType.Float` | 通过字符串名动态调用返回 Float 的函数 |
| **EVALF** | EVAL | `EraType.Float` | 动态求值字符串表达式，返回 Float（不截断） |

**实施详情**：
- `Creator.Method.cs` 新增 `GetVarFMethod` / `GetMethFMethod` / `EvalFMethod` 三个类
- 均设置 `ReturnType = EraType.Float`，重写 `GetFloatValue(exm)`
- `Creator.cs` 注册 "GETVARF" / "GETMETHF" / "EVALF"
- `Lang.cs` 新增 `IsNotFloat` 错误消息
- `emuera-zhs.xml` / `emuera-eng.xml` 同步新增 `Error.IsNotFloat` 翻译

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs` — 新增 3 个方法类
- `Emuera/Runtime/Script/Statements/Function/Creator.cs` — 注册 3 个新函数名
- `Emuera/Runtime/Utils/EvilMask/Lang.cs` — 新增 `IsNotFloat`
- `Emuera/Properties/lang/emuera-zhs.xml` — 新增翻译
- `Emuera/Properties/lang/emuera-eng.xml` — 新增翻译

**优先级**：P2（中）
**状态**：✅ 完成

### 4.4 DT (DataTable) Float 支持 ✅ 已完成

**实施状态**：✅ **已完成（2026-05-04）** — 2 个新函数全部实现并编译通过。

**新增函数**：

| 新函数 | 对应现有 | 返回类型 | 用途 |
|--------|---------|---------|------|
| **DT_CELL_GETF** | DT_CELL_GET | `EraType.Float` | 读取 DataTable 单元格的 Float 值 |
| **DT_CELL_SETF** | DT_CELL_SET | `EraType.Integer` | 向 DataTable 单元格写入 Float 值（返回状态码） |

**实施详情**：
- `Creator.Method.cs` 新增 `DataTableCellGetFloatMethod` / `DataTableCellSetFloatMethod` 两个类
- `DT_CELL_GETF`: `ReturnType = EraType.Float`，重写 `GetFloatValue`，使用 `Convert.ToDouble(v)` 读取
- `DT_CELL_SETF`: `ReturnType = EraType.Integer`，重写 `GetIntValue`，使用 `v.GetFloatValue(exm)` 写入
- `Creator.cs` 注册 "DT_CELL_GETF" / "DT_CELL_SETF"

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs` — 新增 2 个方法类
- `Emuera/Runtime/Script/Statements/Function/Creator.cs` — 注册 2 个新函数名

**优先级**：P2（中）
**状态**：✅ 完成

### 4.5 SQL Float 支持 ✅ 已完成

**实施状态**：✅ **已完成（2026-05-04）** — 3 个新函数 + 2 个 SqlManager 方法全部实现并编译通过。

**新增函数**：

| 新函数 | 对应现有 | 返回类型 | 用途 |
|--------|---------|---------|------|
| **SQL_READER_GET_FLOAT** | SQL_READER_GET_LONG | `EraType.Float` | 从 Reader 读取 Float 列 |
| **SQL_EXECUTE_SCALAR_FLOAT** | SQL_EXECUTE_SCALAR_LONG | `EraType.Float` | 执行标量查询返回 Float |
| **SQL_P_EXECUTE_SCALAR_FLOAT** | SQL_P_EXECUTE_SCALAR_LONG | `EraType.Float` | 参数化标量查询返回 Float |

**实施详情**：
- `SqlManager.cs` 新增 `ReaderGetFloat(long, int)` → `double` 和 `ExecuteScalarFloat(string, string, string[])` → `double`
- `Creator.Method.cs` 新增 `SqlReaderGetFloatMethod` / `SqlExecuteScalarFloatMethod` / `SqlExecuteScalarFloatParamMethod` 三个类
- 均设置 `ReturnType = EraType.Float`，重写 `GetFloatValue(exm)`
- `Creator.cs` 注册 "SQL_READER_GET_FLOAT" / "SQL_EXECUTE_SCALAR_FLOAT" / "SQL_P_EXECUTE_SCALAR_FLOAT"

**涉及文件**：
- `Emuera/Runtime/Utils/尊尼获加/SqlManager.cs` — 新增 2 个方法
- `Emuera/Runtime/Script/Statements/Function/Creator.Method.cs` — 新增 3 个方法类
- `Emuera/Runtime/Script/Statements/Function/Creator.cs` — 注册 3 个新函数名

**优先级**：P2（中）
**状态**：✅ 完成

### 4.6 BAR/BARL Float 参数重载 ✅ 已完成

**实施状态**：✅ **已完成（2026-05-04）** — 内部计算支持 Float 参数，无需新增 API。

**说明**：BAR/BARL 指令的 3 个参数（var, max, length）现在接受 Float 类型表达式。当参数为 Float 类型时，自动转换为 `(long)` 后调用 `CreateBar`。无需新增 BARF/BARLF 指令。

**实施详情**：
- `Instraction.Child.cs` 中 `BAR_Instruction.DoInstruction` 修改：
  - 新增私有静态方法 `GetBarValue(AExpression, ExpressionMediator)` → `long`
  - 检查 `term.GetEraType() == EraType.Float`，若是则调用 `GetFloatValue` 并转为 `(long)`
  - 否则调用 `GetIntValue`（保持 Int 兼容）

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Instraction.Child.cs` — 修改 `BAR_Instruction` 类

**优先级**：P2（中）
**状态**：✅ 完成

### 4.7 SAVEDATA/CHARADATA Float 支持 ✅ 已完成

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

### 4.8 函数参数 Float 支持（ARGF）✅ 已完成

**实施状态**：✅ **已完成（2026-05-04）** — LOCALF/ARGF 数组 + Float 参数传递全部实现并编译通过。

**新增变量**：

| 变量 | 类型 | 维度 | 说明 |
|------|------|------|------|
| **LOCALF** | Float | 1D Array | Float 局部变量数组（对应 LOCAL） |
| **ARGF** | Float | 1D Array | Float 函数参数数组（对应 ARG） |

**新增指令**：

| 指令 | 说明 |
|------|------|
| `#LOCALFSIZE n` | 设置 LOCALF 数组大小（对应 #LOCALSIZE） |

注：ARGF 无需专用指令，由函数参数声明动态分配大小。

**实施详情**：

1. **VariableCode.cs** — 新增枚举值：
   - `LOCALF = 0x00 | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__`
   - `ARGF = 0x01 | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__`

2. **VariableDescriptor.cs** — 注册新变量描述符：
   - `Register("LOCALF", VariableCode.LOCALF, VariableKind.Float, VariableDimension.Array1D, ...)`
   - `Register("ARGF", VariableCode.ARGF, VariableKind.Float, VariableDimension.Array1D, ...)`

3. **VariableToken.cs** — 新增 `LocalFloat1DVariableToken`：
   - 从 `ExecutionContext.LocalFloats` / `ExecutionContext.ArgFloats` 读写
   - 实现 `GetFloatValue` / `SetValue(double)` / `SetValueAll(double)` / `SetDefault` / `resize`

4. **VariableData.cs** — 注册 LOCALF/ARGF 工厂：
   - `localvarTokenDic.Add("LOCALF", new VariableLocal(VariableCode.LOCALF, size, CreateLocalFloat))`
   - `localvarTokenDic.Add("ARGF", new VariableLocal(VariableCode.ARGF, size, CreateLocalFloat))`
   - 新增 `CreateLocalFloat` 工厂方法

5. **ExecutionContext.cs** — 正确初始化 Float 数组：
   - `LocalFloats` / `ArgFloats` 根据函数的 `LocalFloatLength` / `ArgFloatLength` 分配
   - 默认大小与 LOCAL/ARG 一致（1000）

6. **FunctionLabelLine (LogicalLine.cs)** — 新增属性：
   - `LocalFloatLength`

7. **LogicalLineParser.cs** — 新增指令解析：
   - `#LOCALFSIZE` 解析（复用 LOCALSIZE 的验证逻辑）

8. **Process.CalledFunction.cs** — Float 参数传递：
   - `UserDefinedFunctionArgument` 新增 `TransporterFloat: double[]`
   - `SetTransporter` 新增 `EraType.Float` 分支
   - `ConvertArg` 新增 Int→Float 自动提升、Float→Int 拒绝

9. **Process.State.cs** — IntoFunction 参数复制：
   - 新增 `EraType.Float` 分支，调用 `SetValue(double, exm)`

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Variable/VariableCode.cs`
- `Emuera/Runtime/Script/VariableDescriptor.cs`
- `Emuera/Runtime/Script/Statements/Variable/VariableToken.cs`
- `Emuera/Runtime/Script/Statements/Variable/VariableData.cs`
- `Emuera/Runtime/Script/ExecutionContext.cs`
- `Emuera/Runtime/Script/Statements/LogicalLine.cs`
- `Emuera/Runtime/Script/Parser/LogicalLineParser.cs`
- `Emuera/Runtime/Script/Process.CalledFunction.cs`
- `Emuera/Runtime/Script/Process.State.cs`

**优先级**：P1（高）
**状态**：✅ 完成

### 4.9 函数返回值 Float 支持（RESULTF）✅ 已完成

**实施状态**：✅ **已完成（2026-05-04）** — RESULTF 全局变量 + Float 返回值全部实现并编译通过。

**新增变量**：

| 变量 | 类型 | 维度 | 说明 |
|------|------|------|------|
| **RESULTF** | Float | Scalar | Float 全局返回值（对应 RESULT） |

**实施详情**：

1. **VariableCode.cs** — 新增枚举值：
   - `RESULTF = 0x01 | __EXTENDED__`

2. **VariableDescriptor.cs** — 注册新变量描述符：
   - `Register("RESULTF", VariableCode.RESULTF, VariableKind.Float, VariableDimension.Scalar, VariableAttribute.Extended)`

3. **VariableToken.cs** — 新增 `FloatScalarVariableToken`：
   - 从 `VariableData.DataFloat[VarCodeInt]` 读写
   - 实现 `GetFloatValue` / `SetValue(double)` / `SetValueAll(double)`

4. **VariableData.cs** — 注册 RESULTF Token：
   - `varTokenDic.Add("RESULTF", new FloatScalarVariableToken(VariableCode.RESULTF, this))`
   - `SetDefaultValue` 新增 `dataFloat` 清零

5. **VariableEvaluator.cs** — 新增 `RESULTF` 属性：
   - `get` → `varData.DataFloat[(int)(VariableCode.RESULTF & VariableCode.__LOWERCASE__)]`
   - `set` → 同上

**涉及文件**：
- `Emuera/Runtime/Script/Statements/Variable/VariableCode.cs`
- `Emuera/Runtime/Script/VariableDescriptor.cs`
- `Emuera/Runtime/Script/Statements/Variable/VariableToken.cs`
- `Emuera/Runtime/Script/Statements/Variable/VariableData.cs`
- `Emuera/Runtime/Script/Statements/Variable/VariableEvaluator.cs`

**优先级**：P1（高）
**状态**：✅ 完成

### 4.10 #FUNCTIONF 用户自定义 Float 返回函数 ✅ 已完成

**实施状态**：✅ **已完成（2026-05-04）** — #FUNCTIONF 解析 + Float 返回值全部实现并编译通过。

**新增语法**：

| 语法 | 返回类型 | 说明 |
|------|---------|------|
| `#FUNCTIONF` | `EraType.Float` | 用户自定义 Float 返回函数（对应 #FUNCTION / #FUNCTIONS） |

**实施详情**：

1. **LogicalLineParser.cs** — 新增 `#FUNCTIONF` 解析：
   - case 分支新增 `"FUNCTIONF"` 匹配
   - `MethodType = EraType.Float` 赋值
   - 重复声明警告（Float→FUNCTION / Int→FUNCTIONF 等）

2. **UserDefinedMethodTerm.cs** — 适配 Float 返回类型：
   - `SuperUserDefinedMethodTerm` 构造函数：`EraType.Float` 三路判断
   - `GetValue` 默认值：`new SingleFloatTerm(0.0)`
   - `GetFloatValue` 新增：解包 `SingleFloatTerm`
   - `UserDefinedRefMethodTerm` / `UserDefinedRefMethodNoArgTerm` 同步适配

3. **Instraction.Child.cs** — `RETURNF_Instruction` 适配：
   - `SetJumpTo` 新增 Float 类型检查（Float→String 拒绝，Int→Float 允许）

**涉及文件**：
- `Emuera/Runtime/Script/Parser/LogicalLineParser.cs`
- `Emuera/Runtime/Script/Statements/Function/UserDefinedMethodTerm.cs`
- `Emuera/Runtime/Script/Statements/Instraction.Child.cs`

**优先级**：P2（中）
**状态**：✅ 完成

### 4.11 任务依赖关系

```
B.3-0~16 类型系统重构 ✅（已完成，前置条件满足）
  │
  ├── 3.1 数学函数 Float 重载 ✅（Batch 1，已完成）
  ├── 3.2 数组操作函数 Float 支持 ✅（Batch 2，已完成）
  ├── 3.3 反射函数 Float 支持 ✅（Batch 3，已完成）
  │     ├── GETVARF / GETMETHF / EVALF 新增 ✅
  │     ├── SETVAR Float ✅（已有）
  │     └── EXISTMETH Float ✅（已有）
  ├── 3.4 DT Float 支持 ✅（Batch 4，已完成）
  ├── 3.5 SQL Float 支持 ✅（Batch 5，已完成）
  ├── 3.6 BAR/BARL Float 参数重载 ✅（Batch 6，已完成）
  ├── 3.7 SAVEDATA/CHARADATA Float ✅（已有）
  ├── 3.8 ARGF Float 支持 ✅（已完成）──→ 3.10 #FUNCTIONF
  └── 3.9 RESULTF ✅（已完成）──────→ 3.10 #FUNCTIONF ✅（已完成）
```

---

## 5. m-emuera 迁移上下文

> 本节记录 B.3 各子任务在 emuera-lazyloading 完成后，迁移到 m-emuera 时需要的注意事项。

### 5.1 迁移状态总表

| 子任务 | m-emuera 状态 | 注意事项 |
|--------|--------------|----------|
| B.3-0~14 | ⬜ 待整体同步 | m-emuera 的 AExpression 仍使用 `Type` 而非 `EraType` |
| B.3-15a | ⬜ 待迁移 | VariableIdentifier Descriptor 查询 |
| B.3-15b | ⬜ 待迁移 | VariableToken IsSavedata 位标志→Descriptor |
| B.3-15c | ⬜ 待迁移 | VariableData GetExtSaveList + userDefinedSaveVarList 6→9 |
| B.3-15d | ⬜ 待迁移 | CharacterData 位标志 switch→Descriptor |
| B.3-15e | ⬜ 待迁移 | VariableDescriptor.FromCode() 注册表优先 |
| B.3-16 | ⬜ 待迁移 | typeof→EraType 批量替换 |

### 5.2 推荐迁移顺序

1. 先将 B.3-0~14 整体同步到 m-emuera（AExpression Type→EraType、OperatorMethod typeof→EraType、Creator.Method GetOperandType→EraType 等）
2. 同步完整的 VariableDescriptor.cs（含所有 Register 条目 + GetDescriptorByCode）
3. 同步 VariableIdentifier.cs（B.3-15a 修改）
4. 同步 B.3-15b~15e（VariableToken/CharacterData/VariableData）
5. 同步 B.3-16（typeof→EraType 批量替换）
6. 编译验证 + 运行时验证

### 5.3 关键差异点

1. **命名空间**：emuera-lazyloading 使用 `MinorShift.Emuera.Runtime.Script`，m-emuera 使用相同命名空间但项目结构不同（`src/MEmuera.Core/` 子目录）
2. **VariableDescriptorTable.Register 条目**：m-emuera 误创建版本只注册了部分变量（到 JUEL 为止），需从 lazyloading 同步完整版
3. **Config.StrComper 依赖**：VariableDescriptorTable 使用 `new Config.Config.StrComper)` 作为字典比较器，m-emuera 的 Config 命名空间可能不同
4. **extSaveListDic 键变更**：从 `Dictionary<VariableCode, List<VariableCode>>` 改为 `Dictionary<(VariableKind, VariableDimension), List<VariableCode>>`，旧 API 重载保留为向后兼容层

---

## 6. 来源引用索引

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
| 测试日志（基础） | `d:\eratw-chs\20260504-043622.log` |
| 测试日志（全量） | `d:\eratw-chs\20260504-230148.log` |
