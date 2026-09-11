# Valheim 1.0 port baseline

Recorded 2026-09-11. Runtime compatibility has not been tested.

## Inventory

- No `.git` metadata: branch, commit and user-change history cannot be established.
- Installed game: the `Version` static initializer constructs `GameVersion(1, 0, 7)`, verified by reading `assembly_valheim.dll` IL with installed Mono.Cecil.
- Installed BepInEx assembly version: `5.4.23.5`.
- Plugin source: Auga `1.2.15`, ID `randyknapp.mods.auga`.
- Solution projects: `Auga`, `AugaUnityLib` (outputs `Unity.Auga`), `AugaApiExample`. Debug builds the first two.
- Main projects target .NET Framework 4.6.1; its reference assemblies are missing. MSBuild 16 from VS 2019 is present but absent from PATH. No dotnet SDK was listed. The main project requests C# 10.
- Embedded resources: `augaassets`, `Unity.Auga.dll`, `Libs/fastJSON.dll`. The Unity library resource currently points specifically to Debug output.
- `augaassets` is a 133-byte LFS pointer, expected size 92,177,531 bytes, SHA-256 `ccf200092c1d91deb3d452240c443d8da846b47c11cef4fe3db7b853bb95eadb`.
- Original Unity editor: `2020.3.45f1`. Bundle compatibility is untested.
- References: Unity/TMP/UI, BepInEx, Harmony, fastJSON and publicized game assemblies, with legacy installation paths and duplicate `ui_lib_publicized` entries.
- Harmony attributes occur in 20 source files covering startup/menu/connection, HUD, inventory/crafting, map, text/tooltips, chat/messages, store, settings/pause, enemy HUD and damage. Startup currently patches the whole assembly at once.
- Optional integrations: BetterTrader, MultiCraft, SimpleRecycling, Chatter and SearsCatalog. All untested.
- No BepInEx `LogOutput.log` was present.

## Build evidence

Full local logs are under ignored `test-artifacts`:

- `baseline-build.log`: restore failed accessing the sandbox-restricted NuGet migrations directory.
- `baseline-compile.log`: without restore, both main projects fail with MSB3644 (missing .NET Framework 4.6.1 reference assemblies).
- `build-Debug-*.log`: updated build wrapper reproduces those errors and returns exit code 1.

Legacy post-build actions were disabled for baseline commands. Automatic game-folder copying was subsequently removed from plugin and API example projects. The wrapper disables post-build events, discovers MSBuild and saves diagnostic output. Scripts now use ignored local game-path configuration. Validation identifies the targeting-pack and LFS blockers. PowerShell and task JSON syntax checks passed.

## Asset recovery and build migration

Source supplied by the user: https://github.com/RandyKnapp/Auga . The upstream main-branch LFS pointer matches the local pointer. Downloaded the 92,177,531-byte bundle and verified its SHA-256 against the recorded value before replacing the pointer. The original pointer is preserved in local test artifacts.

Restored Microsoft.NETFramework.ReferenceAssemblies.net461 1.0.3 and Microsoft.Net.Compilers.Toolset 4.0.1 under ignored `.build`. `Restore-BuildPrerequisites.ps1` reproduces these downloads with pinned SHA-256 checks and restores the matching asset bundle. No system installation was changed.

Main project changes: `Auga/Auga.csproj`, `AugaUnityLib/AugaUnityLib.csproj`, `Directory.Build.props`, `Directory.Build.targets`. Replaced legacy game paths, removed duplicate references, replaced missing ui_lib with current gui_framework, removed the absent assembly_steamworks reference, and resolved netstandard from the current game. The embedded Unity.Auga library now follows the selected build configuration. API example reference migration is still outstanding.

Tooling changes: `.gitignore`, `.vscode/tasks.json`, `scripts/Restore-BuildPrerequisites.ps1`, `scripts/Prepare-GameReferences.ps1`, `scripts/Build-Auga.ps1`, `scripts/Test-Environment.ps1`, and `README-SETUP.md`. Game-reference preparation reads installed DLLs and writes publicized build-only copies under `.build/game`; original game files are untouched.

Validation now passes. The restore script's already-restored path and all PowerShell syntax checks pass. Debug reaches C# compilation: **22 errors and 1 warning**, recorded in `test-artifacts/build-Debug-20260911-084037.log`. Errors in AugaUnityLib concern RecipeDataPair members, PlayerProfile statistics/path helpers, cloud-save status, input bindings/scrolling, and legacy Text fields now requiring TMP_Text. The warning is a netstandard 2.0/2.1 reference conflict: installed UnityEngine.CoreModule references netstandard 2.1. Framework compatibility still needs resolution; retaining the original target for this diagnostic build does not establish runtime support. The plugin's own compiler errors will become visible after its dependent library builds.

## Next work and runtime testing

Resolve framework alignment and the now-visible source API errors against installed game metadata before changing startup behavior. The bundle and compiler prerequisites are recovered. Never modify installed game assemblies.

No deployment or launch occurred. Runtime tests must wait for a successful build: first verify plugin discovery, dependency/bundle loading, Harmony setup and main-menu navigation; then character selection and remaining screens in README-AI order. Collect the complete BepInEx log and screenshots of failed screens. No UI subsystem is verified yet.
