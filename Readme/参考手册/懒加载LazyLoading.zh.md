# 懒加载 (Lazy Loading) 功能说明

> 本文档原为项目 README.md，现转移至参考手册目录。

---

## 概述

本 Emuera 变体集成了由 지나가던 开发的懒加载代码，基于 [Emuera EE](https://gitlab.com/EvilMask/emuera.em) 移植（commit 87dc1c0e）。

## 使用方法

引擎需要一个配置文件 `lazyloading.cfg` 来指定哪些文件需要懒加载。

配置文件格式：每行一个目录路径（相对于游戏 ERB 文件夹），包含需要懒加载的文件。目录分隔符可使用 Windows 风格 `\` 或 Unix 风格 `/`。

### 示例

`lazyloading.cfg`：
```
口上
RPG\依頼
RPG\闘技場
RPG\ダンジョンアタック\ダンジョンデータ
```

引擎会根据配置文件创建懒加载索引表（`lazyloading.dat`），以加速后续的游戏加载。

### 注意事项

**如果在懒加载文件中新增了函数，必须删除 `lazyloading.dat` 并重新运行引擎以重建索引，否则新函数将无法被识别。**

---

> 原始文档日期：2026-04（迁移自 README.md）
