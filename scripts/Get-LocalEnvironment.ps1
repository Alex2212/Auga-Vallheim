$ErrorActionPreference = 'Stop'
$repoPath = Split-Path -Parent $PSScriptRoot
$localConfigPath = Join-Path $repoPath 'Auga.local.psd1'
$localConfig = @{}
if (Test-Path -LiteralPath $localConfigPath) {
    $localConfig = Import-PowerShellDataFile -LiteralPath $localConfigPath
}
$gamePath = $env:ValheimDir
if ($localConfig.ValheimDir) { $gamePath = $localConfig.ValheimDir }
if (-not $gamePath) {
    throw 'Set ValheimDir in Auga.local.psd1 (see Auga.local.example.psd1), or set the ValheimDir environment variable.'
}
$gamePath = [System.IO.Path]::GetFullPath($gamePath)
$msbuildPath = $null
$msbuildCommand = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuildCommand) { $msbuildPath = $msbuildCommand.Source }
if (-not $msbuildPath) {
    $vswherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswherePath) {
        $msbuildPath = & $vswherePath -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }
}
