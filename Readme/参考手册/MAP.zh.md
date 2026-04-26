# MAP 字典操作函数集 ¶

| 函数名 | 参数 | 返回值 | 来源 |
| :--- | :--- | :--- | :--- |
| **MAP_CREATE** | string mapName | 命令/表达式。返回1=创建成功，0=已存在 | 原有 |
| **MAP_EXIST** | string mapName | 命令/表达式。返回1=存在，0=不存在 | 原有 |
| **MAP_RELEASE** | string mapName | 命令/表达式。返回1 | 原有 |
| **MAP_GET** | string mapName, string key | 表达式。返回value字符串 | 原有 |
| **MAP_SET** | string mapName, string key, string value | 命令/表达式。返回1 | 原有 |
| **MAP_HAS** | string mapName, string key | 命令/表达式。返回1=存在，0=不存在 | 原有 |
| **MAP_REMOVE** | string mapName, string key | 命令/表达式。返回1 | 原有 |
| **MAP_CLEAR** | string mapName | 命令/表达式。返回1 | 原有 |
| **MAP_SIZE** | string mapName | 命令/表达式。返回键值对数量 | 原有 |
| **MAP_GETKEYS** | string mapName {, int mode} | 表达式。返回逗号分隔的key列表 | 原有 |
| **MAP_GETKEYS** | string mapName, ref string[] array, int mode | 表达式。写入数组，返回RESULTS | 原有 |
| **MAP_TOXML** | string mapName | 表达式。返回XML字符串 | 原有 |
| **MAP_FROMXML** | string mapName, string xml | 命令/表达式。返回导入数量 | 原有 |
| **MAP_VALUES** | string mapName {, int mode} | 表达式。返回逗号分隔的value列表 | **新增** |
| **MAP_VALUES** | string mapName, ref string[] array, int mode | 表达式。写入数组，返回RESULTS | **新增** |
| **MAP_MERGE** | string destMap, string srcMap | 命令/表达式。返回1=成功，0=失败 | **新增** |
| **MAP_REMOVEIF** | string mapName, string matchValue, string mode | 命令/表达式。返回删除数量，-1=无效模式 | **新增** |
| **MAP_FINDKEY** | string mapName, string matchValue, string mode | 表达式。返回逗号分隔的key列表 | **新增** |
| **MAP_TOSTRING** | string mapName {, string sep, string kvSep} | 表达式。返回序列化字符串 | **新增** |
| **MAP_FROMSTRING** | string mapName, string data {, string sep, string kvSep} | 命令/表达式。返回导入数量 | **新增** |

### API

``` { #language-erbapi }
; --- 基础管理 ---
int MAP_CREATE mapNameString
int MAP_EXIST mapNameString
int MAP_RELEASE mapNameString

; --- 数据读写 ---
string MAP_GET mapNameString, keyString
int MAP_SET mapNameString, keyString, valueString
int MAP_HAS mapNameString, keyString
int MAP_REMOVE mapNameString, keyString
int MAP_CLEAR mapNameString
int MAP_SIZE mapNameString

; --- 键值遍历 ---
string MAP_GETKEYS mapNameString{, modeNum}
string MAP_GETKEYS mapNameString, refArray, modeNum
string MAP_VALUES mapNameString{, modeNum}
string MAP_VALUES mapNameString, refArray, modeNum

; --- 序列化 / 反序列化 ---
string MAP_TOXML mapNameString
int MAP_FROMXML mapNameString, xmlString
string MAP_TOSTRING mapNameString{, sepString, kvSepString}
int MAP_FROMSTRING mapNameString, dataString{, sepString, kvSepString}

; --- 增强操作 ---
int MAP_MERGE destMapNameString, srcMapNameString
int MAP_REMOVEIF mapNameString, matchValueString, modeString
string MAP_FINDKEY mapNameString, matchValueString, modeString
```

提供基于字典（Dictionary）的键值对存储，所有 key 和 value 均为字符串类型。

### 概述：MAP 与 C# Dictionary 的关系

MAP 的底层实现是 C# 的 `Dictionary<string, string>`，存储在 `VariableData.DataStringMaps` 中。每个 MAP 通过名称（string mapName）索引，对应一个独立的字典实例。

```
DataStringMaps: Dictionary<string, Dictionary<string, string>>
                      ↑ MAP名称              ↑ key     ↑ value
```

#### 原有函数与 C# Dictionary 的对应

原版 Emuera 提供了 12 个 MAP 函数，覆盖了 `Dictionary<string, string>` 的基础 CRUD 操作：

| ERB 函数 | C# 等价操作 | 说明 |
| :--- | :--- | :--- |
| `MAP_CREATE name` | `dict[name] = new Dictionary<string,string>()` | 创建空字典 |
| `MAP_EXIST name` | `dict.ContainsKey(name)` | 检查字典是否存在 |
| `MAP_RELEASE name` | `dict.Remove(name)` | 释放字典引用 |
| `MAP_GET name, key` | `dict[name][key]` | 按键取值 |
| `MAP_SET name, key, val` | `dict[name][key] = val` | 设置键值对 |
| `MAP_HAS name, key` | `dict[name].ContainsKey(key)` | 检查键是否存在 |
| `MAP_REMOVE name, key` | `dict[name].Remove(key)` | 删除单个键 |
| `MAP_CLEAR name` | `dict[name].Clear()` | 清空所有键值对 |
| `MAP_SIZE name` | `dict[name].Count` | 获取键值对数量 |
| `MAP_GETKEYS name` | `dict[name].Keys` | 获取所有 key |
| `MAP_TOXML name` | XML 序列化 | 序列化为 XML 字符串 |
| `MAP_FROMXML name, xml` | XML 反序列化 | 从 XML 字符串导入 |

这些函数构成了字典操作的"最小集"——创建/销毁、读写、查询、遍历 key、序列化。但在实际使用中，原版函数存在以下不足：

#### 原有函数的不足与新增函数的设计动机

**1. 缺少 value 遍历 → MAP_VALUES**

原版有 `MAP_GETKEYS` 获取所有 key，但没有对称的"获取所有 value"。在 C# 中 `Dictionary.Values` 与 `Dictionary.Keys` 是天然对称的属性。缺少 value 遍历意味着要获取所有 value 必须先遍历 key 再逐个 `MAP_GET`，效率低下且代码冗长。

`MAP_VALUES` 的设计完全对称于 `MAP_GETKEYS`，包括两种输出模式（逗号分隔字符串 / RESULTS 数组 / 自定义数组引用），保持 API 风格一致。

**2. 缺少批量合并 → MAP_MERGE**

C# 中合并两个字典是常见操作（如配置覆盖、默认值填充），但 ERB 中只能手动遍历源 MAP 逐个 `MAP_SET`。`MAP_MERGE` 实现了 O(n) 的原地合并，源 MAP 中同 key 覆盖目标 MAP，语义等价于：

```csharp
foreach (var kvp in srcDict)
    destDict[kvp.Key] = kvp.Value;
```

典型场景：默认配置 MAP + 用户覆盖 MAP → 合并得到最终配置。

**3. 缺少条件查询 → MAP_FINDKEY**

C# 中通过 LINQ 可以轻松实现 `dict.Where(kvp => kvp.Key.StartsWith(prefix)).Select(kvp => kvp.Key)`，但 ERB 没有匿名函数/LINQ，条件查询只能用循环 + `SUBSTRING`/`STRLENS` 手动实现，代码量大且易出错。

`MAP_FINDKEY` 将 5 种最常见的匹配模式内置为字符串参数，一行代码即可完成条件查询：

| 模式 | C# 等价 | 典型场景 |
| :--- | :--- | :--- |
| `KEY_CONTAINS` | `key.Contains(val)` | 模糊搜索 |
| `KEY_PREFIX` | `key.StartsWith(val)` | 按前缀分类查找（如 `potion_*`） |
| `KEY_SUFFIX` | `key.EndsWith(val)` | 按后缀查找（如 `*_price`） |
| `VAL_CONTAINS` | `value.Contains(val)` | 按值内容反查 key |
| `VAL_EQ` | `value == val` | 按精确值反查 key |

**4. 缺少条件删除 → MAP_REMOVEIF**

与 `MAP_FINDKEY` 同理，C# 中可以用 `dict.Where(...).ToList().ForEach(k => dict.Remove(k))` 实现条件删除，但 ERB 中需要手动遍历 + 收集 key + 逐个 `MAP_REMOVE`，且遍历中删除会触发集合修改异常。

`MAP_REMOVEIF` 在 C# 层面先收集待删除 key 到临时列表，再统一删除，避免了遍历中修改集合的问题。它比 `MAP_FINDKEY` 多一个 `VAL_NE`（不等于）模式——因为"删除不等于某值的所有条目"是常见需求（如清理无效数据），而"查找不等于某值的所有 key"意义不大且结果可能过多。

**5. 缺少轻量序列化 → MAP_TOSTRING / MAP_FROMSTRING**

原版只有 `MAP_TOXML` / `MAP_FROMXML`，XML 格式冗长（`<map><p><k>key</k><v>value</v></p>...</map>`），不适合以下场景：
- 存入 CSV 或配置文件
- 通过 `SAVEDATA` / 网络传输
- 人类可读的调试输出

`MAP_TOSTRING` / `MAP_FROMSTRING` 提供了 `key=value` 格式的轻量序列化，默认 `"k1=v1,k2=v2"` 格式，可自定义分隔符。与 `MAP_TOXML` / `MAP_FROMXML` 的对比：

| 特性 | MAP_TOXML / MAP_FROMXML | MAP_TOSTRING / MAP_FROMSTRING |
| :--- | :--- | :--- |
| 格式 | XML 标签结构 | 纯文本 key=value |
| 体积 | 较大 | 紧凑 |
| 嵌套支持 | 理论上支持（XML 转义） | 不支持（分隔符冲突） |
| 可读性 | 低 | 高 |
| 自定义分隔符 | 不支持 | 支持 |
| 典型用途 | 复杂数据交换、兼容外部系统 | 简单数据持久化、调试输出、CSV 交互 |

### Hint

!!! hint "Hint"

    **命令/表达式。**

    除 MAP_GET、MAP_GETKEYS、MAP_VALUES、MAP_FINDKEY、MAP_TOSTRING、MAP_TOXML 只能作为表达式外，其余函数均可作为命令或表达式调用。

    命令语法示例：
    ```
    MAP_CREATE "itemDB"
    MAP_SET "itemDB", "sword", "铁剑"
    ```

    表达式语法示例：
    ```
    IF MAP_HAS("itemDB", "sword")
        LOCALS = MAP_GET("itemDB", "sword")
    ENDIF
    ```

---

### 基础管理

1. **MAP_CREATE** - 创建 MAP
   - `mapName`: MAP 名称
   - 返回 1 表示创建成功，0 表示同名 MAP 已存在

2. **MAP_EXIST** - 检查 MAP 是否存在
   - `mapName`: MAP 名称
   - 返回 1 表示存在，0 表示不存在

3. **MAP_RELEASE** - 释放 MAP
   - `mapName`: MAP 名称
   - 总是返回 1

### 数据读写

4. **MAP_GET** - 获取值
   - `mapName`: MAP 名称
   - `key`: 键
   - 返回对应的 value，key 不存在时返回空字符串

5. **MAP_SET** - 设置键值对
   - `mapName`: MAP 名称
   - `key`: 键
   - `value`: 值
   - 返回 1，MAP 不存在时返回 -1

6. **MAP_HAS** - 检查键是否存在
   - `mapName`: MAP 名称
   - `key`: 键
   - 返回 1 表示存在，0 表示不存在，MAP 不存在时返回 -1

7. **MAP_REMOVE** - 删除键值对
   - `mapName`: MAP 名称
   - `key`: 键
   - 返回 1，MAP 不存在时返回 -1

8. **MAP_CLEAR** - 清空所有键值对
   - `mapName`: MAP 名称
   - 返回 1，MAP 不存在时返回 -1

9. **MAP_SIZE** - 获取键值对数量
   - `mapName`: MAP 名称
   - 返回键值对数量，MAP 不存在时返回 -1

### 键值遍历

10. **MAP_GETKEYS** - 获取所有 key
    - `mapName`: MAP 名称
    - `mode`: 输出模式（可省略）
      - 省略或传入 0：返回逗号分隔的 key 列表字符串
      - 传入 1：将 key 写入 RESULTS 数组，RESULT 设置为 key 数量
    - `refArray`: 自定义字符串数组引用（三参数形式）
      - 传入 1 时将 key 写入指定数组，RESULT 设置为 key 数量
    - 行为与 MAP_VALUES 对称

11. **MAP_VALUES** - 获取所有 value
    - `mapName`: MAP 名称
    - `mode`: 输出模式（可省略）
      - 省略或传入 0：返回逗号分隔的 value 列表字符串
      - 传入 1：将 value 写入 RESULTS 数组，RESULT 设置为 value 数量
    - `refArray`: 自定义字符串数组引用（三参数形式）
      - 传入 1 时将 value 写入指定数组，RESULT 设置为 value 数量
    - 行为与 MAP_GETKEYS 对称

### 序列化 / 反序列化

12. **MAP_TOXML** - 序列化为 XML
    - `mapName`: MAP 名称
    - 返回 XML 格式字符串：`<map><p><k>key</k><v>value</v></p>...</map>`

13. **MAP_FROMXML** - 从 XML 反序列化
    - `mapName`: MAP 名称（必须已存在）
    - `xml`: XML 格式字符串
    - 返回导入的键值对数量

14. **MAP_TOSTRING** - 序列化为字符串
    - `mapName`: MAP 名称
    - `sep`: 条目分隔符，默认 `","`
    - `kvSep`: 键值分隔符，默认 `"="`
    - 返回格式如 `"k1=v1,k2=v2,k3=v3"`
    - 可自定义分隔符以适应不同数据格式

15. **MAP_FROMSTRING** - 从字符串反序列化
    - `mapName`: MAP 名称（必须已存在）
    - `data`: 序列化字符串
    - `sep`: 条目分隔符，默认 `","`
    - `kvSep`: 键值分隔符，默认 `"="`
    - 返回导入的键值对数量
    - 解析规则：按 `sep` 分割条目，按第一个 `kvSep` 分割键值
    - 空 entry 或无 `kvSep` 的 entry 会被跳过

### 增强操作

16. **MAP_MERGE** - 合并 MAP
    - `destMap`: 目标 MAP 名称（必须已存在）
    - `srcMap`: 源 MAP 名称（必须已存在）
    - 将源 MAP 的所有键值对合并到目标 MAP
    - 同名 key 的 value 会被源 MAP 覆盖
    - 返回 1 表示成功，0 表示任一 MAP 不存在

17. **MAP_REMOVEIF** - 按条件批量删除
    - `mapName`: MAP 名称
    - `matchValue`: 匹配值
    - `mode`: 匹配模式（见下表）
    - 返回实际删除的键值对数量，无效模式返回 -1

    | 模式 | 说明 |
    | :--- | :--- |
    | `"KEY_CONTAINS"` | key 包含 matchValue |
    | `"KEY_PREFIX"` | key 以 matchValue 开头 |
    | `"KEY_SUFFIX"` | key 以 matchValue 结尾 |
    | `"VAL_CONTAINS"` | value 包含 matchValue |
    | `"VAL_EQ"` | value 等于 matchValue |
    | `"VAL_NE"` | value 不等于 matchValue |

18. **MAP_FINDKEY** - 按条件查找 key
    - `mapName`: MAP 名称
    - `matchValue`: 匹配值
    - `mode`: 匹配模式（见下表）
    - 返回逗号分隔的匹配 key 列表
    - RESULT 设置为匹配数量

    | 模式 | 说明 |
    | :--- | :--- |
    | `"KEY_CONTAINS"` | key 包含 matchValue |
    | `"KEY_PREFIX"` | key 以 matchValue 开头 |
    | `"KEY_SUFFIX"` | key 以 matchValue 结尾 |
    | `"VAL_CONTAINS"` | value 包含 matchValue |
    | `"VAL_EQ"` | value 等于 matchValue |

    !!! note "与 MAP_REMOVEIF 的区别"
        MAP_FINDKEY 不支持 `"VAL_NE"` 模式（查找"不等于"意义不大，且结果可能过多）。
        MAP_FINDKEY 只查找不删除，MAP_REMOVEIF 查找并删除。

### Hint

- MAP 名称不存在时，大部分操作返回 -1 或空字符串，不会报错
- 所有 key 和 value 均为字符串类型，如需存储数字请自行转换
- 游戏重置时 MAP 数据会被清空，如需持久化请使用 MAP_TOXML / MAP_TOSTRING 配合文件操作
- MAP_GETKEYS / MAP_VALUES 的数组输出模式受数组长度限制，超出部分会被截断

### Example

**MAIN.ERB**
```erb
@SYSTEM_TITLE
    ; === 基础操作 ===
    MAP_CREATE "itemDB"
    MAP_SET "itemDB", "sword", "铁剑"
    MAP_SET "itemDB", "shield", "木盾"
    MAP_SET "itemDB", "potion_hp", "生命药水"
    MAP_SET "itemDB", "potion_mp", "魔力药水"
    MAP_SET "itemDB", "potion_sp", "体力药水"

    PRINTFORML MAP大小: {MAP_SIZE("itemDB")}
    PRINTFORML sword: %MAP_GET("itemDB", "sword")%
    PRINTFORML 是否有bow: {MAP_HAS("itemDB", "bow")}

    ; === MAP_VALUES 获取所有值 ===
    LOCALS:0 = %MAP_VALUES("itemDB")%
    PRINTFORML 所有值: %LOCALS:0%

    ; 写入 RESULTS 数组
    MAP_VALUES "itemDB", 1
    PRINTFORML 值数量: {RESULT}
    FOR LOCAL, 0, RESULT
        PRINTFORML   RESULTS:{LOCAL} = %RESULTS:LOCAL%
    NEXT

    ; === MAP_GETKEYS 获取所有键 ===
    LOCALS:0 = %MAP_GETKEYS("itemDB")%
    PRINTFORML 所有键: %LOCALS:0%

    ; === MAP_FINDKEY 按条件查找 ===
    LOCALS:0 = %MAP_FINDKEY("itemDB", "potion_", "KEY_PREFIX")%
    PRINTFORML potion前缀的键: %LOCALS:0% (共{RESULT}个)

    LOCALS:0 = %MAP_FINDKEY("itemDB", "药水", "VAL_CONTAINS")%
    PRINTFORML 值含"药水"的键: %LOCALS:0% (共{RESULT}个)

    ; === MAP_REMOVEIF 按条件删除 ===
    MAP_CREATE "tempDB"
    MAP_SET "tempDB", "a", "1"
    MAP_SET "tempDB", "b", "2"
    MAP_SET "tempDB", "c", "1"
    MAP_SET "tempDB", "d", "3"

    LOCAL:0 = MAP_REMOVEIF("tempDB", "1", "VAL_EQ")
    PRINTFORML 删除了 {LOCAL:0} 个值为1的条目
    PRINTFORML 剩余: %MAP_TOSTRING("tempDB")%

    ; === MAP_MERGE 合并 ===
    MAP_CREATE "baseDB"
    MAP_SET "baseDB", "name", "无名"
    MAP_SET "baseDB", "level", "1"

    MAP_CREATE "overrideDB"
    MAP_SET "overrideDB", "name", "勇者"
    MAP_SET "overrideDB", "class", "战士"

    MAP_MERGE "baseDB", "overrideDB"
    PRINTFORML 合并后: %MAP_TOSTRING("baseDB")%

    ; === MAP_TOSTRING / MAP_FROMSTRING 序列化 ===
    MAP_CREATE "saveDB"
    MAP_SET "saveDB", "hp", "100"
    MAP_SET "saveDB", "mp", "50"
    MAP_SET "saveDB", "gold", "999"

    LOCALS:0 = %MAP_TOSTRING("saveDB")%
    PRINTFORML 序列化: %LOCALS:0%

    MAP_CREATE "loadDB"
    MAP_FROMSTRING "loadDB", LOCALS:0
    PRINTFORML 反序列化: %MAP_TOSTRING("loadDB")%

    ; 自定义分隔符
    LOCALS:1 = %MAP_TOSTRING("saveDB", "|", ":")%
    PRINTFORML 自定义格式: %LOCALS:1%

    MAP_CREATE "loadDB2"
    MAP_FROMSTRING "loadDB2", LOCALS:1, "|", ":"
    PRINTFORML 自定义反序列化: %MAP_TOSTRING("loadDB2")%

    ; === MAP_TOXML / MAP_FROMXML ===
    LOCALS:0 = %MAP_TOXML("saveDB")%
    PRINTFORML XML: %LOCALS:0%

    ; === 清理 ===
    MAP_RELEASE "itemDB"
    MAP_RELEASE "tempDB"
    MAP_RELEASE "baseDB"
    MAP_RELEASE "overrideDB"
    MAP_RELEASE "saveDB"
    MAP_RELEASE "loadDB"
    MAP_RELEASE "loadDB2"

    ONEINPUT
```

**Result**
```text
MAP大小: 5
sword: 铁剑
是否有bow: 0
所有值: 铁剑,木盾,生命药水,魔力药水,体力药水
值数量: 5
  RESULTS:0 = 铁剑
  RESULTS:1 = 木盾
  RESULTS:2 = 生命药水
  RESULTS:3 = 魔力药水
  RESULTS:4 = 体力药水
所有键: sword,shield,potion_hp,potion_mp,potion_sp
potion前缀的键: potion_hp,potion_mp,potion_sp (共3个)
值含"药水"的键: potion_hp,potion_mp,potion_sp (共3个)
删除了 2 个值为1的条目
剩余: b=2,d=3
合并后: name=勇者,level=1,class=战士
序列化: hp=100,mp=50,gold=999
反序列化: hp=100,mp=50,gold=999
自定义格式: hp:100|mp:50|gold:999
自定义反序列化: hp=100,mp=50,gold=999
XML: <map><p><k>hp</k><v>100</v></p><p><k>mp</k><v>50</v></p><p><k>gold</k><v>999</v></p></map>
```
