. (Join-Path $PSScriptRoot 'Get-LocalEnvironment.ps1')
$managedPath = Join-Path $gamePath 'valheim_Data\Managed'
$referencePath = Join-Path $repoPath '.build\game'
New-Item -ItemType Directory -Path $referencePath -Force | Out-Null
[void][Reflection.Assembly]::LoadFrom((Join-Path $gamePath 'BepInEx\core\Mono.Cecil.dll'))
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($managedPath)
$readerParameters = New-Object Mono.Cecil.ReaderParameters
$readerParameters.AssemblyResolver = $resolver
function Expand-TypeAccess($type) {
    if ($type.IsNested) { $type.IsNestedPublic = $true } else { $type.IsPublic = $true }
    foreach ($field in $type.Fields) { $field.IsPublic = $true }
    foreach ($method in $type.Methods) { $method.IsPublic = $true }
    foreach ($nested in $type.NestedTypes) { Expand-TypeAccess $nested }
}
foreach ($name in @('assembly_guiutils', 'assembly_postprocessing', 'assembly_sunshafts', 'assembly_utils', 'assembly_valheim', 'gui_framework')) {
    $sourcePath = Join-Path $managedPath "$name.dll"
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($sourcePath, $readerParameters)
    try {
        foreach ($type in $assembly.MainModule.Types) {
            if ($type.Name -ne '<Module>') { Expand-TypeAccess $type }
        }
        $assembly.Write((Join-Path $referencePath "$name.dll"))
    } finally { $assembly.Dispose() }
    Write-Host "Prepared build reference: $name"
}
$resolver.Dispose()
