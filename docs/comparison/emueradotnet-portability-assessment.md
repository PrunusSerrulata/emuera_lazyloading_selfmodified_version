# EmueraDotNet → LazyLoading 移植进度

## 概述

本文档追踪 EmueraDotNet 中"人有我无"或"人优我劣"的特性向 LazyLoading（skia变体）的单向移植进度。

**移植方向**：DotNet → LazyLoading 单向 pick 式移植。LazyLoading 已有的功能（懒加载、MAP增强、SQL增强、插件系统、音频处理、VARIADIC、SETIMAGELAYER 等）不作为移植目标。

**状态标记**：
- ✅ 已完成 | ⏳ 待处理 | ❌ 否决 | ➖ 舍弃

---

## 移植进度表

### 第一阶段：核心功能与稳定性（v3.4.0）

| 功能 | 状态 | 变更记录 |
|------|------|---------|
| **GETCSVNOBY* 名字反查** | ✅ | 2026-05-09：新增 GETCSVNOBYNAME/NICKNAME/CALLNAME/MASTERNAME 4 个表达式函数，通过 Dictionary<string,long> 实现 O(1) 反查 |
| **MATCHALL / MATCHALLEX** | ✅ | 2026-05-09：重构为比 DotNet 指令形式更灵活的"表达式函数"形式，返回匹配计数，第五参数输出索引数组 |
| **TOINT 边界修复** | ✅ | 2026-05-09：增加 try-catch 拦截非法字符串转换崩溃，无法解析时返回 0 |
| **METHOD_Instruction Float 分支** | ✅ | 2026-05-09：补全 Float 表达式函数（TOFLOAT 等 8 个）用作命令时的 EraType.Float 分支，写入 RESULTF |
| **Preload 字节级优化** | ✅ | 2026-05-09：采用 .NET 8 内存流方案，解决编码兼容性与 BOM 剥离问题。ConstantData.cs 中 2 处 eReader.Open() 改为 OpenOnCache() 充分利用缓存 |
| **MainWindow null 检查** | ✅ | 2026-05-09：ShowConfigDialog 和剪贴板处理器加 console null 检查，防止引擎未初始化时崩溃 |
| **PrintStringBuffer 空检查** | ✅ | 2026-05-09：Flush() 中 ButtonsToDisplayLines 返回空数组时跳过 ret[^1] 访问 |
| **SKPaint using 资源释放** | ✅ | 2026-05-09：Creator.Method.cs L7122 补全 `using var`，防止非托管内存泄漏 |

### 第二阶段：图形渲染增强

| 功能 | 状态 | 变更记录 |
|------|------|---------|
| **图像负尺寸翻转** | ⏳ | DotNet 在 ASpriteSingle.GraphicsDraw 中检测 destRect 宽高符号，负值时 SKCanvas.Scale 翻转 |
| **G_POLYGON 多边形** | ✅ | 2026-05-10：移植 GDrawPolygon/GFillPolygon/GDrawPolygonAddPoint/GDrawPolygonClearPoint 4 个指令 |
| **下划线/删除线** | ✅ | 2026-05-10：补全 ConsoleStyledString 的 HasUnderline/HasStrikeout 渲染 |
| **SKPaint 过时 API** | ✅ | <br>2026-05-09：移除 ConsoleStyledString.cs 和 ConsoleImagePart.cs 中 3 处 `SKPaint.TextAlign` 属性赋值（SKTextAlign.Left 为默认值，移除后无功能变化），消除 3 个 CS0618 警告 |

### 第三阶段：底层架构优化

| 功能 | 状态 | 变更记录 |
|------|------|---------|
| **Stopwatch 高精度计时** | ⏳ | DotNet 用 Stopwatch.GetTimestamp() 替代 DateTime.Now，精度 15ms→100ns，不受系统时间修改影响 |
| **并行加载 (AsParallel)** | ❌ 否决 | 线程安全风险过高：loadErb() 内部 labelDic 和 isOnlyEvent 非线程安全，需大量加锁改造。LazyLoading 的懒加载跳过机制已提供启动加速，并行加载收益有限且与懒加载语义冲突 |
| **SQL_CONNECTION_OPEN** | ✅ | 2026-05-10：新增 SqlManager.ConnectionOpen + SqlConnectionOpenMethod，自动定位 sav/sql/ 目录 |
| **SQL 泛型重构** | ✅ | 2026-05-10：ExecuteScalarLong/String/Float 合并为 ExecuteScalar<T>，ERB 层 API 不变 |

### 舍弃项（不移植）

| 功能 | 状态 | 原因 |
|------|------|------|
| **DICT_* 字典函数** | ➖ | LazyLoading 的 MAP 系列功能更强大（18 vs 6 函数），DICT 存在哈希碰撞设计缺陷 |
| **XXH3 / XXH32 哈希** | ➖ | 引擎目前不需要额外哈希工具函数 |
| **SKImage 渲染路径** | ➖ | 需改造 AbstractImage 基类及所有子类，风险高；Emuera 2D 渲染负载低，GPU 缓存收益微乎其微 |
| **SKSamplingOptions** | ➖ | 受限于 DrawBitmap API 支持，暂维持现有 SKFilterQuality 方案 |
| **Rename 重构** | ➖ | 仅代码风格差异，现有实现已稳定 |
| **DisplayMode 绝对定位** | ➖ | 职责分离：图片绝对定位用 SETIMAGELAYER，HTML UI 锚定用 HTML_PRINT_ISLAND |

---

## DotNet 版本认知更新

### DotNet 独有功能（已评估）

| 功能 | 说明 | 移植建议 |
|------|------|---------|
| **LocalizationManager + resx** | 标准 .NET 本地化方案 | ➖ LazyLoading 使用 EvilMask/Lang 自定义方案，已支持中日英 |
| **JSON 配置 (setting.json)** | 纯 JSON 配置，无 XML | ⏳ 可选：引入 System.Text.Json 读取，与现有 XML 配置并存 |
| **ConcurrentQueue 日志** | 并行加载时的线程安全日志 | ❌ 随并行加载一同否决 |

### DotNet 与 LazyLoading 的架构差异

| 维度 | LazyLoading | DotNet |
|------|------------|--------|
| 渲染引擎 | SkiaSharp + GDI+ 回退 | SkiaSharp 纯引擎 |
| 渲染后端 | OpenGL/CPU 双后端 + 自动回退 | SKGLControl 仅 OpenGL |
| 图片基类型 | SKBitmap（单一） | SKImage + SKBitmap（双类型） |
| 颜色矩阵 | 行主序 + offset×255 | 列主序（转置）+ offset 原值 |
| 动画计时 | DateTime.Now + 暂停/恢复 + 动图 LRU | Stopwatch + 无暂停/恢复 |
| 图片翻转 | 不支持负尺寸 | 支持 SKCanvas.Scale |
| 多边形绘制 | 无 | G_POLYGON 系列 |
| 图片映射 | srcm（MappingGraphName） | 无 |
| HTML img cm | 完整支持 | 无 |
| ERB 加载 | 顺序 + 懒加载跳过 | 并行全量加载 |
| SQL | 24 函数 + 参数化查询 + XML | 9 函数 + 泛型标量查询 |
| 字典 | MAP 18 函数 | DICT 6 函数（有哈希碰撞缺陷） |

---

## 综合项目进度

| 阶段 | 任务 | 优先级 | 进度 |
|------|------|--------|------|
| **第一阶段 (v3.4.0)** | 核心反查、搜索函数、稳定性修复、Preload 优化 | P0 | 100% |
| **第二阶段 (渲染)** | 图像翻转、多边形绘制、文字装饰线、过时 API、DisplayMode | P1 | 60% |
| **第三阶段 (底层)** | Stopwatch 计时、SQL 便利函数、SQL 泛型重构 | P1 | 33% |
| **第四阶段 (配置)** | JSON 配置可选支持 | P2 | 0% |

---

## 待办移植任务清单

### 图形系统 (P1)

- [x] **Stopwatch 计时重构**：修改 SpriteAnime/SpriteAnimated 和 EmueraConsole.cs，使用 Stopwatch.GetTimestamp() 替代 DateTime.Now
- [x] **图像翻转逻辑**：在 ASpriteSingle.GraphicsDraw 中加入 canvas.Scale(sx, sy) 逻辑
- [x] **HTML DisplayMode 属性移植**：img/div 标签支持 display 属性（relative/absolute-lefttop/absolute-leftbottom），ConsoleImagePart DrawTo 绝对定位
- [x] **多边形指令集**：移植 G_POLYGON 相关 4 个指令及 GraphicsImage.cs 顶点管理逻辑
- [x] **装饰线渲染**：在 ConsoleStyledString.cs 中实现 HasUnderline 与 HasStrikeout
- [x] **过时 API 修复**：清理 3 处 SKPaint.TextAlign 警告

### 底层架构 (P1)

- [ ] **SQL 便利性函数**：在 SqlManager.cs 中新增 SQL_CONNECTION_OPEN，自动映射路径
- [ ] **SQL 泛型重构**：引入内部泛型方法，合并 Long/String/Float 标量查询代码

### 配置与杂项 (P2)

- [ ] **JSON 配置支持**：引入 System.Text.Json，实现对 setting.json 的读取支持，与现有 XML 配置并存

---

## 版本历史

- v7.3：G_POLYGON 多边形指令集、HasUnderline/HasStrikeout 装饰线渲染完成（第二阶段 80%，第三阶段 33%）
- v7.2：Stopwatch 计时重构、图像翻转逻辑、HTML DisplayMode 属性移植完成（第二阶段 60%，第三阶段 33%）
- v7.1：SKPaint.TextAlign 过时 API 修复完成（状态从 ⏳ 升 ✅），第二阶段进度更新至 25%
- v6.2：逐项审查修正 10 处错误
- v6.1：修正细粒度 ERB 重载移植判断
- v6.0：概述增加移植方向说明 + 第七章重写
- v5.0：重写第九章 DICT vs MAP 深度对比
- v4.0：整合为十二章 + 目录页
- v3.0：重写第一章 SkiaSharp 实现对比
