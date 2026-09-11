param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)
. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
if (-not $msbuildPath) { throw 'MSBuild was not found. Install Visual Studio Build Tools.' }
& (Join-Path $PSScriptRoot 'Prepare-GameReferences.ps1')
$outputRoot = Join-Path $repoPath 'test-artifacts'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$logPath = Join-Path $outputRoot ("build-{0}-{1}.log" -f $Configuration, (Get-Date -Format 'yyyyMMdd-HHmmss'))
# Build only: deployment and Unity asset updates must remain explicit operations.
& $msbuildPath (Join-Path $repoPath 'Auga.sln') /m "/p:Configuration=$Configuration" "/p:ValheimDir=$gamePath" /p:PostBuildEvent= /fl "/flp:logfile=$logPath;verbosity=diagnostic"
$buildExitCode = $LASTEXITCODE
Write-Host "Complete build log: $logPath"
exit $buildExitCode
