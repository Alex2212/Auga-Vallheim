param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)
. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
$dotnetPath = Join-Path $repoPath '.build\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetPath)) { throw 'Run Restore-BuildPrerequisites.ps1 to install the local SDK.' }
$env:DOTNET_CLI_HOME = Join-Path $repoPath '.build\dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:NUGET_PACKAGES = Join-Path $repoPath '.build\nuget-packages'
& (Join-Path $PSScriptRoot 'Prepare-GameReferences.ps1')
$outputRoot = Join-Path $repoPath 'test-artifacts'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$logPath = Join-Path $outputRoot ("build-{0}-{1}.log" -f $Configuration, (Get-Date -Format 'yyyyMMdd-HHmmss'))
# Build only: deployment and Unity asset updates must remain explicit operations.
& $dotnetPath build (Join-Path $repoPath 'Auga\Auga.csproj') "/p:Configuration=$Configuration" "/p:ValheimDir=$gamePath" "/p:RestoreConfigFile=$repoPath\NuGet.Config" /p:NuGetAudit=false /fl "/flp:logfile=$logPath;verbosity=diagnostic"
$buildExitCode = $LASTEXITCODE
Write-Host "Complete build log: $logPath"
exit $buildExitCode
