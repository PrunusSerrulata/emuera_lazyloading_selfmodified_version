# 移植执行手册：GETCSVNOBY* 名字反查

> **源**：EmueraDotNet `Creator.Method.cs` `GetCsvNoMethod`  
> **目标**：emuera_lazyloading_selfmodified_version `Creator.Method.cs` + `Creator.cs` + `ConstantData.cs`  
> **日期**：2026-05-09  
> **状态**：✅ 已完成

---

## 一、概述

新增 4 个 ERB 表达式函数，通过角色名/昵称/称呼/主人名反查角色模板编号（`TARGET` 等变量使用的编号）。

| 函数 | 反查字段 |
|------|---------|
| `GETCSVNOBYNAME(str)` | `NAME`（角色名） |
| `GETCSVNOBYNICKNAME(str)` | `NICKNAME`（昵称） |
| `GETCSVNOBYCALLNAME(str)` | `CALLNAME`（称呼） |
| `GETCSVNOBYMASTERNAME(str)` | `MASTERNAME`（主人名） |

返回值：找到则返回模板编号（≥0），未找到返回 -1。

---

## 二、涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| [ConstantData.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Data/ConstantData.cs) | 修改 | 新增 4 个 `Dictionary<string, long>` + 填充逻辑 |
| [Creator.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.cs) | 修改 | 注册 4 个函数名 |
| [Creator.Method.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs) | 修改 | 新增 `GetCsvNoMethod` 类 |

---

## 三、前置差异分析

### 3.1 ConstantData 差异

| 对比项 | DotNet | LazyLoading |
|--------|--------|-------------|
| `_nameToTemplateMap` | ✅ `Dictionary<string, long>` | ❌ 不存在 |
| `_nicknameToTemplateMap` | ✅ `Dictionary<string, long>` | ❌ 不存在 |
| `_callnameToTemplateMap` | ✅ `Dictionary<string, long>` | ❌ 不存在 |
| `_masternameToTemplateMap` | ✅ `Dictionary<string, long>` | ❌ 不存在 |
| 填充时机 | `AddCharaCsv()` 中逐模板填充 | — |

LazyLoading 的 `ConstantData` 缺少这 4 个反向查找字典，需要新增。

### 3.2 Creator.cs 注册差异

DotNet 在 [Creator.cs:L34-L37](file:///d:/emuera/EmueraDotNet/Runtime/Script/Statements/Function/Creator.cs) 注册了 4 个函数：

```csharp
["GETCSVNOBYNAME"] = new GetCsvNoMethod(CharacterStrData.NAME),
["GETCSVNOBYNICKNAME"] = new GetCsvNoMethod(CharacterStrData.NICKNAME),
["GETCSVNOBYCALLNAME"] = new GetCsvNoMethod(CharacterStrData.CALLNAME),
["GETCSVNOBYMASTERNAME"] = new GetCsvNoMethod(CharacterStrData.MASTERNAME),
```

LazyLoading 的 [Creator.cs:L31](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.cs) 无此注册。

### 3.3 GetCsvNoMethod 类

DotNet 实现（[Creator.Method.cs:L226-L258](file:///d:/emuera/EmueraDotNet/Runtime/Script/Statements/Function/Creator.Method.cs)）：

```csharp
private sealed class GetCsvNoMethod : FunctionMethod
{
    private CharacterStrData _type;
    public GetCsvNoMethod(CharacterStrData data)
    {
        ReturnType = typeof(Int64);
        argumentTypeArray = new Type[] { typeof(string) };
        CanRestructure = true;
        _type = data;
    }
    public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
    {
        var str = arguments[0].GetStrValue(exm);
        long ret;
        var b = _type switch
        {
            CharacterStrData.NAME => exm.VEvaluator.Constant.NameToTemplateMap.TryGetValue(str, out ret),
            CharacterStrData.NICKNAME => exm.VEvaluator.Constant.NicknameToTemplateMap.TryGetValue(str, out ret),
            CharacterStrData.CALLNAME => exm.VEvaluator.Constant.CallnameToTemplateMap.TryGetValue(str, out ret),
            CharacterStrData.MASTERNAME => exm.VEvaluator.Constant.MasternameToTemplateMap.TryGetValue(str, out ret),
            _ => throw new ExeEE("error")
        };
        if (!b)
            ret = -1;
        return ret;
    }
}
```

LazyLoading 无此类。

---

## 四、执行步骤

### 步骤 1：ConstantData.cs — 新增反向查找字典

**文件**：[ConstantData.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Data/ConstantData.cs)

#### 1a. 新增字段（在 `CharacterTmplList` 附近）

```csharp
private Dictionary<string, long> _nameToTemplateMap = [];
private Dictionary<string, long> _nicknameToTemplateMap = [];
private Dictionary<string, long> _callnameToTemplateMap = [];
private Dictionary<string, long> _masternameToTemplateMap = [];
```

#### 1b. 新增公开属性

```csharp
public IReadOnlyDictionary<string, long> NameToTemplateMap => _nameToTemplateMap;
public IReadOnlyDictionary<string, long> NicknameToTemplateMap => _nicknameToTemplateMap;
public IReadOnlyDictionary<string, long> CallnameToTemplateMap => _callnameToTemplateMap;
public IReadOnlyDictionary<string, long> MasternameToTemplateMap => _masternameToTemplateMap;
```

> **注意**：LazyLoading 使用 .NET 8，`IReadOnlyDictionary` 可直接使用，无需 `AsReadOnly()`。

#### 1c. 填充逻辑（在 `AddCharaCsv()` 方法中，遍历 `CharacterTmplList` 的位置）

在添加模板到列表的循环中，追加：

```csharp
_nameToTemplateMap[t.Name] = t.No;
_nicknameToTemplateMap[t.Nickname] = t.No;
_callnameToTemplateMap[t.Callname] = t.No;
_masternameToTemplateMap[t.Mastername] = t.No;
```

> **注意**：如果同名角色存在多个模板，后面的会覆盖前面的（与 DotNet 行为一致）。

---

### 步骤 2：Creator.cs — 注册函数

**文件**：[Creator.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.cs)

在 `["CSVJUEL"]` 行之后、`["FINDCHARA"]` 行之前插入：

```csharp
["GETCSVNOBYNAME"] = new GetCsvNoMethod(CharacterStrData.NAME),
["GETCSVNOBYNICKNAME"] = new GetCsvNoMethod(CharacterStrData.NICKNAME),
["GETCSVNOBYCALLNAME"] = new GetCsvNoMethod(CharacterStrData.CALLNAME),
["GETCSVNOBYMASTERNAME"] = new GetCsvNoMethod(CharacterStrData.MASTERNAME),
```

---

### 步骤 3：Creator.Method.cs — 新增 GetCsvNoMethod 类

**文件**：[Creator.Method.cs](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Statements/Function/Creator.Method.cs)

在 `CsvDataMethod` 类之后（或 `FindcharaMethod` 类之前）添加：

```csharp
private sealed class GetCsvNoMethod : FunctionMethod
{
    private CharacterStrData _type;
    public GetCsvNoMethod(CharacterStrData data)
    {
        ReturnType = typeof(long);
        argumentTypeArray = [typeof(string)];
        CanRestructure = true;
        _type = data;
    }
    public override long GetIntValue(ExpressionMediator exm, List<AExpression> arguments)
    {
        var str = arguments[0].GetStrValue(exm);
        long ret;
        var b = _type switch
        {
            CharacterStrData.NAME => exm.VEvaluator.Constant.NameToTemplateMap.TryGetValue(str, out ret),
            CharacterStrData.NICKNAME => exm.VEvaluator.Constant.NicknameToTemplateMap.TryGetValue(str, out ret),
            CharacterStrData.CALLNAME => exm.VEvaluator.Constant.CallnameToTemplateMap.TryGetValue(str, out ret),
            CharacterStrData.MASTERNAME => exm.VEvaluator.Constant.MasternameToTemplateMap.TryGetValue(str, out ret),
            _ => throw new ExeEE("error")
        };
        if (!b)
            ret = -1;
        return ret;
    }
}
```

---

### 步骤 4：验证编译

```powershell
dotnet build "D:\emuera\emuera_lazyloading_selfmodified_version\emuera_lazyloading_selfmodified_version.sln" 2>&1
```

预期：0 错误，0 警告。

---

## 五、设计决策说明

### 5.1 为什么用 Dictionary 而非遍历 CharacterTmplList

`CharacterTmplList` 是 `List<CharacterTemplate>`，每次 `GETCSVNOBYNAME` 调用都遍历整个列表是 O(n) 操作。使用 `Dictionary<string, long>` 是 O(1) 查找，与 DotNet 实现一致。

### 5.2 同名覆盖行为

如果多个模板有相同的名字（如不同编号的同一角色），`Dictionary` 会保留最后一个。这与 DotNet 行为一致，也是合理的：后加载的模板覆盖先加载的。

### 5.3 为什么不用 `ReadOnlyDictionary`

LazyLoading 使用 .NET 8，`IReadOnlyDictionary<TKey, TValue>` 接口已足够。`Dictionary<TKey, TValue>` 直接实现该接口，无需额外包装。

---

## 六、风险点与验证

### 6.1 功能验证

1. 启动引擎，确认无编译错误
2. 在 ERB 中测试：
   ```
   LOCALS = GETCSVNOBYNAME("角色名")
   PRINTFORML 找到编号：%LOCALS%
   ```
3. 测试未找到的情况（应返回 -1）
4. 测试 4 个函数分别返回正确结果

### 6.2 性能验证

- `Dictionary.TryGetValue` 是 O(1) 操作，无性能风险
- 字典在 `AddCharaCsv()` 时一次性构建，不影响运行时性能

---

## 七、回滚方案

1. 删除 `Creator.cs` 中新增的 4 行注册
2. 删除 `Creator.Method.cs` 中新增的 `GetCsvNoMethod` 类
3. 删除 `ConstantData.cs` 中新增的 4 个字段、4 个属性、4 行填充逻辑
4. 重新编译

改动涉及 3 个文件，回滚成本低。