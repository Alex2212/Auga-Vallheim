# Auga development workspace

1. Extract the source into your chosen workspace directory.
2. Open Auga-Valheim.code-workspace in VS Code.
3. Copy `Auga.local.example.psd1` to the ignored `Auga.local.psd1` and set `ValheimDir` to your game directory. Alternatively set the `ValheimDir` environment variable. Run **Auga: Restore build prerequisites**. This downloads the checksum-pinned .NET SDK 8.0.408 to ignored `.build`, and recovers the exact original LFS bundle from RandyKnapp/Auga. It does not install tools system-wide. Visual Studio Build Tools and the old .NET Framework targeting pack are no longer required by the main projects.
4. Run Terminal > Run Task > Auga: Validate environment.
5. Run Auga: Build Debug and keep the complete compiler output.

The main plugin and Unity library target .NET Standard 2.1 and resolve game references from `ValheimDir`. Each build prepares publicized copies under `.build/game` using installed Mono.Cecil; these copies are for compilation only and must never be deployed. BepInEx and Unity references resolve from the installed game and are not copied into output. The build wrapper builds the main project and its Unity library dependency. The API example is an SDK project; build and validate it with `scripts/Build-AugaApi.ps1`.

Validation checks installation paths, the SDK and whether the bundle is a Git LFS pointer; it does not prove runtime compatibility. Build logs are saved under `test-artifacts`. The checked-in NuGet configuration has no package sources: runtime project restore uses the SDK's included reference pack. See `PORT-STATUS.md` for current runtime limitations.

Builds do not deploy the plugin. After a successful, reviewed build, close Valheim and disable other Auga copies in Vortex before running Auga: Deploy latest DLL. The script rejects a running game and other files named Auga.dll outside Auga-Dev; renamed copies must also be disabled. Follow the disposable-world test protocol in README-AI.md.

After testing, exit Valheim and run Auga: Collect logs. The resulting ZIP will be under test-artifacts in the repository.

## Prepare Nexus downloads

From the repository root, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Package-Nexus.ps1
```

The script validates the API and example, runs bridge checks, builds the Release mod, and writes `.build/nexus/`:

- `Auga-Unofficial-Valheim-1.0-<mod-version>.zip`: Nexus main file with `BepInEx/plugins/Auga/Auga.dll`, `translations.json`, installation README, and UI settings documentation.
- `AugaAPI-SDK-<api-version>.zip`: optional developer download with `lib/AugaAPI.dll`, bridge sources, API reference, modding/compatibility guides, settings documentation, and buildable example.
- `SHA256.json`: archive checksums.

Versions are read from the source. Only explicit distributable files are archived; game assemblies, logs, local configuration, and test/build output are excluded. Existing archives with the same version are replaced. The output directory is ignored by Git. This command does not deploy to Valheim or upload to Nexus.
