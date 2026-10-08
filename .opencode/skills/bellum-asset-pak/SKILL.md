---
name: bellum-asset-pak
description: Use when packing Bellum Aeternum client images from the GUI theme folder (e.g. D:\Bellum Aeternum v1.3.0\Resources\GUI) into the encrypted Resources\clientassets.pak, when deploying the pak-capable client build, or when theme images (campaign mission previews, buttons, backgrounds) are missing after loose PNGs were removed. Trigger on "pak", "clientassets.pak", "打包", "GUI 图片", "Preview 图不显示".
---

# Bellum Aeternum asset .pak

Packs theme images into a single AES-256 encrypted `.pak` (an uncompressed ZIP
whose entry contents are encrypted) that the client reads via `AssetLoader`.

## How asset loading works

- `Rampastring.XNAUI/AssetLoader.cs` searches `AssetSearchPaths` on disk first,
  then falls back to every registered `AssetPak`.
- The theme folder is `Resources\<ThemeFolder>` (`ClientDefinitions.ini`
  `[Themes]`, currently `GUI/`), added via `ProgramConstants.GetResourcePath()`.
- `DXMainClient/DXGUI/ClientAssetPak.cs` registers `Resources\clientassets.pak`
  using the embedded AES key `ClientAssetPak.KEY_BASE64`.
- The generator `Scripts/CreateAssetPak.ps1` MUST use the same key (its
  `-KeyBase64` default). Change one, change the other.

## Pack

Entries are stored relative to the folder passed as `-ResourcesPath`, so pass
the theme folder itself (`...\Resources\GUI`), not `Resources`.

```powershell
./Scripts/CreateAssetPak.ps1 `
  -ResourcesPath "D:\Bellum Aeternum v1.3.0\Resources\GUI" `
  -OutputPath    "D:\Bellum Aeternum v1.3.0\Resources\clientassets.pak" `
  -ManifestPath  ./Scripts/pak.manifest
```

- `Scripts/pak.manifest` lists paths/wildcards relative to `-ResourcesPath`
  (`*.png` packs all PNGs recursively). Edit it to narrow the set.
- Add `Audio/SE/*.wav` etc. only if the client loads those; images are the
  primary target.

## Deploy a pak-capable client (WindowsDX, .NET Framework)

The game launches `Resources\clientdx.exe` (.NET Framework 4.8) and loads
assemblies from `Resources\Binaries\Windows\` (see `DXMainClient/Program.cs`).
The .NET 8 variant lives in `Resources\BinariesNET8\Windows\`.

```powershell
# Build the net48 WindowsDX client from the repo root
dotnet build .\DXMainClient\DXMainClient.csproj -c WindowsDXRelease -f net48 --nologo

# Back up, then copy the two changed assemblies
$o = '.\DXMainClient\bin\Release\WindowsDX\net48'
Copy-Item "$o\clientdx.exe" "D:\Bellum Aeternum v1.3.0\Resources\clientdx.exe" -Force
Copy-Item "$o\Rampastring.XNAUI.WindowsDX.dll" "D:\Bellum Aeternum v1.3.0\Resources\Binaries\Windows\Rampastring.XNAUI.WindowsDX.dll" -Force
```

Also update `Resources\BinariesNET8\Windows\` (`clientdx.dll`,
`Rampastring.XNAUI.WindowsDX.dll`) from the `net8.0-windows` build if the NET8
client is used. Always back up the files being overwritten first.

## Verify

```powershell
# List entries without decrypting
$fs=[IO.File]::OpenRead('D:\Bellum Aeternum v1.3.0\Resources\clientassets.pak')
([IO.Compression.ZipArchive]::new($fs,[IO.Compression.ZipArchiveMode]::Read)).Entries |
  Select-Object -First 5 -ExpandProperty FullName
```

## Important notes

- The filesystem is searched **before** the pak. While loose PNGs remain in
  `Resources\GUI`, the pak is never read. Move them away to make the pak take
  effect.
- Encrypting a `.pak` only deters casual extraction: the key is embedded in
  the client and filenames remain visible inside the archive.
- The campaign mission preview is discovered via
  `AssetLoader.AssetDirectoryExists("Campaign/Preview")` and loaded through
  `AssetLoader`, so its images must exist inside the pak under
  `Campaign/Preview/` (the manifest's `*.png` includes them).
