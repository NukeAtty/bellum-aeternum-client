---
name: bellum-asset-pak
description: Use when packing Bellum Aeternum client assets (theme images and client-side INI files such as window layouts, the theme INI and GameOptions.ini) into the encrypted Resources\clientassets.pak, when deploying the pak-capable client build, or when theme images/INIs are missing after loose files were removed. Trigger on "pak", "clientassets.pak", "打包", "GUI 图片", "INI 加密", "加密 INI", "Preview 图不显示".
---

# Bellum Aeternum asset .pak

Packs theme images **and client-side INI files** into a single AES-256 encrypted
`.pak` (an uncompressed ZIP whose entry contents are encrypted). Images/sounds are
read via `AssetLoader`; INIs are read via `IniFile`'s packed-content provider.

## How asset loading works

- `Rampastring.XNAUI/AssetLoader.cs` searches `AssetSearchPaths` on disk first,
  then falls back to every registered `AssetPak`.
- The theme folder is `Resources\<ThemeFolder>` (`ClientDefinitions.ini`
  `[Themes]`, currently `GUI/`), added via `ProgramConstants.GetResourcePath()`.
- `DXMainClient/DXGUI/ClientAssetPak.cs` opens `Resources\clientassets.pak`
  using the embedded AES key `ClientAssetPak.KEY_BASE64`.
- The generator `Scripts/CreateAssetPak.ps1` MUST use the same key (its
  `-KeyBase64` default). Change one, change the other.

## How packed INIs work

- `IniFile` (`Rampastring.XNAUI/Rampastring.Tools/IniFile.cs`) gained a static
  `PackedContentProvider` hook and a `FileExists` helper. When an INI file is
  **not** on disk, the provider is asked for its bytes. The file system always
  wins, so loose files override the pak.
- `ClientAssetPak.RegisterEarly()` wires the provider. It is called from
  `PreStartup.Initialize` (before any client configuration is read) and opens a
  standalone `AssetPak`, so packed INIs work even before `AssetLoader` exists.
- `ClientAssetPak.Register()` (called from `GameClass.Initialize`) reuses that
  same `AssetPak` and registers it with `AssetLoader` for images/sounds.
- Window lookup goes through `ClientGUI/IniFileResolver.cs` (used by
  `INItializableWindow` and `XNAWindow`), which checks theme dir → base dir,
  disk before pak.
- `FileHashCalculator.CalculateSHA1ForFile` also falls back to the pak, so a
  packed `GameOptions.ini` is still hashed identically to a loose one.

**Pak namespace = the theme folder.** Entries are stored as if they lived in
`Resources\<ThemeFolder>`. Therefore root INIs (`Resources\*.ini`) are packed
with flat names (`GameLobbyBase.ini`, `GameOptions.ini`, ...) so they resolve as
theme-relative entries.

## What to pack

Pack only INIs that are **read by the client** (not by the game engine/updater):

- Theme: `DTACnCNetClient.ini` (in the theme folder), `GlobalThemeSettings.ini`
- Window layouts: `MainMenu.ini`, `GenericWindow.ini`, `OptionsWindow.ini`,
  `GameLobbyBase.ini`, `SkirmishLobby.ini`, `MultiplayerGameLobby.ini`,
  `CnCNetGameLobby.ini`, `CnCNetLobby.ini`, `CnCNetLoginWindow.ini`,
  `LANGameLobby.ini`, `LANLobby.ini`, `GameCreationWindow.ini`,
  `GameCreationWindow_Advanced.ini`, `GameLoadingLobby.ini`,
  `GameLoadingWindow.ini`, `LoadingScreen.ini`, `StatisticsWindow.ini`,
  `PrivateMessagingWindow.ini`, `PrivacyNotification.ini`,
  `ManualUpdateQueryWindow.ini`, `UpdateQueryWindow.ini`,
  `CampaignSelector.ini`, `KeyboardCommands.ini`
- `GameOptions.ini`

**Never pack** (the game engine/spawner/updater reads them directly, or the
client reads them before the provider is available, or they are hashed raw):
`ClientDefinitions.ini`, `Renderers.ini`, `Fonts.ini`, `GameCollectionConfig.ini`,
`FHCConfig.ini`, `UpdaterConfig.ini`, `spawn.ini`, `spawnSG.ini`, `spawnmap.ini`,
`ra2md.ini`, `Resources\Configs\*.ini`, translation INIs.

## Pack (staging workflow)

Because the pak namespace is the theme folder but root INIs live in `Resources`,
stage a folder that contains the theme contents plus the INIs to pack, then pack
from the staging folder:

```powershell
$res   = "D:\Bellum Aeternum v1.3.0\Resources"
$stage = Join-Path $env:TEMP "bellum_pak_stage"
Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stage | Out-Null

# 1) theme folder contents (images + DTACnCNetClient.ini)
Copy-Item -Path (Join-Path $res 'GUI\*') -Destination $stage -Recurse -Force

# 2) client-only INIs, flattened into the pak root namespace
$inis = @(
  'CampaignSelector.ini','CnCNetGameLobby.ini','CnCNetLobby.ini','CnCNetLoginWindow.ini',
  'GameCreationWindow.ini','GameCreationWindow_Advanced.ini','GameLoadingLobby.ini',
  'GameLoadingWindow.ini','GameLobbyBase.ini','GameOptions.ini','GenericWindow.ini',
  'GlobalThemeSettings.ini','KeyboardCommands.ini','LANGameLobby.ini','LANLobby.ini',
  'LoadingScreen.ini','MainMenu.ini','ManualUpdateQueryWindow.ini','MultiplayerGameLobby.ini',
  'OptionsWindow.ini','PrivacyNotification.ini','PrivateMessagingWindow.ini',
  'SkirmishLobby.ini','StatisticsWindow.ini','UpdateQueryWindow.ini'
)
foreach ($f in $inis) { Copy-Item -LiteralPath (Join-Path $res $f) -Destination $stage -Force }

# 3) pack
./Scripts/CreateAssetPak.ps1 `
  -ResourcesPath $stage `
  -OutputPath    (Join-Path $res 'clientassets.pak') `
  -ManifestPath  ./Scripts/pak.manifest
```

`Scripts/pak.manifest` lists `*.png` plus the INI file names above.

## Deploy a pak-capable client (WindowsDX, .NET Framework)

The game launches `Resources\clientdx.exe` (.NET Framework 4.8) and loads
assemblies from `Resources\Binaries\Windows\`. The .NET 8 variant lives in
`Resources\BinariesNET8\Windows\`. A full `Scripts\Build.ps1` run is easiest:

```powershell
./Scripts/Build.ps1
robocopy ".\Compiled\Resources" "D:\Bellum Aeternum v1.3.0\Resources" /E /NP /NDL /NJS /NJH
```

The pak/INI feature touches: `clientdx.exe`/`clientdx.dll`,
`Binaries*\<engine>\ClientCore.dll`, `ClientGUI.dll`, and
`Rampastring.XNAUI.<engine>.dll`.

## Verify

```powershell
# List entries without decrypting
$fs=[IO.File]::OpenRead('D:\Bellum Aeternum v1.3.0\Resources\clientassets.pak')
([IO.Compression.ZipArchive]::new($fs,[IO.Compression.ZipArchiveMode]::Read)).Entries |
  Select-Object -ExpandProperty FullName |
  Where-Object { $_ -like '*.ini' } | Sort-Object
```

## Important notes

- The filesystem is searched **before** the pak. While loose files remain, the
  pak is never read. Move them away to make the pak take effect (that is the
  intended test: move the packed INIs out of `Resources` and relaunch).
- Encrypting a `.pak` only deters casual extraction: the key is embedded in the
  client and filenames remain visible inside the archive.
- The campaign mission preview is discovered via
  `AssetLoader.AssetDirectoryExists("Campaign/Preview")`, so its images must
  exist inside the pak under `Campaign/Preview/` (the manifest's `*.png` covers
  them).
- `clientassets.pak` is opened for the whole session (read share). Do not try to
  overwrite it while the client runs.
