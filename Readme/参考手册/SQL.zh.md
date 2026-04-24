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

12. **SQL_IMPORT_MAP_XML** - [流式导入] 将 MAP 格式 XML 导入 SQLite
    - `dbName`: 数据库标识符
    - `tableName`: 目标表名
    - `filePath`: XML 文件路径（相对于程序根目录）
    - 内存占用极低，支持大规模数据。表结构固定为 `(k TEXT PRIMARY KEY, v TEXT)`。

13. **SQL_IMPORT_DT_XML** - [流式导入] 将 DataTable 格式 XML 导入 SQLite
    - `dbName`: 数据库标识符
    - `tableName`: 目标表名
    - `schemaPath`: XML 架构文件路径 (.xsd)
    - `dataPath`: XML 数据文件路径 (.xml)
    - 内存占用极低，自动根据 Schema 创建表结构。

14. **SQL_EXPORT_MAP_XML** - 将 SQLite 表导出为 MAP 格式 XML
    - `dbName`: 数据库标识符
    - `tableName`: 源表名
    - `filePath`: 目标 XML 文件路径
    - 导出的格式兼容 Emuera 的 MAP 系统。

15. **SQL_EXPORT_DT_XML** - 将 SQLite 表导出为 DataTable 格式 XML
    - `dbName`: 数据库标识符
    - `tableName`: 源表名
    - `schemaPath`: 目标架构文件路径
    - `dataPath`: 目标数据文件路径
    - 导出的格式兼容 Emuera 的 DataTable 系统。

16. **SQL_IMPORT_XML_CUSTOM** - [通用流式导入] 根据 XPath 映射将复杂 XML 导入 SQLite
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

### Hint

- 支持内存数据库（`:memory:`）用于临时数据存储
- 支持文件数据库用于持久化存储
- 适合用于存储大量结构化数据、实现存档扩展等功能

### Example

**MAIN.ERB**
```erb
@SYSTEM_TITLE
    ; 1. 连接内存数据库
    SQL_CONNECT "mydb"
    
    ; 2. 创建表
    SQL_EXECUTE_NONQUERY "mydb", "CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)"
    
    ; 3. 插入数据
    SQL_EXECUTE_NONQUERY "mydb", "INSERT INTO users VALUES (1, '剑士', 25)"
    SQL_EXECUTE_NONQUERY "mydb", "INSERT INTO users VALUES (2, '魔法师', 30)"
    
    ; 4. 查询数据
    LOCAL:0 = SQL_EXECUTE_READER "mydb", "SELECT * FROM users"
    
    ; 5. 遍历结果
    PRINTFORML "用户列表："
    WHILE SQL_READER_READ(LOCAL:0)
        LOCAL:1 = SQL_READER_GET_LONG(LOCAL:0, 0)
        LOCALS:0 = SQL_READER_GET_STRING(LOCAL:0, 1)
        LOCAL:2 = SQL_READER_GET_LONG(LOCAL:0, 2)
        PRINTFORML ID:{LOCAL:1} 姓名:%LOCALS:0% 年龄:{LOCAL:2}
    WEND
    
    ; 6. 关闭读取器
    SQL_READER_CLOSE LOCAL:0
    
    ; 7. 断开连接（可选，游戏重置时会自动清理）
    SQL_DISCONNECT "mydb"

    ONEINPUT
```

**Result**
```text
用户列表：
ID:1 姓名:剑士 年龄:25
ID:2 姓名:魔法师 年龄:30
```
