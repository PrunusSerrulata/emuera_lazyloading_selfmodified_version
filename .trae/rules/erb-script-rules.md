# ERB 脚本编写规则

> 编写 ERB/ERH 测试脚本时，必须先查阅 erabasic 语法知识库：[syntax-quickref.md](file:///d:/emuera/shared-trae/knowledge/erabasic/syntax-quickref.md)

- `#DIM` 是预处理指令，`#` 不可省略（不能写成 `DIM`）
- 字符串字面量需用 `""` 包裹（如 `"pet_1"`），否则会被当作变量名
- 数组可用内联初始化：`#DIM ARR, 20 = 1, 2, 3, ...`
- A-Z 单字母变量是引擎保留变量，不可用于 `#DIM`

## API 确认流程（强制）

调用任何 ERB 指令/函数前，必须确认 API 签名：

1. 先查知识库是否有该指令的 API 文档
2. 若无，查源码 `DoInstruction` / `ArgumentBuilder` 确认参数个数和类型
3. **禁止凭猜测或类比编写参数**（如 CBGSETIMAGE 仅 1 个 STR 参数，不支持颜色矩阵；HTML img 无 cm 属性）
