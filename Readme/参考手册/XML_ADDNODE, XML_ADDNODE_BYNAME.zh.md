# XML_ADDNODE, XML_ADDNODE_BYNAME ¶

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| **XML_ADDNODE** | int xmlId, string xpath, string nodeXml(, int methodType, int doSetAll) | int |
| **XML_ADDNODE** | ref string xml, string xpath, string nodeXml(, int methodType, int doSetAll) | int |
| **XML_ADDNODE_BYNAME** | string xmlName, string xpath, string nodeXml(, int methodType, int doSetAll) | int |

### API

1. `int XML_ADDNODE(xmlId, xpath, nodeXml[, methodType, doSetAll])`
2. `int XML_ADDNODE(ref xml, xpath, nodeXml[, methodType, doSetAll])`
3. `int XML_ADDNODE_BYNAME(xmlName, xpath, nodeXml[, methodType, doSetAll])`

根据 `xpath` 选择的元素节点（详见 XPath 的介绍），将新的元素节点 `nodeXml` 添加到指定的 XML 中。
函数返回值为 **成功匹配到的目标节点数量**；如果 XML 解析失败或指定的 ID 不存在，返回 -1。

* **methodType（插入方式）**：
  * `0` 或省略：将选择的元素作为父级节点，在其子节点列表的末尾追加 `nodeXml` 作为子节点。
  * `1`：在选择的元素节点之前，添加 `nodeXml` 作为其前置兄弟节点（根节点除外）。
  * `2`：在选择的元素节点之后，添加 `nodeXml` 作为其后置兄弟节点（根节点除外）。

* **doSetAll（批量操作）**：
  * 当 `xpath` 的匹配结果存在多个时，默认（或设为 `0`）将**不执行任何插入操作**（但仍会返回匹配到的数量）。
  * 必须将 `doSetAll` 设为 `0` 以外的数值（如 `1`），才会对所有匹配到的节点批量执行插入操作。

#### 重载说明
* **指定 ID**：从以 `xmlId` 的字符串转换结果（TOSTR）为 ID 而指定的 XmlDocument 中检索并修改。
* **指定 变量**：从指定的 `xml` 字符串变量中检索，修改后会将新的 XML 文本重新赋值给该变量。
* **指定 名称**：从以 `xmlName` 为 ID 而指定的 XmlDocument 中检索并修改。

### Hint

* 命令 / 行内函数两种写法均有效。
* 传入的 `nodeXml` 必须是格式合法的 XML 字符串（例如 `<child id="1">text</child>`）。

### Example

**MAIN.ERB**
```erb
@SYSTEM_TITLE
    #DIMS xml = "<xml><item/><item/></xml>"

    ; 示例 1：向单个匹配节点添加子节点
    PRINTFORML {XML_ADDNODE(xml, "/xml", "<child/>")} -> %xml%

    ; 示例 2：向多个匹配节点批量添加兄弟节点
    XML_DOCUMENT 0, xml
    ; 匹配到 2 个 <item/> 节点，methodType=1(前置兄弟节点)，doSetAll=1(全部应用)
    PRINTFORML 匹配数量: {XML_ADDNODE(0, "/xml/item", "<brother/>", 1, 1)}
    PRINTSL XML_TOSTR(0)

    ONEINPUT
Result
code
Text
1 -> <xml><item /><item /><child /></xml>
匹配数量: 2
<xml><brother /><item /><brother /><item /><child /></xml>
```