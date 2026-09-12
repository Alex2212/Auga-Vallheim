. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
$requiredPaths = @(
    $repoPath,
    $gamePath,
    (Join-Path $gamePath "BepInEx"),
    (Join-Path $gamePath "BepInEx\core\BepInEx.dll"),
    (Join-Path $gamePath "BepInEx\core\0Harmony.dll"),
    (Join-Path $gamePath "BepInEx\plugins"),
    (Join-Path $gamePath "valheim_Data\Managed"),
    (Join-Path $gamePath "valheim_Data\Managed\assembly_valheim.dll")
)
$missing = @($requiredPaths | Where-Object { -not (Test-Path -LiteralPath $_) })
if ($missing.Count -gt 0) {
    Write-Host "Missing required paths:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
$problems = @()
if (-not (Test-Path -LiteralPath (Join-Path $repoPath '.build\dotnet\sdk\8.0.408\dotnet.dll'))) {
    $problems += 'Run scripts/Restore-BuildPrerequisites.ps1 to restore the pinned .NET SDK.'
}
$bundlePath = Join-Path $repoPath 'AugaUnity\AssetBundles\augaassets'
if (-not (Test-Path -LiteralPath $bundlePath)) {
    $problems += "Missing asset bundle: $bundlePath"
} else {
    $stream = [System.IO.File]::OpenRead($bundlePath)
    try {
        $header = New-Object byte[] 64
        $read = $stream.Read($header, 0, $header.Length)
        if ([System.Text.Encoding]::ASCII.GetString($header, 0, $read).StartsWith('version https://git-lfs.github.com/spec/v1')) {
            $problems += 'augaassets is a Git LFS pointer. Retrieve the original bundle with Git LFS before building.'
        }
    } finally { $stream.Dispose() }
}
Write-Host "Repository: $repoPath" -ForegroundColor Green
Write-Host "Valheim:    $gamePath" -ForegroundColor Green
Write-Host "BepInEx:    $(Join-Path $gamePath 'BepInEx')" -ForegroundColor Green
Write-Host "MSBuild:    $msbuildPath"
if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    exit 1
}
Write-Host "Environment validation passed." -ForegroundColor Green
