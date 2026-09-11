# Auga development workspace

1. Extract the source into your chosen workspace directory.
2. Open Auga-Valheim.code-workspace in VS Code.
3. Copy `Auga.local.example.psd1` to the ignored `Auga.local.psd1` and set `ValheimDir` to your game directory. Alternatively set the `ValheimDir` environment variable. Install Visual Studio Build Tools, then run **Auga: Restore build prerequisites**. This downloads checksum-pinned Microsoft reference assemblies and a C# 10 compiler to ignored `.build`, and recovers the exact original LFS bundle from RandyKnapp/Auga. It does not install tools system-wide.
4. Run Terminal > Run Task > Auga: Validate environment.
5. Run Auga: Build Debug and keep the complete compiler output.

The main plugin and Unity library resolve game references from `ValheimDir`. Each build prepares publicized copies under `.build/game` using installed Mono.Cecil; these copies are for compilation only and must never be deployed. BepInEx and Unity references resolve from the installed game and are not copied into output. The separate API example has not been migrated.

Validation checks installation paths, MSBuild, targeting assemblies, the compiler and whether the bundle is a Git LFS pointer; it does not prove API or runtime compatibility. Build logs are saved under `test-artifacts`. The build script can be run directly to capture failures while resolving prerequisites. Debug does not require the API example's NuGet restore. See `PORT-STATUS.md` for current compiler blockers.

Builds do not deploy the plugin. After a successful, reviewed build, close Valheim and disable other Auga copies in Vortex before running Auga: Deploy latest DLL. The script rejects a running game and other files named Auga.dll outside Auga-Dev; renamed copies must also be disabled. Follow the disposable-world test protocol in README-AI.md.

After testing, exit Valheim and run Auga: Collect logs. The resulting ZIP will be under test-artifacts in the repository.
