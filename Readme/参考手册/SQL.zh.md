# SQL 数据库操作函数集 ¶

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| **SQL_CONNECT** | string dbName (, string connectionString) | 命令/表达式。返回1=成功，0=失败 |
| **SQL_DISCONNECT** | string dbName | 命令/表达式。返回1 |
| **SQL_EXECUTE_NONQUERY** | string dbName, string sql | 命令/表达式。返回受影响行数 |
| **SQL_EXECUTE_READER** | string dbName, string sql | 命令/表达式。返回readerId |
| **SQL_READER_READ** | long readerId | 命令/表达式。返回1=有数据，0=已读完 |
| **SQL_READER_GET_LONG** | long readerId, int columnIndex | 命令/表达式。返回整数值，NULL返回0 |
| **SQL_READER_GET_STRING** | long readerId, int columnIndex | 命令/表达式。返回字符串值 |
| **SQL_READER_ISNULL** | long readerId, int columnIndex | 命令/表达式。返回1=NULL，0=非NULL |
| **SQL_READER_CLOSE** | long readerId | 命令/表达式。返回1 |
| **SQL_EXECUTE_SCALAR_LONG** | string dbName, string sql | 命令/表达式。返回查询结果整数 |
| **SQL_EXECUTE_SCALAR_STRING** | string dbName, string sql | 命令/表达式。返回查询结果字符串 |
| **SQL_ESCAPE** | string | 表达式。返回转义后的字符串 |
| **SQL_P_EXECUTE_NONQUERY** | string dbName, string sql, params... | 命令/表达式。返回受影响行数 |
| **SQL_P_EXECUTE_READER** | string dbName, string sql, params... | 命令/表达式。返回readerId |
| **SQL_P_EXECUTE_SCALAR_LONG** | string dbName, string sql, params... | 命令/表达式。返回查询结果整数 |
| **SQL_P_EXECUTE_SCALAR_STRING** | string dbName, string sql, params... | 命令/表达式。返回查询结果字符串 |
| **SQL_IMPORT_MAP_XML** | string dbName, string tableName, string filePath | 命令/表达式。返回1=成功 |
| **SQL_IMPORT_DT_XML** | string dbName, string tableName, string schemaPath, string dataPath | 命令/表达式。返回1=成功 |
| **SQL_EXPORT_MAP_XML** | string dbName, string tableName, string filePath | 命令/表达式。返回1=成功 |
| **SQL_EXPORT_DT_XML** | string dbName, string tableName, string schemaPath, string dataPath | 命令/表达式。返回1=成功 |
| **SQL_IMPORT_XML_CUSTOM** | string dbName, string tableName, string filePath, string rowXPath, string columnMappings | 命令/表达式。返回1=成功 |

### API

``` { #language-erbapi }
int SQL_CONNECT dbNameString{, connectionString}
int SQL_DISCONNECT dbNameString
int SQL_EXECUTE_NONQUERY dbNameString, sqlString
int SQL_EXECUTE_READER dbNameString, sqlString
int SQL_READER_READ readerIdNum
int SQL_READER_GET_LONG readerIdNum, columnIndexNum
string SQL_READER_GET_STRING readerIdNum, columnIndexNum
int SQL_READER_ISNULL readerIdNum, columnIndexNum
int SQL_READER_CLOSE readerIdNum
int SQL_EXECUTE_SCALAR_LONG dbNameString, sqlString
string SQL_EXECUTE_SCALAR_STRING dbNameString, sqlString
string SQL_ESCAPE inputString
int SQL_P_EXECUTE_NONQUERY dbNameString, sqlString{, param0, param1, ...}
int SQL_P_EXECUTE_READER dbNameString, sqlString{, param0, param1, ...}
int SQL_P_EXECUTE_SCALAR_LONG dbNameString, sqlString{, param0, param1, ...}
string SQL_P_EXECUTE_SCALAR_STRING dbNameString, sqlString{, param0, param1, ...}
int SQL_IMPORT_MAP_XML dbNameString, tableNameString, filePathString
int SQL_IMPORT_DT_XML dbNameString, tableNameString, schemaPathString, dataPathString
int SQL_EXPORT_MAP_XML dbNameString, tableNameString, filePathString
int SQL_EXPORT_DT_XML dbNameString, tableNameString, schemaPathString, dataPathString
int SQL_IMPORT_XML_CUSTOM dbNameString, tableNameString, filePathString, rowXPathString, columnMappingsString
```

提供完整的 SQLite 数据库操作能力，支持内存数据库和文件数据库。

### Hint

!!! hint "Hint"

    **命令/表达式。**

    所有SQL函数作为命令时，使用空格分隔参数，也可以作为表达式调用。

    命令语法示例：
    ```
    SQL_CONNECT "gameData"
    SQL_EXECUTE_NONQUERY "gameData", "CREATE TABLE IF NOT EXISTS players (name TEXT, score INTEGER)"
    ```

    表达式语法示例：
    ```
    IF SQL_CONNECT("gameData") == 1
        LOCAL:0 = SQL_EXECUTE_SCALAR_LONG("gameData", "SELECT COUNT(*) FROM players")
    ENDIF
    ```

1. **SQL_CONNECT** - 连接数据库
   - `dbName`: 数据库标识符，用于后续操作引用
   - `connectionString`: SQLite 连接字符串，默认值为 `"Data Source=:memory:"`（内存数据库）
   - 返回 1 表示连接成功，0 表示失败

2. **SQL_DISCONNECT** - 断开数据库连接
   - `dbName`: 要断开的数据库标识符
   - 总是返回 1

3. **SQL_EXECUTE_NONQUERY** - 执行非查询语句（INSERT, UPDATE, DELETE, CREATE 等）
   - `dbName`: 数据库标识符
   - `sql`: SQL 语句
   - 返回受影响的行数

4. **SQL_EXECUTE_READER** - 执行查询语句（SELECT）
   - `dbName`: 数据库标识符
   - `sql`: SELECT 语句
   - 返回 readerId（读取器标识符），用于后续读取操作

5. **SQL_READER_READ** - 读取下一行数据
   - `readerId`: 读取器标识符
   - 返回 1 表示有数据，0 表示已读完

6. **SQL_READER_GET_LONG** - 获取整数列
   - `readerId`: 读取器标识符
   - `columnIndex`: 列索引（从 0 开始）
   - 返回整数值，NULL 时返回 0

7. **SQL_READER_GET_STRING** - 获取字符串列
   - `readerId`: 读取器标识符
   - `columnIndex`: 列索引（从 0 开始）
   - 返回字符串值，NULL 时返回空字符串

8. **SQL_READER_ISNULL** - 检查列是否为 NULL
   - `readerId`: 读取器标识符
   - `columnIndex`: 列索引（从 0 开始）
   - 返回 1 表示为 NULL，0 表示不为 NULL

9. **SQL_READER_CLOSE** - 关闭读取器，释放资源
   - `readerId`: 读取器标识符
   - 总是返回 1

10. **SQL_EXECUTE_SCALAR_LONG** - 执行标量查询并返回整数
    - `dbName`: 数据库标识符
    - `sql`: SELECT 语句（返回单行单列）
    - 返回查询结果，NULL 时返回 0

11. **SQL_EXECUTE_SCALAR_STRING** - 执行标量查询并返回字符串
    - `dbName`: 数据库标识符
    - `sql`: SELECT 语句（返回单行单列）
    - 返回查询结果，NULL 时返回空字符串

12. **SQL_ESCAPE** - SQL 字符串转义
    - `input`: 要转义的字符串
    - 返回将 `'` 替换为 `''` 后的字符串
    - 用于拼接 SQL 时的安全处理，防止单引号破坏 SQL 语法
    - **纯函数**（CanRestructure = true），可被编译器优化

13. **SQL_P_EXECUTE_NONQUERY** - 参数化执行非查询语句
    - `dbName`: 数据库标识符
    - `sql`: SQL 语句，使用 `@0`, `@1`, `@2`... 作为占位符
    - `param0, param1, ...`: 可变参数，按序绑定到占位符
    - 返回受影响的行数
    - 参数值通过 SQLite 原生参数绑定机制传递，**不会被解释为 SQL 代码**，彻底避免 SQL 注入

14. **SQL_P_EXECUTE_READER** - 参数化执行查询语句
    - 参数同 SQL_P_EXECUTE_NONQUERY
    - 返回 readerId

15. **SQL_P_EXECUTE_SCALAR_LONG** - 参数化标量查询（整数）
    - 参数同 SQL_P_EXECUTE_NONQUERY
    - 返回查询结果整数

16. **SQL_P_EXECUTE_SCALAR_STRING** - 参数化标量查询（字符串）
    - 参数同 SQL_P_EXECUTE_NONQUERY
    - 返回查询结果字符串

17. **SQL_IMPORT_MAP_XML** - [流式导入] 将 MAP 格式 XML 导入 SQLite
    - `dbName`: 数据库标识符
    - `tableName`: 目标表名
    - `filePath`: XML 文件路径（相对于程序根目录）
    - 内存占用极低，支持大规模数据。表结构固定为 `(k TEXT PRIMARY KEY, v TEXT)`。

18. **SQL_IMPORT_DT_XML** - [流式导入] 将 DataTable 格式 XML 导入 SQLite
    - `dbName`: 数据库标识符
    - `tableName`: 目标表名
    - `schemaPath`: XML 架构文件路径 (.xsd)
    - `dataPath`: XML 数据文件路径 (.xml)
    - 内存占用极低，自动根据 Schema 创建表结构。

19. **SQL_EXPORT_MAP_XML** - 将 SQLite 表导出为 MAP 格式 XML
    - `dbName`: 数据库标识符
    - `tableName`: 源表名
    - `filePath`: 目标 XML 文件路径
    - 导出的格式兼容 Emuera 的 MAP 系统。

20. **SQL_EXPORT_DT_XML** - 将 SQLite 表导出为 DataTable 格式 XML
    - `dbName`: 数据库标识符
    - `tableName`: 源表名
    - `schemaPath`: 目标架构文件路径
    - `dataPath`: 目标数据文件路径
    - 导出的格式兼容 Emuera 的 DataTable 系统。

21. **SQL_IMPORT_XML_CUSTOM** - [通用流式导入] 根据 XPath 映射将复杂 XML 导入 SQLite
    - `dbName`: 数据库标识符
    - `tableName`: 目标表名
    - `filePath`: XML 文件路径
    - `rowXPath`: 行节点路径 (例如 `/data/enemy_data`)
    - `columnMappings`: 列映射定义，格式为 `"列名1=映射1,列名2=映射2"`
        - `@attr`: 获取属性值
        - `node`: 获取子节点 InnerText
        - `node(xml)`: 获取子节点 InnerXml (保留嵌套结构)
    - 内存占用极低。通过将复杂 XML 的分支以文本形式存入 SQL，可以实现大规模非结构化数据的快速索引与按需加载。

**重要提示：**
- 使用完读取器后务必调用 `SQL_READER_CLOSE` 防止内存泄漏
- 游戏重置时会自动清理所有数据库连接和读取器资源
- **优先使用 `SQL_P_*` 参数化查询**，避免 SQL 注入风险
- `SQL_ESCAPE` 仅作为不方便使用参数化查询时的后备方案

### Hint

- 支持内存数据库（`:memory:`）用于临时数据存储
- 支持文件数据库用于持久化存储
- 适合用于存储大量结构化数据、实现存档扩展等功能

### 参数化查询详解

参数化查询使用 `@0`, `@1`, `@2`... 占位符，后续可变参数按序绑定：

| 写法 | 安全性 | 说明 |
| :--- | :--- | :--- |
| `SQL_EXECUTE_NONQUERY db, @"INSERT INTO t(v) VALUES('%val%')"` | ❌ 不安全 | 字符串拼接，`val` 含 `'` 时 SQL 语法错误或注入 |
| `SQL_EXECUTE_NONQUERY db, @"INSERT INTO t(v) VALUES('%SQL_ESCAPE(val)%')"` | ⚠️ 较安全 | 手动转义，但仍有遗漏风险 |
| `SQL_P_EXECUTE_NONQUERY db, "INSERT INTO t(v) VALUES(@0)", val` | ✅ 安全 | 参数化绑定，参数值不会被解释为 SQL 代码 |

占位符规则：
- `@0` 对应第 3 个参数（第 1 个可变参数）
- `@1` 对应第 4 个参数（第 2 个可变参数）
- 以此类推
- 可变参数数量不限，可省略（退化为普通查询）

### Example

**MAIN.ERB**
```erb
@SYSTEM_TITLE
    ; 1. 连接内存数据库
    SQL_CONNECT "mydb"
    
    ; 2. 创建表
    SQL_EXECUTE_NONQUERY "mydb", "CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)"
    
    ; 3. 插入数据（拼接方式）
    SQL_EXECUTE_NONQUERY "mydb", "INSERT INTO users VALUES (1, '剑士', 25)"
    SQL_EXECUTE_NONQUERY "mydb", "INSERT INTO users VALUES (2, '魔法师', 30)"
    
    ; 4. 插入数据（参数化方式 — 推荐）
    SQL_P_EXECUTE_NONQUERY "mydb", "INSERT INTO users VALUES (@0, @1, @2)", "3", "盗贼", "22"
    
    ; 5. 查询数据
    LOCAL:0 = SQL_EXECUTE_READER "mydb", "SELECT * FROM users"
    
    ; 6. 遍历结果
    PRINTFORML "用户列表："
    WHILE SQL_READER_READ(LOCAL:0)
        LOCAL:1 = SQL_READER_GET_LONG(LOCAL:0, 0)
        LOCALS:0 = SQL_READER_GET_STRING(LOCAL:0, 1)
        LOCAL:2 = SQL_READER_GET_LONG(LOCAL:0, 2)
        PRINTFORML ID:{LOCAL:1} 姓名:%LOCALS:0% 年龄:{LOCAL:2}
    WEND
    
    ; 7. 关闭读取器
    SQL_READER_CLOSE LOCAL:0

    ; 8. 参数化查询（WHERE 条件）
    LOCALS:name = "剑士"
    LOCAL:0 = SQL_P_EXECUTE_READER("mydb", "SELECT * FROM users WHERE name = @0", LOCALS:name)
    IF SQL_READER_READ(LOCAL:0)
        PRINTFORML 找到: %SQL_READER_GET_STRING(LOCAL:0, 1)%
    ENDIF
    SQL_READER_CLOSE LOCAL:0

    ; 9. SQL_ESCAPE 后备方案
    LOCALS:safeName = %SQL_ESCAPE(LOCALS:name)%
    SQL_EXECUTE_NONQUERY "mydb", @"INSERT INTO log(msg) VALUES('%LOCALS:safeName%加入了队伍')"
    
    ; 10. 断开连接（可选，游戏重置时会自动清理）
    SQL_DISCONNECT "mydb"

    ONEINPUT
```

**Result**
```text
用户列表：
ID:1 姓名:剑士 年龄:25
ID:2 姓名:魔法师 年龄:30
ID:3 姓名:盗贼 年龄:22
找到: 剑士
```
