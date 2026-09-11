. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
$outputRoot = Join-Path $repoPath "test-artifacts"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runPath = Join-Path $outputRoot $stamp
New-Item -ItemType Directory -Path $runPath -Force | Out-Null
$log = Join-Path $gamePath "BepInEx\LogOutput.log"
if (Test-Path -LiteralPath $log) {
    Copy-Item -LiteralPath $log -Destination (Join-Path $runPath "LogOutput.log")
} else {
    Write-Warning "BepInEx log was not found at $log"
}
Get-ChildItem -LiteralPath (Join-Path $gamePath "BepInEx\plugins") -Filter "*.dll" -File -Recurse |
    Select-Object FullName, Length, LastWriteTimeUtc |
    Sort-Object FullName |
    Out-File -LiteralPath (Join-Path $runPath "plugins.txt") -Encoding utf8
Compress-Archive -LiteralPath $runPath -DestinationPath "$runPath.zip" -Force
Write-Host "Diagnostics: $runPath.zip" -ForegroundColor Green
