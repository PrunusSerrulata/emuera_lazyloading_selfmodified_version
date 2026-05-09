# Emuera LazyLoading版 vs EmueraDotNet 对比审查工作日志

## 工作进度追踪

### 2026-05-09

**工作目标**：对比两者 SkiaSharp 实现与接口差异，补充移植工作手册和知识库

---

### 阶段六：SkiaSharp 实现深度对比

**时间**：全天

**工作内容**：
- 逐文件对比两个项目的 SkiaSharp 渲染实现
- 分析 AbstractImage 基类、ASpriteSingle/ASpriteAnime、GraphicsImage、EraPictureBox、ConsoleImagePart 等核心渲染组件
- 识别颜色矩阵转换公式差异（关键发现）
- 更新移植工作手册和知识库

**关键发现**：

1. **重要纠正**：两个项目**均已使用 SkiaSharp**。LazyLoading 版（正式命名"skia变体"）是 SkiaSharp + GDI+ 回退的双轨方案，而非纯 GDI+。

2. **AbstractImage 基类差异**：
   - skia变体：单一 `SKBitmap` 类型
   - DotNet：`SKImage`（优先）+ `SKBitmap`（回退）双类型系统

3. **颜色矩阵转换差异（⚠️ 关键）**：
   - skia变体：行主序（`cm[row][col]`）+ offset×255
   - DotNet：列主序（`cm[col][row]`，即转置）+ offset 原值
   - **两个实现对同一 ERB 颜色矩阵输入产生不同渲染结果**

4. **渲染后端差异**：
   - skia变体：OpenGL/CPU 双后端 + 自动回退（3次失败阈值）
   - DotNet：仅 SKGLControl，无回退逻辑

5. **动画系统差异**：
   - skia变体：DateTime.Now 计时 + 暂停/恢复 + 动图 LRU 缓存
   - DotNet：Stopwatch 高精度计时 + 无暂停/恢复 + 无动图支持

6. **ConsoleImagePart 差异**：
   - skia变体：HTML img cm 颜色矩阵 + srcm 图片映射
   - DotNet：DisplayMode 绝对定位

7. **GraphicsImage 差异**：
   - skia变体：GDI+ 风格 Brush/Pen + drawImgList 优化 + GDI+ 文本回退
   - DotNet：Skia 原生 SKPaint + G_POLYGON 多边形绘制

**输出**：
- 更新 `emueradotnet-portability-assessment.md` → v3.0（重写第一章 SkiaSharp 实现对比）
- 更新 `lazyloading-features.md` → v2.0（新增 SkiaSharp 渲染系统章节）
- 更新 `emueradotnet-features.md` → v2.0（扩展 SkiaSharp 渲染章节）

---

### 阶段七：文档整合

**时间**：同日

**工作内容**：
- 将 `emuera-vs-emueradotnet-comparison.md` 第1-4章合并至 `emueradotnet-portability-assessment.md`
- 删除 comparison 文档第5-8章（适用场景分析、技术选型建议、迁移注意事项、总结）
- 为 portability-assessment 新增目录页
- 全文重新编号为十二章

**输出**：
- `emueradotnet-portability-assessment.md` → v4.0（整合为十二章 + 目录页）
- `emuera-vs-emueradotnet-comparison.md` → v2.0（精简为四章）

---

### 阶段八：SQL 与字典数据类型深度对比

**时间**：同日

**工作内容**：
- 对比 EmueraDotNet SQL（9函数）与 LazyLoading SQL（24函数）的架构差异
- 对比 EmueraDotNet DICT（6函数）与 LazyLoading MAP（18函数）的功能差异
- 分析 DICT 的 6 项实现缺陷（哈希碰撞、无管理函数、无序列化等）
- 分析 LazyLoading SQL 的架构优势（ReaderContext、参数化查询、XML导入导出）
- 更新三个知识库文件和移植评估文档

**关键发现**：

1. **SQL 对比**：
   - LazyLoading 版 SQL 功能远超 DotNet（24 vs 9 个函数）
   - LazyLoading 独有：多连接管理、参数化查询（`@0`, `@1`）、ReaderContext 封装、Float 类型、XML 导入导出（MAP/DT/Custom）、SQL_ESCAPE
   - DotNet 独有：泛型 `ExecuteScaler<T>`、Save/Load 存档复制、临时数据库自动清理
   - DotNet 缺失关键安全特性：无参数化查询（SQL 直接拼接，有注入风险）

2. **DICT vs MAP 对比**：
   - MAP 功能远超 DICT（18 vs 6 个函数）
   - DICT 存在哈希碰撞设计缺陷（字符串键通过 `GetHashCode()` 转换为 long）
   - DICT 缺失：RELEASE/REMOVE/CLEAR/SIZE/GETKEYS/VALUES/TOXML/FROMXML/MERGE/REMOVEIF/FINDKEY
   - DICT 无存档支持，MAP 通过 VarExt.csv 持久化
   - DICT 仅有的优势：值类型支持 long（MAP 仅支持 string）

3. **移植建议**：
   - DICT → LazyLoading：❌ 不建议移植（MAP 已更完整）
   - MAP → DotNet：✅ 建议移植（替代有缺陷的 DICT）
   - SQL 增强 → DotNet：✅ 建议参考 LazyLoading 版补充参数化查询和 XML 导入导出

**输出**：
- `emueradotnet-features.md` → v3.0（扩展 SQL/DICT 章节：架构特点、API 表、缺失功能清单、实现缺陷）
- `lazyloading-features.md` → v3.0（扩展 MAP 章节：架构特点、实现类层次、新增函数详解、对比表；扩展 SQL 章节：架构特点、ReaderContext、参数化查询、完整 24 函数 API、XML 导入导出详解、对比表）
- `emueradotnet-portability-assessment.md` → v5.0（重写第九章：DICT vs MAP 深度对比，含 6 项缺陷分析、16 维度对比表、优化建议）

---

### 2026-05-08

**工作目标**：完成两个项目的功能性差异对比审查

---

### 阶段一：项目结构探索

**时间**：09:00 - 10:00

**工作内容**：
- 探索 `emuera_lazyloading_selfmodified_version` 项目结构
- 探索 `EmueraDotNet` 项目结构
- 识别核心模块和文件组织差异

**发现**：
1. LazyLoading版包含额外模块：
   - `PluginSystem/` - 插件管理系统
   - `SoundTouch_byMarkHeath/` - 音频变速处理
   - `EvilMask/` - 自定义语言系统
   - `尊尼获加/` - SQL和BitArray工具
   - `Process.LazyLoading.cs` - 懒加载核心实现

2. EmueraDotNet的结构特点：
   - 独立的 `Dictionary/` 和 `SQL/` 模块
   - 函数模块下有 `Method/` 子目录
   - UI层有多语言resx文件支持

---

### 阶段二：核心代码分析

**时间**：10:00 - 12:00

**工作内容**：
- 分析两个项目的 `Process.cs` 文件
- 分析懒加载机制实现（`Process.LazyLoading.cs`）
- 分析ERB加载器差异

**发现**：
1. **初始化流程差异**：
   - LazyLoading版在初始化时加载插件系统
   - EmueraDotNet在初始化时设置SQL临时数据库

2. **ERB加载策略差异**：
   - LazyLoading版：支持懒加载模式，通过配置文件控制
   - EmueraDotNet：使用并行加载（`AsParallel().ForAll`）

3. **懒加载核心机制**：
   - 三个关键文件：`lazyloading.cfg`、`lazyloading.bin`、`lazyloadingfiles.bin`
   - 支持增量更新（检测文件变化）
   - 支持函数到文件的映射

---

### 阶段三：本地化方案对比

**时间**：14:00 - 15:00

**工作内容**：
- 对比两个项目的本地化方案
- 分析语言文件结构和访问方式

**发现**：
1. **LazyLoading版本地化**：
   - 使用XML文件存储语言数据
   - 通过静态类属性访问（如 `trsl.LoadingFile.Text`）
   - 支持中/日/英三语

2. **EmueraDotNet本地化**：
   - 使用标准resx资源文件
   - 通过 `LocalizationManager` 统一管理
   - 支持中日英三语

---

### 阶段四：功能特性对比

**时间**：15:00 - 17:00

**工作内容**：
- 对比预处理器扩展
- 分析函数参数解析差异
- 整理适用场景

**发现**：
1. **预处理器扩展**：
   - LazyLoading版支持 `[SKIPSTART]`、`[IF_DEBUG]`、`[IF_NDEBUG]`、`[IF]` 等指令
   - 支持文件加载顺序扩展（`*#*` 目录优先）
   - EmueraDotNet支持新随机数生成器配置

2. **函数参数解析**：
   - LazyLoading版支持 `VARIADIC` 可变参数
   - EmueraDotNet仅支持标准参数定义

---

### 阶段五：文档整理

**时间**：17:00 - 18:00

**工作内容**：
- 整理对比分析结果
- 创建知识库文档
- 编写适用场景建议

**输出**：
- `emuera-vs-emueradotnet-comparison.md` - 完整对比文档

---

## 关键发现总结

### 架构层面
| 特性 | LazyLoading版 | EmueraDotNet |
|------|-------------|--------------|
| 核心扩展 | 懒加载、插件系统 | 并行加载、多语言 |
| 代码组织 | 功能模块集中 | 模块化拆分 |
| 复杂度 | 较高 | 较低 |

### 性能层面
| 特性 | LazyLoading版 | EmueraDotNet |
|------|-------------|--------------|
| 启动策略 | 按需加载 | 并行加载 |
| 运行时性能 | 可能有加载延迟 | 流畅 |
| 内存占用 | 较低（按需加载） | 较高（全量加载） |

### 适用场景
| 场景 | 推荐版本 |
|------|---------|
| 大型ERB项目 | LazyLoading版 |
| 中小型项目 | EmueraDotNet |
| 需要插件扩展 | LazyLoading版 |
| 需要多语言支持 | EmueraDotNet |

---

### 阶段九：手册错误修正与知识库同步审查

**时间**：同日

**工作内容**：
- 交叉审查 portability-assessment.md、emueradotnet-features.md、lazyloading-features.md 三个文件的一致性
- 修正发现的错误和遗漏

**发现与修正**：

1. **portability-assessment.md §10.2 "Skia 采样选项" 行**：
   - 错误：`LazyLoading 版无 SkiaSharp`
   - 修正：`LazyLoading 版使用 SKFilterQuality（旧API），非 SKSamplingOptions`
   - 原因：文档自身已在 §2 明确声明两个项目均使用 SkiaSharp，此处自相矛盾

2. **emueradotnet-features.md §4 标题**：
   - 错误：`来源：DotNet 独有 | 纯 SkiaSharp 渲染引擎，无 GDI+ 依赖`
   - 修正：`DotNet 使用纯 SkiaSharp 渲染引擎，无 GDI+ 依赖。LazyLoading 版（skia变体）也使用 SkiaSharp 作为主渲染引擎，但保留 GDI+ 回退方案。`
   - 原因：原表述暗示 SkiaSharp 本身是 DotNet 独有，实际两个项目均使用 SkiaSharp

3. **lazyloading-features.md "LazyLoading 版缺失功能" 表**：
   - 遗漏：`GETCSVNOBY*` 名字反查功能未列入
   - 修正：补充 `GETCSVNOBY* 名字反查 | CSV 数据查询 | ✅ 低 | 按 NAME/NICKNAME/CALLNAME/MASTERNAME 反查角色编号`
   - 原因：emueradotnet-features.md 和 portability-assessment.md 均已记录此功能，lazyloading-features.md 遗漏

**交叉验证结果**：
- SkiaSharp 使用情况：三文件一致 ✅
- 颜色矩阵转换差异：三文件一致 ✅
- G_POLYGON 多边形绘制：三文件一致 ✅
- XXH3/XXH32 哈希：三文件一致 ✅
- GETCSVNOBY* 名字反查：三文件一致 ✅（已补全）
- DICT vs MAP 对比：三文件一致 ✅
- SQL 功能对比：三文件一致 ✅
- 懒加载/插件/音频/VARIADIC：三文件一致 ✅

**输出**：
- `emueradotnet-portability-assessment.md` → 修正 1 处
- `emueradotnet-features.md` → 修正 1 处
- `lazyloading-features.md` → 补充 1 处

---

### 阶段十：运行时差异深度审查

**时间**：同日

**工作内容**：
- 在 portability-assessment.md 概述中增加移植方向说明（仅 DotNet→LazyLoading，不反向）
- 逐文件对比两个变体的 `Process.Initialize()` 初始化管线
- 深度分析 VARIADIC 可变参数的解析、约束检查、运行时转换全链路
- 对比 ERB 加载策略（并行 vs 懒加载跳过）
- 对比 ERB 重载方法（DotNet 三种粒度 vs LazyLoading 单一方法）
- 更新知识库文件

**关键发现**：

1. **初始化管线差异**：
   - 两个变体均继承 emuera.em 上游的同一套初始化管线（Parser→Image→KeyMacro→Replace→Rename→GameBase→CSV→ERH→ERB→SystemProc）
   - LazyLoading 在 ERH 加载前注入 `PluginManager` 初始化，ERB 加载传入 `UseLazyLoading` 标志
   - DotNet 在 ERB 加载后调用 `SQL.SetUpTempDB()`，无插件系统、无懒加载

2. **ERB 加载策略差异**：
   - LazyLoading：顺序加载 + 懒加载跳过（`LazyLoadingFiles.Contains(file)` → `continue`）
   - DotNet：`AsParallel().ForAll()` 全量并行加载 + `ConcurrentQueue<string>` 线程安全日志

3. **ERB 重载方法差异**：
   - LazyLoading：`ReloadErb()` 全部重载 + `ReloadPartialErb(List<string>)` 按文件列表 + `ReloadFolder(string)` 按目录
   - DotNet：`ReloadErbAll()` / `ReloadPartialErb(List<string>)` / `ReloadErbFolder(string)` 三种粒度
   - 双方均支持细粒度重载，方法名略有不同

4. **VARIADIC 可变参数全链路**：
   - 解析阶段：`ErbLoader.parseLabel()` 扫描 VARIADIC 关键字 → 移除关键字保留被修饰参数
   - 约束检查：必须位于最后、只能修饰 ARG/ARGS/ARGF、不得与固定参数类型冲突
   - 运行时：`Process.CalledFunction.ConvertArg()` 将 variadicIndex 之后的实参打包为 `VariadicArgTerm`
   - 类型系统：`ArgType.Variadic = 1 << 8`，支持 VariadicAny/VariadicInt/VariadicString/VariadicSameAsFirst
   - DotNet 完全无 VARIADIC 支持（零匹配）

5. **移植判断**：
   - 并行加载 → 🔄 建议移植（可整合到懒加载的非跳过文件加载中）
   - SQL 临时数据库 → ❌ 不移植（LazyLoading 的 SqlManager 功能更完整）
   - 细粒度 ERB 重载 → 🔄 建议移植（开发调试价值）
   - VARIADIC/插件系统/懒加载 → LazyLoading 已有，无需移植

**输出**：
- `emueradotnet-portability-assessment.md` → v6.0（概述增加移植方向说明 + 第七章重写）
- `lazyloading-features.md` → v3.0（VARIADIC 章节扩展：解析/约束/运行时/类型系统全链路）
- `emueradotnet-features.md` → v4.0（并行加载章节扩展 + 新增 ERB 重载方法章节 + 章节重编号）

---

### 阶段十一：并行加载移植准备

**时间**：同日

**工作内容**：
- 深入对比 DotNet 和 LazyLoading 的 `ErbLoader.LoadErbDir()` 实现
- 确认 LazyLoading 版**已有**细粒度 ERB 重载（`ReloadPartialErb`/`ReloadFolder`），无需移植
- 编写并行加载移植执行手册

**关键发现**：

1. **细粒度 ERB 重载已存在**：
   - `Process.ReloadPartialErb(List<string>)` — [Process.cs:L251](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Process.cs#L251)
   - `EmueraConsole.ReloadPartialErb(List<string>)` — [EmueraConsole.cs:L2809](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/EmueraConsole.cs#L2809)
   - `EmueraConsole.ReloadFolder(string)` — [EmueraConsole.cs:L2847](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/UI/Game/EmueraConsole.cs#L2847)
   - `ErbLoader.LoadErbList(...)` — [ErbLoader.cs:L195](file:///d:/emuera/emuera_lazyloading_selfmodified_version/Emuera/Runtime/Script/Loader/ErbLoader.cs#L195)
   - 修正了 portability-assessment.md 和 emueradotnet-features.md 中的错误移植建议

2. **并行加载移植前置差异**：
   - LazyLoading 缺少 3 个 using：`System.Linq`、`System.Threading`、`System.Collections.Concurrent`
   - `loadErb()` 签名差异：LazyLoading 多一个 `bool isLazyLoading = false` 参数（有默认值，不影响调用）
   - LazyLoading 有 `*#*` 优先目录加载（EE 扩展），不可并行化
   - LazyLoading 有懒加载跳过逻辑，需在并行化前预过滤

3. **执行手册要点**：
   - 仅修改 1 个文件（`ErbLoader.cs`）
   - 2 处改动：添加 3 个 using + 改造主加载循环
   - 采用 LINQ `Where` 预过滤方案（而非 lambda 内判断）
   - 保留 `*#*` 优先目录的顺序加载语义
   - 风险点：`isOnlyEvent.Add()` 多线程写 `List<T>` 不安全，需在执行前验证

**输出**：
- `porting-manual-parallel-loading.md` → 新建（完整执行手册：差异分析 + 步骤 + 设计决策 + 风险 + 回滚）
- `emueradotnet-portability-assessment.md` → v6.1（修正细粒度 ERB 重载移植判断）
- `emueradotnet-features.md` → v4.1（修正 ERB 重载方法移植建议）

---

## 后续建议

1. **知识库更新**：定期更新对比文档，跟踪两个项目的版本演进
2. **迁移指南**：根据实际迁移需求，完善迁移步骤文档
3. **功能融合**：考虑将双方优势特性进行整合的可能性

---

**文档版本**：v3.1
**创建日期**：2026-05-08
**更新日期**：2026-05-09
**作者**：系统分析
**变更**：逐项审查修正 6 处错误：
1. 阶段三：语言支持 中英双语 → 中/日/英三语
2. 阶段八：SQL 函数数量 25 → 24（源码实际注册数）
3. 阶段八：MAP 函数数量 21 → 18（源码实际注册数）
4. 阶段六：移除 GSetFont/Brush/Pen 作为 DotNet 独有（双方都有）
5. 阶段十：ERB 重载方法 LazyLoading 仅 ReloadErb → 三种粒度都有
6. 全文 SQL/MAP 函数数量引用修正（共 4 处）