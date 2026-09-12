param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
[void][Reflection.Assembly]::LoadFrom((Join-Path $gamePath 'BepInEx/core/Mono.Cecil.dll'))
$runtime = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $repoPath "Auga/bin/$Configuration/Auga.dll"))
$shim = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $repoPath 'Auga/bin/API/AugaAPI.dll'))
function Get-ApiSurface($assembly) {
    $api = $assembly.MainModule.Types | Where-Object FullName -eq 'Auga.API'
    foreach ($method in $api.Methods | Where-Object { $_.IsPublic -and $_.IsStatic -and -not $_.IsConstructor }) {
        $parameters = $method.Parameters | ForEach-Object { "$($_.ParameterType.FullName) $($_.Name):$($_.IsOptional):$($_.Constant)" }
        "$($method.ReturnType.FullName) $($method.Name)($($parameters -join ','))"
    }
    foreach ($field in $api.Fields | Where-Object { $_.IsPublic -and $_.IsStatic }) { "field $($field.FieldType.FullName) $($field.Name)" }
    foreach ($name in 'Auga.RequirementWireState','Auga.PlayerPanelTabData','Auga.WorkbenchTabData') {
        $type = $assembly.MainModule.Types | Where-Object FullName -eq $name
        foreach ($field in $type.Fields | Where-Object IsPublic) { "$name/$($field.Name):$($field.FieldType.FullName):$($field.Constant)" }
    }
}
try {
    $differences = Compare-Object @(Get-ApiSurface $runtime | Sort-Object) @(Get-ApiSurface $shim | Sort-Object)
    if ($differences) { $differences | Format-Table | Out-String | Write-Host; throw 'Runtime/shim API contract mismatch.' }
    Write-Host 'Compiled runtime/shim signatures, defaults, palette fields and DTOs match.'
} finally { $runtime.Dispose(); $shim.Dispose() }
