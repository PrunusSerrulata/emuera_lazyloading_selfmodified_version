# B.3 Float 支持全面审计清单

> ⚠️ **本文件已归档，可安全删除。**
> 全部审计内容已归纳合并至 `B3_TRACKER.md` §2（全量审计摘要）。
> 保留本文件仅作历史参考，确认无需后请手动删除。

> 创建日期：2026-05-04
> 目的：逐文件、逐函数盘点所有内置函数/指令的 Float 支持状态，找出缺失。

---

## 审计范围

| 文件 | 内容 | 函数数 |
|------|------|--------|
| `Creator.cs` | 表达式级内置函数注册 | ~200+ |
| `Creator.Method.cs` | 表达式级内置函数实现 | ~200+ |
| `FunctionIdentifier.cs` | 指令级内置函数注册 | ~100+ |
| `Instraction.Child.cs` | 指令级内置函数实现 | ~100+ |

---

## 分类体系

| 标记 | 含义 |
|------|------|
| ✅ | Float 已支持 |
| ❌ | Float 缺失，需新增 |
| ⚠️ | 部分支持/有截断 |
| N/A | 不涉及数值类型（纯字符串/图形/控制流等） |

---

## 1. 数学函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| RAND | Int | ❌ | 需 RANDF |
| MIN | Int | ❌ | 需 MINF |
| MAX | Int | ❌ | 需 MAXF |
| ABS | Int | ❌ | 需 ABSF |
| POWER | Int | ❌ | 需 POWERF |
| SQRT | Int | ❌ | 需 SQRTF |
| CBRT | Int | ❌ | 需 CBRTF |
| LOG | Int | ❌ | 需 LOGF |
| LOG10 | Int | ❌ | 需 LOG10F |
| EXPONENT | Int | ❌ | 需 EXPF |
| SIGN | Int | ❌ | 需 SIGNF |
| LIMIT | Int | ❌ | 需 LIMITF |
| UNCHECKED_ADD | Int | N/A | 溢出控制，仅整数有意义 |
| UNCHECKED_SUB | Int | N/A | 同上 |
| UNCHECKED_MUL | Int | N/A | 同上 |
| UNCHECKED_NEG | Int | N/A | 同上 |

**缺失数：12 个 Float 重载**

---

## 2. 数组操作函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| SUMARRAY | Int | ❌ | 需 SUMARRAYF |
| SUMCARRAY | Int | ❌ | 需 SUMCARRAYF |
| MATCH | Int | ❌ | 需 MATCHF |
| CMATCH | Int | ❌ | 需 CMATCHF |
| GROUPMATCH | Int | ❌ | 需 GROUPMATCHF |
| NOSAMES | Int | ❌ | 需 NOSAMESF |
| ALLSAMES | Int | ❌ | 需 ALLSAMESF |
| MAXARRAY | Int | ❌ | 需 MAXARRAYF |
| MAXCARRAY | Int | ❌ | 需 MAXCARRAYF |
| MINARRAY | Int | ❌ | 需 MINARRAYF |
| MINCARRAY | Int | ❌ | 需 MINCARRAYF |
| INRANGE | Int | ❌ | 需 INRANGEF |
| INRANGEARRAY | Int | ❌ | 需 INRANGEARRAYF |
| INRANGECARRAY | Int | ❌ | 需 INRANGECARRAYF |
| FINDELEMENT | Int | ❌ | 需 FINDELEMENTF |
| FINDLASTELEMENT | Int | ❌ | 需 FINDLASTELEMENTF |
| GETBIT | Int | N/A | 位操作，仅整数 |
| GETNUM | Int | N/A | 返回数量，总是整数 |
| GETNUMB | Int | N/A | 同上 |
| GETPALAMLV | Int | N/A | 等级计算，总是整数 |
| GETEXPLV | Int | N/A | 同上 |
| ARRAYMSORT | Int | N/A | 排序，不返回值 |

**缺失数：16 个 Float 重载**

---

## 3. 反射/动态调用函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| GETVAR | Int | ❌ | 需 GETVARF |
| GETVARS | String | N/A | String 版本 |
| SETVAR | Int | ✅ | 已有 case EraType.Float |
| VARSETEX | Int | ❌ | 需确认 |
| GETMETH | Int | ❌ | 需 GETMETHF |
| GETMETHS | String | N/A | String 版本 |
| EXISTMETH | Int | ✅ | 已有 case EraType.Float |
| EVAL | Int | ⚠️ | Float 被 (long) 截断，需 EVALF |
| EVALS | String | ⚠️ | Float 被 .ToString()，需 EVALF |
| **GETVARF** | **Float** | **✅** | **2026-05-04 新增** |
| **GETMETHF** | **Float** | **✅** | **2026-05-04 新增** |
| **EVALF** | **Float** | **✅** | **2026-05-04 新增** |

**缺失数：0（3 个新函数已实现）**

---

## 4. 类型转换函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| TOINT | Int | ✅ | 接受 Float 参数 |
| TOFLOAT | Float | ✅ | 接受 Int/String 参数 |
| TOSTR | String | ✅ | 接受 Int 参数 |
| TOSTRF | String | ✅ | 接受 Float 参数 |
| CONVERT | Int | N/A | 汉字数字转换 |

---

## 5. 字符串函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| STRLENS | Int | N/A | 字符串长度 |
| STRLENSU | Int | N/A | Unicode 长度 |
| SUBSTRING | String | N/A | |
| SUBSTRINGU | String | N/A | |
| STRFIND | Int | N/A | |
| STRFINDU | Int | N/A | |
| STRCOUNT | Int | N/A | |
| TOUPPER | String | N/A | |
| TOLOWER | String | N/A | |
| TOHALF | String | N/A | |
| TOFULL | String | N/A | |
| LINEISEMPTY | Int | N/A | |
| REPLACE | String | N/A | |
| UNICODE | Int | N/A | |
| UNICODEBYTE | Int | N/A | |
| ISNUMERIC | Int | N/A | |
| ESCAPE | String | N/A | |
| ENCODETOUNI | String | N/A | |
| CHARATU | String | N/A | |
| GETLINESTR | String | N/A | |
| STRFORM | String | N/A | |
| STRJOIN | String | N/A | |
| REGEXPMATCH | Int | N/A | |

**缺失数：0（全部 N/A）**

---

## 6. 角色数据函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| GETCHARA | Int | N/A | 返回角色 ID |
| GETSPCHARA | Int | N/A | |
| CSVNAME | String | N/A | |
| CSVCALLNAME | String | N/A | |
| CSVNICKNAME | String | N/A | |
| CSVMASTERNAME | String | N/A | |
| CSVCSTR | String | N/A | |
| CSVBASE | Int | N/A | 角色基础数据，总是整数 |
| CSVABL | Int | N/A | |
| CSVMARK | Int | N/A | |
| CSVEXP | Int | N/A | |
| CSVRELATION | Int | N/A | |
| CSVTALENT | Int | N/A | |
| CSVCFLAG | Int | N/A | |
| CSVEQUIP | Int | N/A | |
| CSVJUEL | Int | N/A | |
| FINDCHARA | Int | N/A | |
| FINDLASTCHARA | Int | N/A | |
| EXISTCSV | Int | N/A | |

**缺失数：0（全部 N/A — 角色数据系统本身不支持 Float 字段）**

---

## 7. MAP 数据集函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| MAP_CREATE | Int | N/A | 管理操作 |
| MAP_EXIST | Int | N/A | |
| MAP_RELEASE | Int | N/A | |
| MAP_GET | String | N/A | 内部是 `Dictionary<string, string>` |
| MAP_CLEAR | Int | N/A | |
| MAP_SIZE | Int | N/A | |
| MAP_HAS | Int | N/A | |
| MAP_SET | Int | N/A | 值参数是 String |
| MAP_REMOVE | Int | N/A | |
| MAP_GETKEYS | String | N/A | |
| MAP_TOXML | String | N/A | |
| MAP_FROMXML | Int | N/A | |
| MAP_VALUES | String | N/A | |
| MAP_MERGE | Int | N/A | |
| MAP_REMOVEIF | Int | N/A | |
| MAP_FINDKEY | String | N/A | |
| MAP_TOSTRING | String | N/A | |
| MAP_FROMSTRING | Int | N/A | |

**缺失数：0（MAP 是纯 String-String 字典，不涉及数值类型）**

> **设计决策**：MAP 内部是 `Dictionary<string, string>`，如需存储 Float 值，用户应使用 TOSTRF()/TOFLOAT() 转换。不需要引擎层面新增 Float Map。

---

## 8. DT (DataTable) 数据集函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| DT_CREATE | Int | N/A | 管理操作 |
| DT_EXIST | Int | N/A | |
| DT_RELEASE | Int | N/A | |
| DT_NOCASE | Int | N/A | |
| DT_CLEAR | Int | N/A | |
| DT_COLUMN_ADD | Int | N/A | |
| DT_COLUMN_NAMES | String | N/A | |
| DT_COLUMN_EXIST | Int | N/A | |
| DT_COLUMN_REMOVE | Int | N/A | |
| DT_COLUMN_LENGTH | Int | N/A | |
| DT_ROW_ADD | Int | N/A | |
| DT_ROW_SET | Int | N/A | |
| DT_ROW_REMOVE | Int | N/A | |
| DT_ROW_LENGTH | Int | N/A | |
| DT_CELL_GET | Int | ⚠️ | 内部 `Convert.ToInt64(v)`，Float 列会抛异常 |
| DT_CELL_ISNULL | Int | N/A | |
| DT_CELL_GETS | String | N/A | |
| DT_CELL_SET | Int | ⚠️ | 检查 `v.GetEraType() != EraType.Integer`，拒绝 Float |
| **DT_CELL_GETF** | **Float** | **✅** | **2026-05-04 新增** |
| **DT_CELL_SETF** | **Int** | **✅** | **2026-05-04 新增** |
| DT_SELECT | String | N/A | |
| DT_TOXML | String | N/A | |
| DT_FROMXML | Int | N/A | |

**缺失数：0（2 个新函数已实现）**

> **设计决策**：DT 底层是 `System.Data.DataTable`，列类型在创建时确定。如需 Float 列，需新增 DT_COLUMN_ADDF 或扩展现有 DT_COLUMN_ADD 支持 Float 类型参数，以及对应的 DT_CELL_GETF / DT_CELL_SETF。

---

## 9. SQL 函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| SQL_CONNECT | Int | N/A | |
| SQL_DISCONNECT | Int | N/A | |
| SQL_EXECUTE_NONQUERY | Int | N/A | |
| SQL_EXECUTE_READER | Int | N/A | |
| SQL_READER_READ | Int | N/A | |
| SQL_READER_GET_LONG | Int | ❌ | 需 SQL_READER_GET_FLOAT |
| SQL_READER_GET_STRING | String | N/A | |
| SQL_READER_ISNULL | Int | N/A | |
| SQL_READER_CLOSE | Int | N/A | |
| SQL_EXECUTE_SCALAR_LONG | Int | ❌ | 需 SQL_EXECUTE_SCALAR_FLOAT |
| SQL_EXECUTE_SCALAR_STRING | String | N/A | |
| SQL_IMPORT_MAP_XML | Int | N/A | |
| SQL_IMPORT_DT_XML | Int | N/A | |
| SQL_EXPORT_MAP_XML | Int | N/A | |
| SQL_EXPORT_DT_XML | Int | N/A | |
| SQL_IMPORT_XML_CUSTOM | Int | N/A | |
| SQL_ESCAPE | String | N/A | |
| SQL_P_EXECUTE_NONQUERY | Int | N/A | |
| SQL_P_EXECUTE_READER | Int | N/A | |
| SQL_P_EXECUTE_SCALAR_LONG | Int | ❌ | 需 SQL_P_EXECUTE_SCALAR_FLOAT |
| SQL_P_EXECUTE_SCALAR_STRING | String | N/A | |
| **SQL_READER_GET_FLOAT** | **Float** | **✅** | **2026-05-04 新增** |
| **SQL_EXECUTE_SCALAR_FLOAT** | **Float** | **✅** | **2026-05-04 新增** |
| **SQL_P_EXECUTE_SCALAR_FLOAT** | **Float** | **✅** | **2026-05-04 新增** |

**缺失数：0（3 个新函数已实现）**

---

## 10. 位操作函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| BITSET | Int | N/A | 位操作，仅整数 |
| BITGET | Int | N/A | |
| BITTOGGLE | Int | N/A | |
| BITINDEXOFFIRST | Int | N/A | |

---

## 11. 通用/系统函数 (Creator.Method.cs)

| 函数 | 返回类型 | Float | 备注 |
|------|---------|-------|------|
| VARSIZE | Int | N/A | |
| CHKFONT | Int | N/A | |
| CHKDATA | Int | N/A | |
| ISSKIP | Int | N/A | |
| MOUSESKIP | Int | N/A | |
| MESSKIP | Int | N/A | |
| GETCOLOR | Int | N/A | |
| GETDEFCOLOR | Int | N/A | |
| GETFOCUSCOLOR | Int | N/A | |
| GETBGCOLOR | Int | N/A | |
| GETDEFBGCOLOR | Int | N/A | |
| GETSTYLE | Int | N/A | |
| GETFONT | String | N/A | |
| BARSTR | String | N/A | |
| CURRENTALIGN | Int | N/A | |
| CURRENTREDRAW | Int | N/A | |
| COLOR_FROMNAME | Int | N/A | |
| COLOR_FROMRGB | Int | N/A | |
| CHKCHARADATA | String | N/A | |
| FIND_CHARADATA | String | N/A | |
| MONEYSTR | String | N/A | |
| PRINTCPERLINE | Int | N/A | |
| PRINTCLENGTH | Int | N/A | |
| SAVENOS | Int | N/A | |
| GETTIME | Int | N/A | |
| GETTIMES | String | N/A | |
| GETMILLISECOND | Int | N/A | |
| GETSECOND | Int | N/A | |
| GETCONFIG | Int | N/A | |
| GETCONFIGS | String | N/A | |
| EXISTFILE | Int | N/A | |
| EXISTVAR | Int | N/A | |
| ISDEFINED | Int | N/A | |
| ENUMFUNC* | String | N/A | |
| ENUMVAR* | String | N/A | |
| ENUMMACRO* | String | N/A | |
| ENUMFILES | String | N/A | |
| EXISTSOUND | Int | N/A | |
| EXISTFUNCTION | Int | N/A | |
| GETMEMORYUSAGE | Int | N/A | |
| CLEARMEMORY | Int | N/A | |
| GETTEXTBOX | String | N/A | |
| SETTEXTBOX | Int | N/A | |
| ERDNAME | String | N/A | |
| GETDISPLAYLINE | Int | N/A | |
| GETDOINGFUNCTION | String | N/A | |
| FLOWINPUT | Int | N/A | |
| FLOWINPUTS | String | N/A | |
| GET_TEXT_DRAWING_MODE | Int | N/A | |
| GET_SKIA_QUALITY | Int | N/A | |

---

## 12. 图形/精灵函数 (Creator.Method.cs)

全部 N/A — 图形操作不涉及 Float 数值计算。

---

## 13. HTML 函数 (Creator.Method.cs)

全部 N/A — HTML 处理不涉及 Float 数值计算。

---

## 14. XML 函数 (Creator.Method.cs)

全部 N/A — XML 处理不涉及 Float 数值计算。

---

## 15. 声音/BGM 函数 (Creator.Method.cs)

全部 N/A — 声音控制不涉及 Float 数值计算。

---

## 16. 热键函数 (Creator.Method.cs)

全部 N/A — 热键状态不涉及 Float 数值计算。

---

## 17. 指令级函数 (FunctionIdentifier.cs + Instraction.Child.cs)

### 17.1 PRINT 系指令

全部 N/A — 输出显示，不涉及 Float 计算。

### 17.2 输入系指令 (INPUT, TINPUT, ONEINPUT 等)

全部 N/A — 用户输入，结果存入 Int/String 变量。

### 17.3 流程控制指令 (IF, ELSEIF, SELECTCASE, REPEAT, FOR, WHILE 等)

全部 N/A — 控制流，条件表达式本身已支持 Float。

### 17.4 变量操作指令

| 指令 | Float | 备注 |
|------|-------|------|
| VARSET | ✅ | 已有 case EraType.Float |
| CVARSET | ✅ | 已有 case EraType.Float |
| SPLIT | N/A | 字符串分割 |
| SWAP | N/A | 变量交换（通过 VariableTerm.SetValue 已支持） |
| SWAPCHARA | N/A | 角色交换 |
| COPYCHARA | N/A | 角色复制 |
| ADDCOPYCHARA | N/A | 角色复制添加 |

### 17.5 数组操作指令

| 指令 | Float | 备注 |
|------|-------|------|
| ARRAYSHIFT | ✅ | 已有 Float 支持 |
| ARRAYREMOVE | ✅ | 已有 Float 支持 |
| ARRAYSORT | ✅ | 已有 Float 支持 |
| ARRAYCOPY | ✅ | 已有 Float 支持 |

### 17.6 数据存取指令

| 指令 | Float | 备注 |
|------|-------|------|
| SAVEDATA | ✅ | 序列化已支持 Float |
| LOADDATA | ✅ | 反序列化已支持 Float |
| DELDATA | N/A | 删除操作 |
| SAVEGLOBAL | ✅ | 序列化已支持 Float |
| LOADGLOBAL | ✅ | 反序列化已支持 Float |
| RESETDATA | N/A | 重置操作 |
| RESETGLOBAL | N/A | 重置操作 |

### 17.7 计算指令

| 指令 | Float | 备注 |
|------|-------|------|
| TIMES | ✅ | 已修改支持变量第二参数 |
| BAR | ✅ | Float 参数自动转 long 用于内部计算（2026-05-04） |
| BARL | ✅ | 同上 |

### 17.8 角色操作指令

| 指令 | Float | 备注 |
|------|-------|------|
| ADDCHARA | N/A | |
| ADDSPCHARA | N/A | |
| ADDDEFCHARA | N/A | |
| ADDVOIDCHARA | N/A | |
| DELCHARA | N/A | |
| DELALLCHARA | N/A | |
| PICKUPCHARA | N/A | |
| SORTCHARA | N/A | 排序键是 Int 变量 |
| UPCHECK | N/A | |
| CUPCHECK | N/A | |

### 17.9 位操作指令

| 指令 | Float | 备注 |
|------|-------|------|
| SETBIT | N/A | 位操作，仅整数 |
| CLEARBIT | N/A | |
| INVERTBIT | N/A | |

### 17.10 其他指令

| 指令 | Float | 备注 |
|------|-------|------|
| RANDOMIZE | N/A | |
| DUMPRAND | N/A | |
| INITRAND | N/A | |
| SETCOLOR | N/A | |
| RESETCOLOR | N/A | |
| SETBGCOLOR | N/A | |
| RESETBGCOLOR | N/A | |
| FONTBOLD | N/A | |
| FONTITALIC | N/A | |
| FONTREGULAR | N/A | |
| FONTSTYLE | N/A | |
| ALIGNMENT | N/A | |
| SETFONT | N/A | |
| REDRAW | N/A | |
| SKIPDISP | N/A | |
| SKIPLOG | N/A | |
| NOSKIP | N/A | |
| FORCEKANA | N/A | |
| RESET_STAIN | N/A | |
| PUTFORM | N/A | |
| QUIT | N/A | |
| BEGIN | N/A | |
| SAVEGAME | N/A | |
| LOADGAME | N/A | |
| CALL | N/A | |
| JUMP | N/A | |
| TRYCALL | N/A | |
| TRYJUMP | N/A | |
| CALLFORM | N/A | |
| JUMPFORM | N/A | |
| RETURN | N/A | |
| RETURNFORM | N/A | |
| RETURNF | N/A | 返回 Float 值（已实现） |
| CALLTRAIN | N/A | |
| DOTRAIN | N/A | |
| STOPCALLTRAIN | N/A | |
| DATA | N/A | |
| DATAFORM | N/A | |
| DATALIST | N/A | |
| STRDATA | N/A | |
| STRLEN | N/A | |
| STRLENFORM | N/A | |
| STRLENU | N/A | |
| STRLENFORMU | N/A | |
| CUSTOMDRAWLINE | N/A | |
| DRAWLINEFORM | N/A | |
| CLEARTEXTBOX | N/A | |
| DRAWLINE | N/A | |
| SETBGIMAGE | N/A | |
| CLEARBGIMAGE | N/A | |
| REMOVEBGIMAGE | N/A | |
| MOVETEXTBOX | N/A | |
| RESUMETEXTBOX | N/A | |
| OUTPUTLOG | N/A | |
| WAIT | N/A | |
| WAITANYKEY | N/A | |
| FORCEWAIT | N/A | |
| TWAIT | N/A | |
| CLEARLINE | N/A | |
| REUSELASTLINE | N/A | |
| PRINTBUTTON | N/A | |
| PRINTPLAIN | N/A | |
| PRINT_ABL | N/A | |
| PRINT_TALENT | N/A | |
| PRINT_MARK | N/A | |
| PRINT_EXP | N/A | |
| PRINT_PALAM | N/A | |
| PRINT_ITEM | N/A | |
| PRINT_SHOPITEM | N/A | |

---

## 汇总

### 需新增 Float 重载/函数

| 类别 | 缺失数 | 优先级 |
|------|--------|--------|
| 1. 数学函数 | 12 | P1（高） |
| 2. 数组操作函数 | 16 | P1（高） |
| 3. 反射函数 | 3 | P2（中） |
| 7. MAP | 0 | N/A |
| 8. DT | 2 | P3（低） |
| 9. SQL | 3 | P3（低） |
| 17.7 BAR/BARL | 2 | P3（低） |
| **总计** | **38** | |

### 已确认 Float 支持 ✅

| 类别 | 数量 |
|------|------|
| SETVAR | 1 |
| EXISTMETH | 1 |
| VARSET / CVARSET | 2 |
| ARRAYSHIFT/REMOVE/SORT/COPY | 4 |
| SAVEDATA/LOADDATA/SAVEGLOBAL/LOADGLOBAL | 4 |
| TIMES | 1 |
| TOINT/TOFLOAT/TOSTR/TOSTRF | 4 |
| RETURNF | 1 |

---

## 实施批次建议

| 批次 | 内容 | 函数数 | 优先级 |
|------|------|--------|--------|
| **Batch 1** | 数学函数 Float 重载 | 12 | P1 |
| **Batch 2** | 数组操作函数 Float 重载 | 16 | P1 |
| **Batch 3** | 反射函数 (GETVARF/GETMETHF/EVALF) | 3 | P2 |
| **Batch 4** | DT Float 支持 | 2 | P3 |
| **Batch 5** | SQL Float 支持 | 3 | P3 |
| **Batch 6** | BAR/BARL Float 支持 | 2 | P3 |
