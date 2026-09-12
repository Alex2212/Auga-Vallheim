. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
if (Get-Process valheim -ErrorAction SilentlyContinue) {
    throw 'Close Valheim before deploying Auga.'
}
$pluginPath = Join-Path $gamePath "BepInEx\plugins\Auga-Dev"
$otherCopies = @(Get-ChildItem -LiteralPath (Join-Path $gamePath 'BepInEx\plugins') -Filter '*.dll' -File -Recurse | Where-Object {
    $_.DirectoryName -ne $pluginPath -and $_.BaseName -eq 'Auga'
})
if ($otherCopies.Count -gt 0) {
    throw "Disable other Auga copies in Vortex before deploying: $($otherCopies.FullName -join ', ')"
}
$dll = @('Debug', 'Release') | ForEach-Object {
    Get-Item -LiteralPath (Join-Path $repoPath "Auga\bin\$_\Auga.dll") -ErrorAction SilentlyContinue
} |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1
if (-not $dll) { throw "No compiled Auga.dll was found. Build the solution first." }
New-Item -ItemType Directory -Path $pluginPath -Force | Out-Null
Copy-Item -LiteralPath $dll.FullName -Destination (Join-Path $pluginPath "Auga.dll") -Force
$translations = Join-Path $repoPath "Auga\translations.json"
if (Test-Path -LiteralPath $translations) {
    Copy-Item -LiteralPath $translations -Destination $pluginPath -Force
}
Write-Host "Deployed to $pluginPath" -ForegroundColor Green
Write-Warning "Disable any other Auga package in the active Vortex profile."
