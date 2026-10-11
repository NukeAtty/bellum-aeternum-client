# AGENTS.md

本文件给在这个仓库里工作的 AI agent / 开发者提供快速上下文与约定。内容来自项目结构与维护过程中总结的经验，**改动这些约定前请先确认**。

## 项目概述

- 这是 **Bellum Aeternum** 的 CnCNet 客户端，基于 [CnCNet/xna-cncnet-client](https://github.com/CnCNet/xna-cncnet-client)（DTA/TSC 系客户端）的 fork。
- 技术栈：C# / .NET（同时支持 .NET Framework 4.8 与 .NET 8.0），渲染用 MonoGame（DX11/OpenGL）或 XNA，UI 框架为 `Rampastring.XNAUI`。
- 仓库里包含**主题资源模板**；实际运行的游戏在部署目录里。

## 部署与"该改哪里"（重要）

- 实际运行的游戏目录：`D:\Bellum Aeternum v1.3.0\`。
  - 运行时 INI、贴图、`clientassets.pak` 都在 `D:\Bellum Aeternum v1.3.0\Resources\`。
  - **改布局/主题优先直接改这里**（或用仓库资源改好后部署过去）。
- 仓库中的主题资源源文件在 `DXMainClient/Resources/DTA/`（以及 `Default Theme/`）。
- 只改 INI / 贴图一般**无需重新编译**，重启客户端即可生效；改 C# 才需要重新构建。
- 资源查找优先级：运行时先读主题目录（`ProgramConstants.GetResourcePath()` = `Resources\<主题>\`），再读基础目录（`GetBaseResourcePath()` = `Resources`）。改错副本会导致"改了没效果"。
- 图片资产可用加密 pak：见下节 skill。

## 构建 / 运行

- 需要 **.NET SDK 10.0** 与 **PowerShell 7**；编辑代码建议 Visual Studio 2026+ 或 Rider 2025.3+。
- 克隆后先初始化子模块：`git submodule update --init --recursive`（`Rampastring.XNAUI` 是 fork：`NukeAtty/Rampastring.XNAUI`）。
- 命令行构建：`Scripts\Build.bat`（输出到 `Compiled\`）。发布时把 `Compiled\` 内容拷到目标游戏的 `Resources\`。
- IDE 调试：打开解决方案 `DXClient.slnx`，F5。
- 打包 GUI 图片到 `clientassets.pak`：`Scripts\CreateAssetPak.ps1`（清单 `Scripts\pak.manifest`）。
- 排查构建异常：`Scripts\ClearObjDirs.bat` / `ClearBinAndObjDirs.bat`；切换配置后建议重启 IDE。

## 仓库结构

- `DXMainClient/` — 主客户端（`DXGUI` 窗口/UI、`Domain` 玩法与数据、`Online` IRC/网络）。
- `ClientCore/` — 配置、统计、路径/工具。
- `ClientGUI/` — 控件与 INI 解析核心（`XNAWindow`、`INItializableWindow`、`ClientGUICreator` 等）。
- `Rampastring.XNAUI/` — UI 框架（git submodule）。
- `ClientUpdater/`、`SecondStageUpdater/`、`TranslationNotifierGenerator/` — 更新器与翻译工具。
- `DXMainClient/Resources/DTA/` — 主题资源（INI、贴图、字体、音频）。
- `Docs/` — 关键文档：`INISystem.md`（INI 表达式/常量）、`Migration-INI.md`（新窗口格式迁移）、`Build.md`。
- `Scripts/` — 构建与打包脚本。

## UI / INI 架构（务必先读）

项目里有**两套窗口系统**，改 INI 前必须分清：

- **XNAWindow（旧）** — 例如普通大厅 `CnCNetLobby`、`LANLobby`。
  - 读 `{WindowName}.ini`；只认普通 `[Section]`/`key`。
  - 支持 `Location/Size/Width/Height/X/Y`、`DistanceFromRightBorder/DistanceFromBottomBorder`、`FillWidth/FillHeight`、`Visible/Enabled`、`DrawOrder/UpdateOrder`、`RemapColor/Text` 等。
  - **不支持** `$X`、`getWidth()` 等表达式；窗口同名 section 不存在时回退到 `[GenericWindow]`。
  - 额外控件用 `[ExtraControls]`（**无** `$`），如 `L1=btnX:XNALinkButton`。
- **INItializableWindow（新）** — 例如游戏大厅 `GameLobbyBase`（`SkirmishLobby` / `MultiplayerGameLobby`）。
  - `$CCxx=name:Type` 定义子控件；`$X/$Y/$Width/$Height/$TextAnchor/$AnchorPoint` 由 `Parser` 求值，常量来自 `[ParserConstants]`。
  - `$BaseSection=其它段` 复制该段缺失的键；额外控件用 `[$ExtraControls]`（**带** `$`）。
  - 只读**与窗口名相同**的 section，**不会**回退 `[GenericWindow]`。

继承与查找：

- `[INISystem] BasedOn=<file>` 做合并，子文件覆盖父文件；支持 `$THEME_DIR$` 前缀。
- 窗口按 `Name` 选择 INI 文件与 section。游戏大厅窗口名：单机 = `SkirmishLobby`，多人（LAN/CnCNet）= `MultiplayerGameLobby`。

易踩的坑（都实际遇到过）：

1. **控件名大小写**：代码用 `FindChild(nameof(field), StringComparison.Ordinal)`（**区分大小写**）。INI 里的控件名必须与 C# 字段名 `nameof` 完全一致，否则 `FindChild` 返回 `null`，表现为"控件显示出来但点击没反应"。例：`btnSaveLoadGameOptions`（不能写成 `BtnSaveLoadGameOptions`）。
2. **`DrawBorders` 只管控件自身**，不管子控件。`XNAPanel` 及其派生（`XNAMultiColumnListBox`、`XNAListBox`、`MapPreviewBox` 等）默认 `DrawBorders=true`，会以主题 `PanelBorderColor` 画边框；窗口级 `DrawBorders=false` 关不掉子面板。多列列表的列标题是代码里 `new XNAPanel(...)` 创建、**没有 Name**，INI 无法单独关闭。
3. **`DistanceFromRightBorder/DistanceFromBottomBorder` 用解析到该键那一刻的 `Width/Height`**，而 `Size`/`IdleTexture` 会改尺寸但保留 `X/Y`。因此**键顺序很重要**：想让边距基于最终尺寸计算，尺寸类键要放在距离类键**前面**。
4. **控件的创建时机**：需被 INI 配置的控件必须在 `base.Initialize()` 之前创建并命名（INI 在 `Initialize()` 中解析）。
5. **共享段里别放某类大厅专属控件**：游戏大厅的聊天控件 `lbChatMessages` / `tbChatInput` 只属于多人，应放在 `[MultiplayerGameLobby]`，**不要**放 `[GameLobbyBase]`；否则单机大厅也会创建它们，未定位的零宽 `XNATextBox` 会在左上角画出一条竖线（见下条）。
6. `[GameLobbyBase]`（窗口段）与 `GameLobbyBase.ini`（基类布局）是两回事；`SkirmishLobby.ini` 用 `[SkirmishLobby] $BaseSection=GameLobbyBase` 复制键。

主题颜色 / 常量：

- 颜色在部署的 `GlobalThemeSettings.ini`（仓库对应 `Default Theme/DTACnCNetClient.ini`）的 `[General]`：如 `PanelBorderColor`、`WindowBorderColor`、`UILabelColor` 等。
- 布局常量在 `[ParserConstants]`：如 `LOBBY_PADDING_*`、`LOBBY_PANEL_SPACING`、`GAME_OPTION_DD_*` 等；缺失会让新格式大厅启动报错。
- `UIDesignConstants`（C#）里有按钮尺寸等常量。

## 代码约定

- 遵循现有代码风格；控件/图表命名与 INI 联动时用 `nameof` 保持一致。
- 新增可在 INI 中配置的控件时，在 `base.Initialize()` 之前创建。
- 不要在未明确要求时提交（commit）。
- 修 bug 时先判断是**配置(INI/资源)**问题还是**代码**问题：很多"显示/交互异常"其实来自 INI 名不匹配、段放错、键顺序或主题/基础目录加载优先级。

## 图片 / INI 打包（加密 .pak）

主题 GUI 图片**和客户端侧 INI** 可打包进单个加密 `.pak`（本质是**未压缩 zip**，每个条目内容用 AES-256-CBC 加密、16 字节 IV 前置；zip 内文件名仍为明文）。图片/音频经 `Rampastring.XNAUI/AssetLoader.cs` 在磁盘搜索路径之后回退读取；INI 经 `IniFile` 的 packed-content 机制读取。

- **生成**：`Scripts\CreateAssetPak.ps1`，清单 `Scripts\pak.manifest`（相对 `-ResourcesPath` 的路径/通配符）。pak 命名空间 = **主题目录**（`Resources\GUI`），所以根目录 INI 要以**扁平条目名**（`GameLobbyBase.ini`、`GameOptions.ini`…）打入。打包根目录 INI 需先**暂存**：把主题目录内容 + 待打包 INI 复制进一个临时目录再打包（详见 skill）。
- **密钥**：内嵌在 `DXMainClient/DXGUI/ClientAssetPak.cs` 的 `KEY_BASE64`，必须与脚本 `-KeyBase64` 默认值一致；改一处要改另一处。属**混淆级**加密，防普通提取，防不住逆向。
- **INI 支持机制（代码）**：
  - `Rampastring.XNAUI/Rampastring.Tools/IniFile.cs` 新增 `PackedContentProvider` 钩子与 `FileExists`。磁盘文件优先，缺失时才问 provider。写回（`WriteIniFile`）始终明文。
  - `ClientAssetPak.RegisterEarly()`（由 `PreStartup.Initialize` 在读取任何客户端配置前调用）打开独立 `AssetPak` 并注册 INI provider，因此**早于 `AssetLoader` 初始化**也能读打包的 INI；`ClientAssetPak.Register()`（`GameClass.Initialize`）复用同一实例注册给 `AssetLoader`。
  - 窗口 INI 查找走 `ClientGUI/IniFileResolver.cs`（供 `INItializableWindow`、`XNAWindow` 使用）：主题目录→基础目录，均先磁盘后 pak。
  - `FileHashCalculator.CalculateSHA1ForFile` 也回退 pak，因此打包后的 `GameOptions.ini` 哈希与散文件一致。
- **可打包（仅客户端读）**：主题 `DTACnCNetClient.ini`、`GlobalThemeSettings.ini`；所有窗口 INI（`*Lobby.ini`、`*Window*.ini`、`MainMenu.ini`、`GenericWindow.ini`、`CampaignSelector.ini`、`KeyboardCommands.ini` 等）；`GameOptions.ini`。
- **禁止打包**（引擎/更新器直接读，或 provider 就绪前就被读，或被原始哈希）：`ClientDefinitions.ini`、`Renderers.ini`、`Fonts.ini`、`GameCollectionConfig.ini`、`FHCConfig.ini`、`UpdaterConfig.ini`、`spawn*.ini`、`ra2md.ini`、`Resources\Configs\*.ini`、翻译 INI。
- **部署**：游戏实际运行的是 **.NET Framework WindowsDX** 客户端 `Resources\clientdx.exe`，依赖从 `Resources\Binaries\Windows\` 加载。特征改动涉及 `clientdx.exe`/`clientdx.dll`、`Binaries*\*\ClientCore.dll`、`ClientGUI.dll`、`Rampastring.XNAUI.<engine>.dll`。推荐 `Scripts\Build.ps1` 后 robocopy `Compiled\Resources` 到 `Resources`。
- **生效前提（重要）**：文件系统优先，散文件存在时 pak 不会被读取；要让它生效需移走对应散文件（散图 + 打包过的 INI）。
- 依赖目录判定的功能（如任务 Preview 用 `AssetLoader.AssetDirectoryExists("Campaign/Preview")`）也走 AssetLoader，因此 pak 内需包含对应目录（`*.png` 清单已涵盖）。

## 相关 Skill

- `.opencode/skills/bellum-asset-pak/SKILL.md`：上述打包/部署流程的完整 skill。当需要重新打包 `Resources\GUI` 图片、部署支持 pak 的客户端，或出现"散图已删、任务 Preview 等图不显示"时使用（可用 `skill` 工具加载 `bellum-asset-pak`）。
