# 图像资源管理函数集 ¶

## 概述

Emuera 的图像资源管理系统已重构为基于 AppContents + SQLite 的统一架构。所有图像资源（无论来自 CSV 预加载还是运行时创建）都通过同一套 LRU 机制管理内存。


## 核心概念

### 图像资源分类

| 类型 | 来源 | LRU 管理 | 说明 |
|:-----|:-----|:---------|:-----|
| **CSV 预加载** | ContentDir/*.csv | ✅ 是 | 启动时索引，按需加载 |
| **SPRITECREATEFROMFILE** | 运行时指定文件 | ✅ 是 | 自动识别动图/拼接图 |
| **GCREATE/GCREATEFROMFILE** | G_ID 指定 | ❌ 否 | 独立 GraphicsImage，不归 LRU 管 |

### LRU 机制

```
MAX_LRU = 800 个文件

┌──────────────────────────────────────────────────────┐
│ fileLruCache (key = FilePath)                        │
│                                                      │
│ "char.png" → {                                      │
│   Bitmap: SKBitmap,     ← 只加载一次                  │
│   SpriteNames: [SPRITE_A, SPRITE_B]               │
│ }                                                    │
└──────────────────────────────────────────────────────┘
```

- 同一图像文件的多个 sprite 共享同一个 Bitmap
- 当 LRU 超过容量时，淘汰最久未使用的文件
- 淘汰时释放 Bitmap，sprite 索引保留在 SQLite

---

## 函数手册

### 1. SPRITECREATEFROMFILE

**功能**: 从图像文件创建 Sprite，自动识别动图和拼接图

**语法**:
```
SPRITECREATEFROMFILE "SPRITENAME", "filename"[, isRelative]
```

| 参数 | 类型 | 说明 |
|:-----|:-----|:-----|
| SPRITENAME | string | Sprite 名称（自动转为大写） |
| filename | string | 图像文件路径 |
| isRelative | int | 0=相对于 ContentDir，1=相对路径（可选） |

**返回值**: 成功返回 1，失败返回 0

**自动识别**:
- **多帧动图** (WebP/GIF): 自动创建 SpriteAnime，每帧 Copy() 为独立 ConstImage
- **拼接大图**: 加载后由 SpriteF 管理矩形区域
- **普通图像**: 创建 SpriteF

**LRU 集成**: ✅ 是
- 调用 `AppContents.CreateSpriteFromFileDynamic()`
- 图像文件进入 LRU 管理
- 复用已缓存的 Bitmap

**示例**:
```erb
; 从 ContentDir 加载
SPRITECREATEFROMFILE "CHARA_001", "char/001.png", 0

; 从相对路径加载
SPRITECREATEFROMFILE "CHARA_002", "char/002.png", 1

; 加载动图
SPRITECREATEFROMFILE "ANIME_001", "effects/explosion.webp", 0
```

---

### 2. GCREATEFROMFILE

**功能**: 从文件加载图像到 Graphics (G_ID)

**语法**:
```
GCREATEFROMFILE gID, "filename"[, isRelative]
```

| 参数 | 类型 | 说明 |
|:-----|:-----|:-----|
| gID | int | Graphics ID (0-) |
| filename | string | 图像文件路径 |
| isRelative | int | 0=相对于 ContentDir，1=相对路径（可选） |

**返回值**: 成功返回 1，失败返回 0

**特点**:
- ❌ **不归 LRU 管理**
- ❌ **不自动识别动图**（只加载第一帧）
- 需手动 GDISPOSE 释放

**示例**:
```erb
GCREATEFROMFILE 1000, "texture.png", 0
IF RESULT == 1
    DRAWG 1000
ENDIF
```

---

### 3. GCREATE

**功能**: 创建空白 Graphics 或从已有 Graphics 创建子区域

**语法**:
```
; 空白 Graphics
GCREATE gID, width, height

; 从已有 Graphics 创建
GCREATE gID, parentGID, x, y, width, height

; 带目标尺寸缩放
GCREATE gID, parentGID, x, y, width, height, destWidth, destHeight
```

**参数**:
| 参数 | 类型 | 说明 |
|:-----|:-----|:-----|
| gID | int | 新 Graphics ID |
| parentGID | int | 父 Graphics ID |
| x, y | int | 源矩形左上角 |
| width, height | int | 源矩形尺寸 |
| destWidth, destHeight | int | 目标尺寸（可选） |

**返回值**: 成功返回 1，失败返回 0

**特点**:
- ❌ **不归 LRU 管理**
- 创建的是引用而非复制，父释放则子无效

---

### 4. GDISPOSE

**功能**: 释放 Graphics 内存

**语法**:
```
GDISPOSE gID
```

**返回值**: 成功返回 1，不存在返回 0

**示例**:
```erb
GDISPOSE 1000
```

---

### 5. SPRITECREATE

**功能**: 从 Graphics 创建 Sprite

**语法**:
```
; 基本创建
SPRITECREATE "spriteName", gID

; 指定截取区域
SPRITECREATE "spriteName", gID, x, y, width, height

; 带偏移和目标尺寸
SPRITECREATE "spriteName", gID, x, y, width, height, posX, posY, destWidth, destHeight
```

**参数**:
| 参数 | 类型 | 说明 |
|:-----|:-----|:----- |
| spriteName | string | Sprite 名称 |
| gID | int | Graphics ID |
| x, y, width, height | int | 截取区域 |
| posX, posY | int | 偏移位置 |
| destWidth, destHeight | int | 目标尺寸 |

**LRU 集成**: ✅ 是（如果源 Graphics 来自 LRU 管理文件）
- Sprite 关联到源文件的 LRU 条目

**示例**:
```erb
GCREATEFROMFILE 1000, "char.png", 0
SPRITECREATE "FACE_001", 1000, 0, 0, 100, 100
```

---

### 6. SPRITEDISPOSE

**功能**: 释放 Sprite（保留索引）

**语法**:
```
SPRITEDISPOSE "spriteName"
```

**返回值**: 成功返回 1，不存在返回 0

**说明**:
- 释放内存中的 Bitmap
- SQLite 索引保留
- 下次访问时自动重新加载

---

### 7. SPRITEDISPOSEALL

**功能**: 释放所有 Sprite

**语法**:
```
SPRITEDISPOSEALL deleteCsvImage
```

| 参数 | 类型 | 说明 |
|:-----|:-----|:----- |
| deleteCsvImage | int | 0=保留 CSV 索引，1=删除索引 |

**返回值**: 释放的 Sprite 数量

---

## SQL 资源查询

### SQL_RESOURCE_EXIST

**功能**: 检查 Sprite 是否存在（不加载）

**语法**:
```
SQL_RESOURCE_EXIST "spriteName"
```

**返回值**: 1=存在，0=不存在

**说明**: 仅查询 SQLite 索引，不触发加载

**示例**:
```erb
SQL_RESOURCE_EXIST "CHARA_001"
IF RESULT == 1
    PRINTW 索引中存在此人设
ENDIF
```

---

## 行为对比

| 函数 | 动图识别 | 多帧展开 | LRU 管理 | 内存释放 |
|:-----|:--------:|:--------:|:--------:|:---------:|
| CSV 预加载 | ✅ | ✅ | ✅ | 自动 |
| SPRITECREATEFROMFILE | ✅ | ✅ | ✅ | 自动 |
| SPRITECREATE | ❌ | ❌ | ✅* | 自动 |
| GCREATEFROMFILE | ❌ | ❌ | ❌ | 手动 |
| GCREATE | ❌ | ❌ | ❌ | 手动 |

*如果源 Graphics 来自 LRU 管理的文件

---

## 内存管理建议

### 推荐模式

```erb
; 1. 检查是否存在
SQL_RESOURCE_EXIST "CHARA_001"
IF RESULT == 0
    PRINTW 资源不存在
    RETURN
ENDIF

; 2. 直接使用（自动加载）
DRAWCHARA "CHARA_001"

; 3. 不需要手动释放
; LRU 会自动管理内存
```

### 不推荐模式

```erb
; ❌ 手动管理内存（不必要且容易出错）
GCREATEFROMFILE 1000, "char.png"
SPRITECREATE "CHARA_001", 1000
; ... 使用 ...
GDISPOSE 1000  ; 需要手动释放
SPRITEDISPOSE "CHARA_001"  ; 需要手动释放
```

### 何时使用 Graphics (G_ID)

- 需要直接操作像素
- 需要用 GDRAW 等低级绘图函数
- 需要跨帧复用临时缓冲区

### 何时使用 Sprite

- 绝大多数情况
- 角色立绘、特效、背景等
- DRAWCHARA / SPRITEDRAW 等高级渲染

---

## 附录: LRU 配置

```csharp
// AppContents.cs
private static readonly int MAX_LRU_CAPACITY = 800;  // 最大缓存文件数
```

调整此值可平衡内存占用和性能。

---

## 旧版 RM_* 函数（已废弃）

以下函数已在重构后移除，请使用新的 SQL 资源函数：

| 旧函数 | 替代方案 |
|:-------|:---------|
| RM_RESOURCECHECK_LOAD | 直接使用 Sprite 渲染函数（自动加载） |
| RM_RESOURCE_EXIST | SQL_RESOURCE_EXIST |
| RM_RELEASE_ALL | SPRITEDISPOSEALL |
