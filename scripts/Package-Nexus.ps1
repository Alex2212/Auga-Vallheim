param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
Push-Location $repoPath
try {
    # Validate the runtime, API contract and example before assembling upload files.
    & (Join-Path $PSScriptRoot 'Build-AugaApi.ps1') -Package
    $dotnetPath = Join-Path $repoPath '.build/dotnet/dotnet.exe'
    & $dotnetPath build Auga/Auga.csproj -c Release "/p:ValheimDir=$gamePath"
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }

    $versionMatch = [regex]::Match((Get-Content Auga/Auga.cs -Raw), 'public const string Version = "([^"]+)"')
    $apiMatch = [regex]::Match((Get-Content Auga/API.cs -Raw), 'GetApiVersion\(\)\s*=>\s*"([^"]+)"')
    if (-not $versionMatch.Success -or -not $apiMatch.Success) { throw 'Cannot determine mod/API versions.' }
    $version = $versionMatch.Groups[1].Value
    $apiVersion = $apiMatch.Groups[1].Value
    $output = Join-Path $repoPath '.build/nexus'
    $stage = Join-Path $output 'mod'
    $plugin = Join-Path $stage 'BepInEx/plugins/Auga'
    New-Item -ItemType Directory -Force -Path $plugin,(Join-Path $stage 'docs') | Out-Null
    Copy-Item -LiteralPath Auga/bin/Release/Auga.dll,Auga/translations.json -Destination $plugin -Force
    Copy-Item -LiteralPath docs/UI-SETTINGS.md -Destination (Join-Path $stage 'docs') -Force
    @"
# Auga - Unofficial Valheim 1.0 Port

Mod version: $version

An independently maintained port of Project Auga. Not affiliated with or endorsed by the official Auga maintainers.
Original Project Auga credits: RandyKnapp, n4, Vapok, and contributors.

## Installation

1. Install BepInExPack for Valheim and launch the game once.
2. Close Valheim and disable/remove every other Auga installation.
3. Extract this archive into your Valheim game directory, merging its BepInEx folder.
4. Check that BepInEx/plugins/Auga contains Auga.dll and translations.json.
5. Launch Valheim. Settings > Accessibility > Scroll Speed defaults to 10x.

Developed against Valheim 1.0.12. Other UI replacement mods may conflict.
See [UI settings](docs/UI-SETTINGS.md) for appearance and accessibility details.

## For modders

Download the separate AugaAPI-SDK-$apiVersion.zip for AugaAPI.dll, bridge source,
API reference, modding guide, and example project. The SDK is a development
download, not an additional plugin that players need to install.
"@ | Set-Content -LiteralPath (Join-Path $stage 'README.md') -Encoding UTF8

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    Add-Type -AssemblyName System.IO.Compression
    $manifest = @('BepInEx/plugins/Auga/Auga.dll', 'BepInEx/plugins/Auga/translations.json', 'README.md', 'docs/UI-SETTINGS.md')
    $archive = Join-Path $output "Auga-Unofficial-Valheim-1.0-$version.zip"
    $stream = [IO.File]::Open($archive, [IO.FileMode]::Create)
    $zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $manifest) {
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $stage $entry), $entry)
        }
    } finally { $zip.Dispose(); $stream.Dispose() }

    # Read back the archive: only the explicitly approved distributable files belong here.
    $check = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        if (Compare-Object $manifest @($check.Entries | ForEach-Object FullName)) { throw 'Nexus archive manifest mismatch.' }
        foreach ($entry in $check.Entries) { if ($entry.Length -eq 0) { throw "Empty archive entry: $($entry.FullName)" } }
    } finally { $check.Dispose() }
    $sdk = Join-Path $output "AugaAPI-SDK-$apiVersion.zip"
    Copy-Item -LiteralPath (Join-Path $repoPath ".build/AugaAPI-SDK-$apiVersion.zip") -Destination $sdk -Force
    @(
        '# Nexus upload files', '',
        "Main file: Auga-Unofficial-Valheim-1.0-$version.zip", '',
        "Optional developer file: AugaAPI-SDK-$apiVersion.zip", '',
        'Upload these ZIPs, not the staging directory. Packaging does not publish or deploy anything.'
    ) | Set-Content -LiteralPath (Join-Path $output 'README.md') -Encoding UTF8
    Get-FileHash -Algorithm SHA256 -LiteralPath $archive,$sdk |
        Select-Object @{Name='File';Expression={Split-Path $_.Path -Leaf}},Hash |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'SHA256.json') -Encoding UTF8
    Write-Host "Nexus main file: $archive"
    Write-Host "Nexus optional API SDK: $sdk"
} finally { Pop-Location }
