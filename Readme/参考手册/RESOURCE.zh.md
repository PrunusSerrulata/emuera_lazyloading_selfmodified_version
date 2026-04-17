# 资源管理函数集 (ResourceManager) ¶

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| **RM_RESOURCECHECK_LOAD** | string resourceName | int |
| **RM_RESOURCE_EXIST** | string resourceName | int |
| **RM_RELEASE_ALL** | (无) | int |

### API

1. `int RM_RESOURCECHECK_LOAD(resourceName)`
2. `int RM_RESOURCE_EXIST(resourceName)`
3. `int RM_RELEASE_ALL()`

提供高性能的图像与动画资源按需加载与自动缓存管理功能。

**函数说明：**

1. **RM_RESOURCECHECK_LOAD** - 检查并加载资源
   - `resourceName`: 资源标识符（对应 `resources.idx` 中的名称）
   - 如果资源已在内存中，则更新其 LRU 热度并返回 1。
   - 如果资源未加载，则根据索引元数据从磁盘加载，并分配内部 `G_ID`。
   - 返回 1 表示资源可用（已加载或加载成功），0 表示资源不存在或加载失败。
   - **特性**：触发 LRU 检查，若内存中 Sprite 数量超过 200，将自动释放最久未使用的资源。

2. **RM_RESOURCE_EXIST** - 检查资源是否存在
   - `resourceName`: 资源标识符
   - 仅检查资源是否在内存中或存在于二进制索引中，不触发实际加载。
   - 返回 1 表示资源存在，0 表示不存在。

3. **RM_RELEASE_ALL** - 手动释放所有资源
   - 强制清空资源管理器管理的所有 Sprite 和 Graphic 资源。
   - 总是返回 1。
   - **注意**：游戏重置（Reset）时会自动调用此函数。

### 核心机制

- **二进制索引 (resources.idx)**：
  - 程序启动或首次调用时加载。
  - 存储了所有资源的源文件路径、切割参数、动画帧序列等元数据。
  - 相比传统的 `_Rename.csv`，大幅提升了海量小文件的检索速度。

- **LRU 缓存淘汰**：
  - 系统维护一个最近使用列表。
  - 默认容量为 200 个 Sprite。
  - 旨在解决大型游戏中图像资源过多导致的内存溢出问题。

- **引用计数管理**：
  - 多个 Sprite 可以安全地共享同一个底层磁盘图像（Graphic）。
  - 只有当所有依赖该图像的 Sprite 都被释放时，底层图像资源才会从内存中卸载。

### Hint

- 资源文件应存放在程序根目录下的 `resources/` 文件夹内。
- 建议在显示立绘、背景图前调用 `RM_RESOURCECHECK_LOAD`。
- 如果需要精细控制内存，可以在转场或大段剧情结束时调用 `RM_RELEASE_ALL`。

### Example

**MAIN.ERB**
```erb
@SHOW_CHARA_IMAGE(CHARA_ID)
    #DIMS RESOURCE_NAME
    RESOURCE_NAME = %"CHARA_{CHARA_ID}_NORMAL"%
    
    ; 1. 尝试加载资源
    IF RM_RESOURCECHECK_LOAD(RESOURCE_NAME)
        ; 2. 加载成功后，可以像普通 Sprite 一样使用
        PRINT_SPRITE RESOURCE_NAME
    ELSE
        PRINTFORML [错误] 找不到资源: %RESOURCE_NAME%
    ENDIF
```
