$ErrorActionPreference = 'Stop'
$repoPath = Split-Path -Parent $PSScriptRoot
$buildPath = Join-Path $repoPath '.build'
New-Item -ItemType Directory -Path $buildPath -Force | Out-Null
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Restore-Package($name, $url, $sha256, $marker) {
    $destination = Join-Path $buildPath $name
    if (Test-Path -LiteralPath (Join-Path $destination $marker)) {
        Write-Host "Already present: $name"
        return
    }
    if (Test-Path -LiteralPath $destination) { throw "Incomplete package directory: $destination. Rename it before retrying." }
    $archive = Join-Path $buildPath "$name.nupkg"
    Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $archive
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $sha256) { throw "Checksum mismatch: $name" }
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $destination)
    Write-Host "Restored: $name"
}
Restore-Package 'net461' 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net461/1.0.3/microsoft.netframework.referenceassemblies.net461.1.0.3.nupkg' 'BD52289E5FB8765090BB189B399A06477A350C0863018A455B5E5D9E45298CC9' 'build\.NETFramework\v4.6.1\mscorlib.dll'
Restore-Package 'compiler' 'https://api.nuget.org/v3-flatcontainer/microsoft.net.compilers.toolset/4.0.1/microsoft.net.compilers.toolset.4.0.1.nupkg' '82031774CAC5B8C2E4D0A6A968EFC1AEB33D62039C24EDE1354D7E596D5BD222' 'tasks\net472\csc.exe'

$bundlePath = Join-Path $repoPath 'AugaUnity\AssetBundles\augaassets'
$bundleHash = 'CCF200092C1D91DEB3D452240C443D8DA846B47C11CEF4FE3DB7B853BB95EADB'
if ((Test-Path -LiteralPath $bundlePath) -and (Get-FileHash -LiteralPath $bundlePath -Algorithm SHA256).Hash -eq $bundleHash) {
    Write-Host 'Original asset bundle already verified.'
    return
}
if (Test-Path -LiteralPath $bundlePath) {
    if ((Get-Item -LiteralPath $bundlePath).Length -gt 1024 -or (Get-Content -LiteralPath $bundlePath -Raw) -notmatch $bundleHash.ToLowerInvariant()) {
        throw 'The asset bundle differs from the expected original. Preserve and review it before restoring.'
    }
    Copy-Item -LiteralPath $bundlePath -Destination (Join-Path $buildPath 'augaassets.lfs-pointer.txt') -Force
}
$downloadPath = Join-Path $buildPath 'augaassets.download'
Invoke-WebRequest -UseBasicParsing -Uri 'https://media.githubusercontent.com/media/RandyKnapp/Auga/main/AugaUnity/AssetBundles/augaassets' -OutFile $downloadPath
if ((Get-FileHash -LiteralPath $downloadPath -Algorithm SHA256).Hash -ne $bundleHash) { throw 'Asset bundle checksum mismatch; original file was preserved.' }
Copy-Item -LiteralPath $downloadPath -Destination $bundlePath -Force
Write-Host 'Restored and verified original asset bundle.'
