# ee+em/master 上游更新审阅报告

> **审阅范围**: `a9216fc71ee1132888eb687b6fb95659be942caf` → `60f5272a6f547b5980d07e61af2cf5f31af11a54` (HEAD)
> **上游仓库**: https://gitlab.com/EvilMask/emuera.em
> **审阅日期**: 2026-05-05
> **总计提交**: 34 个 (含合并提交)
> **同步日期**: 2026-05-05

---

## 一、审阅范围

| 分段 | 起止 | 提交数 | 说明 |
|------|------|--------|------|
| 前半 | `a9216fc` → `792d38f` | 14 个非合并 | 私家版v23 基线 |
| 后半 | `792d38f` → `60f5272` | 20 个（含合并） | 私家版v24 + 后续修复 |

---

## 二、更新概要

### 前半 (a9216fc → 792d38f)

| # | Commit | 描述 | 审阅结果 |
|---|--------|------|----------|
| A1 | `6afed01` | fix:ボタンの当たり判定を改善 | ✅ 已同步 |
| A2 | `fd15691` | feat:ログ出力時にバリアント情報も出力 | ✅ 已同步 |
| A3 | `69ef192` | update:EEv53 | ⬜ 版本号，跳过 |
| A4 | `cc0cd11` | fix:CurrentCultureをInvariantCultureに設定 | ✅ **本次同步** |
| A5 | `8095c43` | Enable NO array access from plugin | ✅ 已同步 |
| A6 | `3121d10` | fix:SPRITEGETCOLORの返り値修正 | ✅ 已同步 |
| A7 | `4406917` | fix:BINPUTSで常に0を通す不具合修正 | ✅ 已同步 |
| A8 | `86aa06b` | Add GetStackTrace() in PluginManager | ✅ 已同步 |
| A9 | `6b87aee` | merge:リソース読み込みエラーログ出力 | ⬌ 不适用（SkiaSharp重写） |
| A10 | `d72c0e4` | merge:VSCode対策プラグイン記述 | ⬌ 不适用（csproj结构不同） |
| A11 | `72e54b6` | update:libwebp.dll更新 | ⬜ 二进制，跳过 |
| A12 | `2175173` | fix:リソース読み込みエラー対策 | ✅ 已同步 |
| A13 | `ade50ea` | fix:リソース読み込み処理修正 | ✅ 已同步 |
| A14 | `d9d024a` | fix:TIMESのカルチャー依存問題修正 | ✅ **本次同步** |

### 后半 (792d38f → 60f5272)

| # | Commit | 描述 | 审阅结果 |
|---|--------|------|----------|
| 1 | `adbcb03` | merge:私家版v24 | ✅ 已同步 |
| 2 | `1615943` | fix:divのxposの不具合修正 | ✅ 已同步 |
| 3 | `e464355` | fix:バージョンにコミットハッシュ付与 | ⬜ 可选，跳过 |
| 4 | `4dcafa1` | feat:ONEBINPUT、ONEBINPUTS追加 | ✅ 已同步 |
| 5 | `5476c5c` | feat:-genlang别名対応 | ✅ 已同步 |
| 6 | `7bd957f` | fix:ERD二次元配列文字列型変数参照不具合 | ✅ **本次同步** |
| 7 | `5d74d53` | dotNET 10にアップデート | ⏭️ 跳过（不升级框架） |
| 8 | `cde11d6` | バージョン名修正 | ⬜ 可选，跳过 |
| 9 | `e9082fa` | fix:ExeDir起動パス修正+SoundDir追加 | ⏭️ 跳过（不涉及绝对路径） |
| 10 | `ca51056` | feat:ServerGC追加、WorkingDir撤廃 | ⚠️ ServerGC **本次同步**，WorkingDir跳过 |
| 11 | `bc1eed4` | fix:ツールチップ表示処理修正 | ✅ 已同步 |
| 12 | `a4d3665` | fix:ENUMFILES相対パス修正 | ⏭️ 跳过（不涉及绝对路径） |
| 13 | `1c495b5` | fix:ENUMFILES挙動修正+ツールチップ位置 | ⏭️ 跳过（不涉及绝对路径） |
| 14 | `302f1b7` | fix:ツールチップ表示処理再調整 | ✅ 已同步 |
| 15 | `64443a6` | fix:ツールチップ表示時間例外修正 | ✅ 已同步 |
| 16 | `25c23dc` | fix:ツールチップ遅延クリック判定消失対処 | ✅ 已同步 |
| 17 | `84a2e5e` | bugfix (PrintPlainWithSingleLine) | ✅ 已同步 (c9e9519) |
| 18 | `e3c5bb4` | fix:FORCE_QUITが機能してなかったのを修正 | ✅ **本次同步** |
| 19 | `844f646` | fix:div border+radius併用描画修正 | ✅ 已同步 |
| 20 | `60f5272` | Merge pluginmanager fix (同#17) | ✅ 同#17 |

---

## 三、最终同步方案（已执行）

共同步 **5 项**变更，涉及 **5 个文件**：

| # | 上游 Commit | 描述 | 文件 | 改动 |
|---|-----------|------|------|------|
| 1 | `cc0cd11` | CurrentCulture → InvariantCulture 全局设置 | Program.cs | +3行 |
| 2 | `d9d024a` | TIMES double.Parse 加 CultureInfo.InvariantCulture | LexicalAnalyzer.cs | 1行修改 |
| 3 | `7bd957f` | VARS2D 二维字符串数组引用修复 | ConstantData.cs | 1行修改 |
| 4 | `e3c5bb4` | FORCE_QUIT 非重启场景添加 Application.Exit() | EmueraConsole.cs | +2行 |
| 5 | `ca51056` | ServerGC 启用 | *.csproj | +1行 |

### 同步详情

#### 1. CurrentCulture 全局设置 (`cc0cd11`)

**桌面端**: `Emuera/Program.cs` — Main() 开头添加
**m-emuera**: `src/MEmuera/MauiProgram.cs` — CreateMauiApp() 开头添加

```csharp
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
```

#### 2. TIMES 文化依赖修复 (`d9d024a`)

**双端**: `Runtime/Script/Parser/LexicalAnalyzer.cs`

```csharp
// 修改前
return double.Parse(st.SubstringROS(start, st.CurrentPosition - start));
// 修改后
return double.Parse(st.SubstringROS(start, st.CurrentPosition - start), CultureInfo.InvariantCulture);
```

#### 3. VARS2D 修复 (`7bd957f`)

**双端**: `Runtime/Script/Data/ConstantData.cs`

```csharp
// 修改前
if (code == VariableCode.VAR2D && index == 0 || code == VariableCode.CVAR2D && index == 1)
// 修改后
if ((code == VariableCode.VAR2D || code == VariableCode.VARS2D) && index == 0 || code == VariableCode.CVAR2D && index == 1)
```

#### 4. FORCE_QUIT 修复 (`e3c5bb4`)

**双端**: `UI/Game/EmueraConsole.cs` (桌面) / `GameView/EmueraConsole.cs` (m-emuera)

```csharp
if (Program.rebootFlag)
    window.Reboot();
else
    Application.Exit();
```

#### 5. ServerGC 启用 (`ca51056`)

**桌面端**: `Emuera/Emuera.csproj`
**m-emuera**: `src/MEmuera/MEmuera.csproj`

```xml
<ServerGarbageCollection>true</ServerGarbageCollection>
```

---

## 四、跳过留档

以下变更经审阅后决定不同步，留档备查：

| # | Commit | 描述 | 跳过原因 |
|---|--------|------|----------|
| A3 | `69ef192` | EEv53 版本号更新 | 版本号自行管理 |
| A9 | `6b87aee` | ConstImage 资源加载错误日志 | 用户已用 SkiaSharp 完全重写 ConstImage |
| A10 | `d72c0e4` | VSCode対策 NAudio Choose重构 | 用户 csproj 结构不同 |
| A11 | `72e54b6` | libwebp.dll 更新 | 二进制文件，自行管理 |
| 3 | `e464355` | 版本号加 commit hash | 可选功能 |
| 7 | `5d74d53` | .NET 10 升级 | 不升级框架版本 |
| 8 | `cde11d6` | 版本名修正 | 可选 |
| 9 | `e9082fa` | ExeDir 绝对路径统一 + SoundDir | 不涉及绝对路径修改 |
| 10 | `ca51056` | WorkingDir撤廃（部分） | WorkingDir 仍被多处使用 |
| 12 | `a4d3665` | ENUMFILES 相对路径修正 | 不涉及绝对路径修改 |
| 13 | `1c495b5` | ENUMFILES 相对路径基准修正 | 不涉及绝对路径修改 |

---

## 五、Git 提交信息

### 桌面端 (`emuera_lazyloading_selfmodified_version`)

```
fix: 上游同步 — CurrentCulture/TIMES文化依赖/VARS2D/FORCE_QUIT/ServerGC

同步 ee+em/master 的 5 项修复 (a9216fc..60f5272):

- cc0cd11: CurrentCulture を InvariantCulture に設定 (Program.cs)
- d9d024a: TIMES のカルチャー依存問題を修正 (LexicalAnalyzer.cs)
- 7bd957f: ERB二次元配列の文字列型変数参照不具合を修正 (ConstantData.cs)
- e3c5bb4: FORCE_QUIT が機能していなかったのを修正 (EmueraConsole.cs)
- ca51056: ServerGC を有効化 (Emuera.csproj)
```

### m-emuera (`m-emuera`)

```
fix: 上游同步 — CurrentCulture/TIMES文化依赖/VARS2D/FORCE_QUIT/ServerGC

同步 ee+em/master 的 5 项修复 (a9216fc..60f5272):

- cc0cd11: CurrentCulture を InvariantCulture に設定 (MauiProgram.cs)
- d9d024a: TIMES のカルチャー依存問題を修正 (LexicalAnalyzer.cs)
- 7bd957f: ERB二次元配列の文字列型変数参照不具合を修正 (ConstantData.cs)
- e3c5bb4: FORCE_QUIT が機能していなかったのを修正 (EmueraConsole.cs)
- ca51056: ServerGC を有効化 (MEmuera.csproj)
```
