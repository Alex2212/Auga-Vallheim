# AI Development Guide: Port Auga to Current Valheim

## Mission

Port the Auga UI overhaul from its old Valheim target to the currently installed version of Valheim. Preserve Auga's visual identity, behavior, public API, and compatibility where practical.

The immediate goal is not a complete rewrite. First establish a reproducible build, make the plugin load safely, and then restore UI features incrementally.

## Local environment

- Repository: `E:\Desktop\Agua-Vallheim`
- Valheim: `G:\SteamLibrary\steamapps\common\Valheim`
- Managed game assemblies: `G:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed`
- BepInEx: `G:\SteamLibrary\steamapps\common\Valheim\BepInEx`
- BepInEx core: `G:\SteamLibrary\steamapps\common\Valheim\BepInEx\core`
- Development deployment folder: `G:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\Auga-Dev`
- BepInEx log: `G:\SteamLibrary\steamapps\common\Valheim\BepInEx\LogOutput.log`
- Mod manager: Vortex

Do not assume Valheim is installed on `C:`. Do not restore the original developer's hard-coded paths.

## Source baseline

Auga is a BepInEx and Harmony-based UI replacement originally maintained by RandyKnapp, n4, and Vapok. The old source targets .NET Framework 4.6.1 and references local copies of Valheim, Unity, BepInEx, Harmony, and publicized game assemblies.

The old source contains compatibility logic for Valheim `0.217.x`. The current game is much newer. Treat all direct accesses to game fields, methods, prefab paths, and hierarchy paths as potentially stale.

Useful upstream locations:

- Source fork: https://github.com/Vapok/Auga
- Original source: https://github.com/RandyKnapp/Auga
- Historical package: https://thunderstore.io/c/valheim/p/RandyKnapp/Auga/

## Non-negotiable rules

1. Preserve existing source and assets unless a change is required and understood.
2. Never edit files inside the Valheim installation except the dedicated `BepInEx\plugins\Auga-Dev` deployment folder.
3. Never overwrite Vortex-managed Auga files.
4. Do not load two copies of Auga. The user must disable any old Auga package in the active Vortex profile.
5. Do not modify game DLLs, BepInEx core DLLs, or Harmony.
6. Do not commit proprietary Valheim assemblies, Unity assemblies, generated logs, build output, or local absolute paths.
7. Keep machine-specific paths out of tracked project files. Put them in an ignored local configuration file or pass them as build properties.
8. Make small, reviewable commits grouped by subsystem.
9. Do not suppress exceptions merely to make the game start. Log the failure and disable only the affected Auga subsystem.
10. Do not claim a feature works until it has compiled and been tested in the game.

## Required workflow

### Phase 1: Establish the baseline

1. Inspect `git status` and preserve all user changes.
2. Record the current branch and commit.
3. Run the workspace task `Auga: Validate environment`.
4. Inventory the solution, projects, embedded resources, asset bundles, dependencies, and Harmony patches.
5. Run `Auga: Build Debug`.
6. Save the complete compiler output before changing project references.

### Phase 2: Modernize the build

1. Replace hard-coded assembly paths with one configurable `ValheimDir` property.
2. Resolve references from:
   - `$(ValheimDir)\valheim_Data\Managed`
   - `$(ValheimDir)\BepInEx\core`
3. Confirm the framework required by the installed BepInEx and Valheim assemblies before changing the target framework.
4. Remove duplicate references.
5. Do not add NuGet packages for assemblies already supplied by the game or BepInEx unless there is a clear reason.
6. Add `bin`, `obj`, logs, local paths, test artifacts, and game assemblies to `.gitignore`.
7. Reach a clean compile before changing runtime behavior.

### Phase 3: Safe plugin startup

1. Confirm the plugin ID remains `randyknapp.mods.auga` unless a deliberate compatibility decision changes it.
2. Update obsolete game-version checks.
3. Make asset and dependency loading failures explicit in the log.
4. Apply Harmony patches by subsystem so one failed patch does not hide all other failures.
5. Launch Valheim and verify:
   - BepInEx discovers the plugin.
   - The plugin reaches `Awake`.
   - Embedded assemblies load.
   - The asset bundle loads.
   - Harmony reports no missing target methods.
   - The main menu remains usable.

### Phase 4: Restore features incrementally

Use this order:

1. Main menu and startup screens
2. Player HUD
3. Health, stamina, eitr, and any current resource bars
4. Inventory and containers
5. Crafting and workbench panels
6. Build menu
7. Tooltips and item information
8. Minimap
9. Chat, messages, and text input
10. Store, character selection, world selection, and connection dialogs
11. Enemy HUD and damage text
12. Settings
13. External mod integrations
14. Controller navigation and current input behavior

For every subsystem:

1. Identify the original patches and prefab hierarchy assumptions.
2. Compare them with current game types through the installed assemblies.
3. Update field, method, and hierarchy references.
4. Add focused diagnostic logging.
5. Compile.
6. Deploy.
7. Test only the affected screen plus basic game navigation.
8. Review `LogOutput.log`.
9. Record what was tested and what remains broken.

## Harmony patch guidance

- Verify every target with reflection or decompiled current assemblies.
- Prefer explicit signatures when methods are overloaded.
- Check whether a target was renamed, moved, made static, or had parameters changed.
- Treat a patch that applies successfully as untested until its prefix/postfix executes in game.
- Avoid broad exception swallowing inside patches.
- Log each subsystem once during setup; do not spam per-frame logs.
- Unpatch only Auga's own Harmony instance during teardown.

## Unity and asset-bundle guidance

The embedded `augaassets` bundle may have been built with an older Unity version.

Before rebuilding it:

1. Attempt to load it with the current game.
2. Log the exact exception and Unity version.
3. Verify whether prefabs load and whether their scripts/components resolve.
4. Determine the Unity editor version used by current Valheim.
5. Preserve the original bundle and Unity project.

If a rebuild is necessary, rebuild from the Auga Unity project using a compatible Unity editor. Do not replace the bundle merely because individual C# bindings have changed.

## Testing protocol

Before each test:

1. Ensure Valheim is closed.
2. Build Debug.
3. Deploy only to `BepInEx\plugins\Auga-Dev`.
4. Ensure no second Auga DLL exists in enabled Vortex-managed plugin folders.
5. Launch through Steam/Vortex as appropriate.

Minimum smoke test:

1. Reach the main menu.
2. Open character selection.
3. Load a disposable local test world.
4. Check HUD and status bars.
5. Open inventory and a container.
6. Open crafting and building interfaces.
7. Open map, chat, settings, and pause menu.
8. Return to the main menu and exit normally.
9. Run `Auga: Collect logs`.

Never use an important world for early tests. Back up saves before testing builds that touch character/world-selection behavior.

## Bug report format

For every failed test, request or record:

- Current git commit
- Built Auga version
- Valheim version
- BepInEx version
- Active mod list
- Exact reproduction steps
- Expected result
- Actual result
- Screenshot or short video
- Complete `LogOutput.log`
- Whether the issue occurs with only Auga and its required dependencies enabled

## AI response requirements

At the end of each development iteration, report:

1. What changed
2. Why it changed
3. Files changed
4. Build result
5. Runtime test requested
6. Exact screens/actions the user must test
7. Logs or screenshots required next
8. Known remaining problems

If blocked, state the exact missing file, assembly, symbol, log entry, or test result. Do not guess that an API is compatible.

## Definition of done

The port is complete only when:

- The repository builds reproducibly without developer-specific paths.
- Auga loads without fatal BepInEx or Harmony errors.
- All intended Auga screens work with the current Valheim UI.
- The legacy asset bundle is either verified or rebuilt reproducibly.
- The minimum smoke test passes.
- A clean test with only required dependencies passes.
- Known incompatibilities are documented.
- Installation and build instructions match the current project.
- A release package can be produced without including game assemblies or private files.
