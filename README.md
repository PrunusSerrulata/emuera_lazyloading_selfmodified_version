# Emuera-SKIA

> **画蛇又添足版** — 基于 Emuera 1.824 + EMv18 + EEv53 + Lazyloading v2.4 私家改造。

[![Base](https://img.shields.io/badge/base-Emuera%201.824%20%2B%20EEv53-blue)](#)
[![Lang](https://img.shields.io/badge/lang-C%23%20%2F%20ERABASIC-green)](#)
[![Render](https://img.shields.io/badge/render-SkiaSharp%20%2B%20OpenGL-orange)](#)
[![Status](https://img.shields.io/badge/status-active%20development-brightgreen)](#)

---

## 核心亮点

| 类别 | 亮点 |
|:---|:---|
| 🎨 **渲染引擎** | SkiaSharp 替代 GDI+，OpenGL 硬件加速，SRGB 色彩空间修复 |
| 📐 **浮点类型** | `#DIMF` / `#FUNCTIONF` / `LOCALF` / `ARGF` / `RESULTF`，三角函数与端数处理 |
| 🧠 **运行时** | ExecutionContext 栈式上下文 + SparseArray 稀疏存储 + SafeArithmetic 溢出保护 |
| 📝 **新指令** | EVAL/EVALS 动态求值、CALLSTR 动态调度、HTML_PRINTC 像素制表、SQL 全套、MAP 增强 |
| 🔧 **语法扩展** | 可变参数 `...` / `ARGLEN`、`#REF`/`#REFS` 引用声明、`OUT` 输出参数 |
| 🐛 **Bug 修复** | 懒加载 EXISTFUNCTION、.als 指针、SPRITECREATE、SpriteG 快照、字体泄漏等 20+ 项 |

---

## 快速开始

1. 将 `Emuera.exe` 放置于游戏目录
2. 配置 `lazyloading.cfg`（可选，详见 [懒加载说明](Readme/参考手册/懒加载LazyLoading.zh.md)）
3. 启动引擎 — ERB 脚本无需任何修改

---

## 文档导航

| 文档 | 说明 |
|:---|:---|
| [CHANGELOG.md](CHANGELOG.md) | 版本更新日志（Release Notes） |
| [Readme/参考手册/](Readme/参考手册/) | 各功能模块详细参考手册 |
| [Readme/画蛇又添足版自改emuera相关说明.txt](Readme/画蛇又添足版自改emuera相关说明.txt) | 原始开发日志（历史参考） |

---

## 致谢

| 项目 | 来源 |
|:---|:---|
| Emuera | [Emuera EE](https://gitlab.com/EvilMask/emuera.em) |
| Lazyloading | 지나가던 |
| SkiaSharp | Google / .NET Foundation |
| SoundTouch | [markheath/naudio](https://github.com/naudio/varispeed-sample) |

---

> 版本 V3
