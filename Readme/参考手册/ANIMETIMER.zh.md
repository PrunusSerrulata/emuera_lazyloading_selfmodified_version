# SETANIMETIMER, GETANIMETIMER ¶

| 函数名 | 参数 | 返回值 |
| :--- | :--- | :--- |
| **SETANIMETIMER** | int time | 命令专用。总是返回1 |
| **int GETANIMETIMER** | 无 | 命令/表达式。返回当前动画定时器间隔（毫秒） |

### API

``` { #language-erbapi }
SETANIMETIMER timeNum
int GETANIMETIMER
```

控制动画精灵在输入等待期间的重绘间隔。

### Hint

!!! hint "Hint"

    **SETANIMETIMER 为命令专用，GETANIMETIMER 为命令/表达式。**

    SETANIMETIMER 语法：
    ```
    SETANIMETIMER 100
    ```

    GETANIMETIMER 语法：
    ```
    GETANIMETIMER
    LOCAL = GETANIMETIMER()
    ```

* `SETANIMETIMER`：命令专用。设置动画精灵的重绘间隔（毫秒）。
  * 正常情况下，Emuera 在 [`INPUT`](INPUT.md) 等输入等待期间不会重绘。
  * 通过此命令设置重绘间隔，可以在 `INPUT` 等输入等待期间保持动画播放。
  * 注意：在 [`TINPUT`](TINPUT.md) 等具有超时处理的命令中不会执行重绘。
  * 由于计算机性能限制，实际绘制间隔会略慢于设定值。因此如果将绘制间隔设置为与动画的 `delay` 相同，会导致频繁丢帧。建议设置明显小于 `delay` 的间隔。
  * 此命令独立于 config 中的"Frames per second"设置。
  * 此命令不受 [`REDRAW`](REDRAW.md) 命令的重绘抑制效果影响。
  * 参数范围：int.MinValue ~ 32767（毫秒）
  * 作为表达式时返回 1

* `GETANIMETIMER`：命令/表达式。返回当前设置的动画定时器间隔（毫秒）。
