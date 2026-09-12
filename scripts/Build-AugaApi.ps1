param([switch]$Package)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
$dotnetPath = Join-Path $repoPath '.build/dotnet/dotnet.exe'
$env:DOTNET_CLI_HOME = Join-Path $repoPath '.build/dotnet-home'
Push-Location $repoPath
try {
    & python scripts/api/generate.py --check
    if ($LASTEXITCODE -ne 0) { throw 'Generated API files are stale.' }
    foreach ($build in @(@('Auga/Auga.csproj','Debug'), @('Auga/Auga.csproj','API'), @('AugaApiExample/AugaApiExample.csproj','Debug'), @('tests/ApiBridge/Runtime/Runtime.csproj','Debug'), @('tests/ApiBridge/ApiBridge.csproj','Debug'))) {
        & $dotnetPath build $build[0] -c $build[1] "/p:ValheimDir=$gamePath"
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $($build[0]) $($build[1])" }
    }
    & (Join-Path $PSScriptRoot 'Test-AugaApi.ps1')
    & $dotnetPath tests/ApiBridge/bin/Debug/net8.0/ApiBridge.dll tests/ApiBridge/Runtime/bin/Debug/net8.0/Auga.dll
    if ($LASTEXITCODE -ne 0) { throw 'API bridge checks failed.' }
    if ($Package) {
        $output = Join-Path $repoPath '.build/AugaAPI-SDK'
        foreach ($directory in 'Auga','docs','AugaApiExample/Properties','lib') {
            New-Item -ItemType Directory -Force -Path (Join-Path $output $directory) | Out-Null
        }
        foreach ($name in 'API.External.cs','API.Bridge.cs','API.Common.cs','API.Palette.cs') {
            Copy-Item -LiteralPath (Join-Path $repoPath "Auga/$name") -Destination (Join-Path $output 'Auga') -Force
        }
        Copy-Item -LiteralPath docs/MODDING.md,docs/API-REFERENCE.md,docs/LEGACY-API.md -Destination (Join-Path $output 'docs') -Force
        Copy-Item -LiteralPath AugaApiExample/AugaApiExample.cs,AugaApiExample/AugaApiExample.csproj -Destination (Join-Path $output 'AugaApiExample') -Force
        Copy-Item -LiteralPath AugaApiExample/Properties/AssemblyInfo.cs -Destination (Join-Path $output 'AugaApiExample/Properties') -Force
        Copy-Item -LiteralPath Auga/bin/API/AugaAPI.dll -Destination (Join-Path $output 'lib') -Force
        Copy-Item -LiteralPath scripts/api/SDK.Directory.Build.props -Destination (Join-Path $output 'Directory.Build.props') -Force
        Set-Content -LiteralPath (Join-Path $output 'README.md') -Value '# Auga API SDK 2.0.0', '', 'Start with [the modding guide](docs/MODDING.md). This archive is an SDK, not a game plugin.'
        $archive = Join-Path $repoPath '.build/AugaAPI-SDK-2.0.0.zip'
        # Package an explicit manifest so previous example builds cannot leak bin/obj into the SDK.
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        Add-Type -AssemblyName System.IO.Compression
        $stream = [IO.File]::Open($archive, [IO.FileMode]::Create)
        $zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($entry in 'Auga/API.External.cs','Auga/API.Bridge.cs','Auga/API.Common.cs','Auga/API.Palette.cs',
                'docs/MODDING.md','docs/API-REFERENCE.md','docs/LEGACY-API.md','AugaApiExample/AugaApiExample.cs','AugaApiExample/AugaApiExample.csproj',
                'AugaApiExample/Properties/AssemblyInfo.cs','lib/AugaAPI.dll','Directory.Build.props','README.md') {
                [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $output $entry), $entry)
            }
        } finally { $zip.Dispose(); $stream.Dispose() }
        Write-Host "SDK archive: $archive (no game DLLs or runtime mod included)"
    }
} finally { Pop-Location }
