#Requires -Version 7.0
<#
.SYNOPSIS
    Creates an encrypted asset .pak file for the CnCNet client.

.DESCRIPTION
    Packs the image files listed in a manifest into a single .pak file.
    The .pak is a standard (uncompressed) ZIP archive where the content of every
    entry is encrypted with AES-256-CBC. The 16-byte initialization vector (IV)
    is stored as a prefix of the entry content, followed by the ciphertext.

    The AES key MUST match ClientAssetPak.KEY_BASE64 in
    DXMainClient/DXGUI/ClientAssetPak.cs.

.PARAMETER ResourcesPath
    The path of the Resources directory that contains the assets.

.PARAMETER OutputPath
    The path of the .pak file to create.

.PARAMETER ManifestPath
    The path of the manifest file that lists the assets to include. Each
    non-empty, non-comment line is a path or wildcard relative to ResourcesPath.
    Wildcards are matched recursively. Defaults to Scripts/pak.manifest.

.PARAMETER KeyBase64
    The base64-encoded 32-byte AES-256 key. Defaults to the key embedded in the client.

.EXAMPLE
    ./CreateAssetPak.ps1 -ResourcesPath ../Compiled/Resources -OutputPath ../Compiled/Resources/clientassets.pak
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourcesPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$ManifestPath = (Join-Path $PSScriptRoot 'pak.manifest'),

    [string]$KeyBase64 = 'arU5uT8l1LEDPTrab1qegVAluXwh2zxd1Y33kwQ9dTw='
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resourcesFullPath = (Resolve-Path -LiteralPath $ResourcesPath).Path
if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Manifest not found: $ManifestPath"
}

$key = [Convert]::FromBase64String($KeyBase64)
if ($key.Length -ne 32) {
    throw "The AES key must be 32 bytes (256 bits), got $($key.Length) bytes."
}

$patterns = Get-Content -LiteralPath $ManifestPath |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_ -ne '' -and -not $_.StartsWith('#') }

$files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
$seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

foreach ($pattern in $patterns) {
    $target = Join-Path $resourcesFullPath $pattern

    if (Test-Path -LiteralPath $target -PathType Leaf) {
        $matches = @(Get-Item -LiteralPath $target)
    }
    else {
        $matches = @(Get-ChildItem -Path $target -File -Recurse -ErrorAction SilentlyContinue)
    }

    if ($matches.Count -eq 0) {
        Write-Warning "Pattern matched no files: $pattern"
    }

    foreach ($file in $matches) {
        if ($seen.Add($file.FullName)) {
            $files.Add($file)
        }
    }
}

if ($files.Count -eq 0) {
    throw "No files matched the manifest. Nothing to pack."
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFullPath)
if (-not [string]::IsNullOrEmpty($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Force
}

$rootWithSeparator = $resourcesFullPath.TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar

$fileStream = [System.IO.File]::Open(
    $outputFullPath,
    [System.IO.FileMode]::CreateNew,
    [System.IO.FileAccess]::Write,
    [System.IO.FileShare]::None)

try {
    $zipArchive = [System.IO.Compression.ZipArchive]::new(
        $fileStream,
        [System.IO.Compression.ZipArchiveMode]::Create,
        $false)

    try {
        foreach ($file in $files) {
            $relativePath = $file.FullName.Substring($rootWithSeparator.Length).Replace('\', '/')
            $entry = $zipArchive.CreateEntry($relativePath, [System.IO.Compression.CompressionLevel]::NoCompression)

            $plainBytes = [System.IO.File]::ReadAllBytes($file.FullName)

            $aes = [System.Security.Cryptography.Aes]::Create()
            try {
                $aes.Key = $key
                $aes.GenerateIV()

                $encryptor = $aes.CreateEncryptor()
                try {
                    $cipherBytes = $encryptor.TransformFinalBlock($plainBytes, 0, $plainBytes.Length)
                }
                finally {
                    $encryptor.Dispose()
                }

                $entryStream = $entry.Open()
                try {
                    $entryStream.Write($aes.IV, 0, $aes.IV.Length)
                    $entryStream.Write($cipherBytes, 0, $cipherBytes.Length)
                }
                finally {
                    $entryStream.Dispose()
                }
            }
            finally {
                $aes.Dispose()
            }
        }
    }
    finally {
        $zipArchive.Dispose()
    }
}
finally {
    $fileStream.Dispose()
}

Write-Host "Created $outputFullPath with $($files.Count) entries."
