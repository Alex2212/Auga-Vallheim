# Integrating with Auga

This guide describes API **2.0.0** in this repository's native-UI port, built against **Valheim 1.0.12**. The plugin ID remains `randyknapp.mods.auga`. The API version is separate from the mod package version. This is not a compatibility claim for other Auga releases.

See the [complete signature reference](API-REFERENCE.md), [example plugin](../AugaApiExample/AugaApiExample.cs), and runtime implementation in `Auga/API.cs` in the source repository.

The [tab and variant API guide](LEGACY-API.md) documents restored methods, their parameters, return objects, callbacks, ownership, and differences from the legacy implementation. It also identifies superseded integration patterns.

## Choose an integration

**Optional integration (recommended):** compile these four SDK source files into your plugin:

- `Auga/API.External.cs`: generated public methods.
- `Auga/API.Bridge.cs`: runtime discovery and forwarding.
- `Auga/API.Common.cs`: enums and tab data contracts.
- `Auga/API.Palette.cs`: shared color values.

Do not compile `API.cs` into your plugin and do not also reference `Auga.dll`. The example project shows source linking; you can instead copy the four files into your own source tree. The shim does not patch methods with Harmony and does not require Auga to be installed.

```csharp
[BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
```

**Required integration:** reference the installed `Auga.dll` with `Private=false`, omit all shim source, and declare a BepInEx hard dependency. Keep the Auga runtime assemblies in their own mod package. Do not bundle Unity, game assemblies, BepInEx, or `Unity.Auga.dll` with your integration.

The SDK also includes `lib/AugaAPI.dll` for existing binary-based integrations. Source embedding avoids shared-shim version conflicts between mods. `AugaAPI.dll` is not a BepInEx plugin and must never replace `Auga.dll`. Historical `AugaAPI.zip` and `AugaAPI1.1.zip` in the repository are old snapshots; use the new SDK archive.

## Readiness and lifetime

All UI calls, subscriptions, disposal, and callbacks belong on Unity's main thread.

| Probe | Meaning |
| --- | --- |
| `IsLoaded()` | Runtime assembly exists; assets/UI may not be ready yet. |
| `GetApiVersion()` | Contract version; optional shim returns null without Auga or against an older runtime without this method. |
| `IsReady()` | Shared panel and tooltip assets are loaded. |
| `IsInventoryReady()` | Current inventory presentation finished its `Start` setup. |
| `SupportsFeature(name)` | Exact capability below is available now; unknown names return false. |

Do not add UI in an `InventoryGui.Awake` postfix and assume Auga has finished. Wait for `IsInventoryReady()`, as the example does. Reacquire roots and buttons for each new scene; Unity references from a prior world are invalid. `Inventory_GetRoot()` returns the native inventory visibility root, so mod-owned children follow inventory visibility.

The optional shim returns default values/no-ops when Auga is absent. A missing non-probe method on an installed older runtime raises `NotSupportedException`. Runtime exceptions retain their original type and stack. Check readiness and capabilities instead of treating null results as successful creation.

## Current support

| Capability | API families and limitations |
| --- | --- |
| `ui-factories` | Fonts, palette, panels, buttons, dividers. Original Auga assets and Unity `UI.Text` controls are preserved. TMP font helpers support current native TMP controls. |
| `tooltips` | Simple, item, food, status-effect and skill tooltip setup. The caller supplies a raycastable hover target. |
| `tooltip-events` | Complex tooltip text/sections, stat preprocessors, and generation callbacks. |
| `requirements` | Inspect original `CraftingRequirementsPanel` objects and update wires. A panel without wires is a no-op for `SetWires`. |
| `inventory-access` | Inventory visibility root, built-in player tab buttons, workbench Craft/Upgrade buttons, and active-state queries. |
| `player-tabs` | Custom player tabs registered with the current presentation after inventory readiness. |
| `workbench-tabs` | Custom workbench tabs with independent requirements/details, optional native-description mirroring, and results-panel creation. |
| `custom-variants` | Original Auga custom-variant button and text dialog, owned by the current presentation. |

Tab/variant calls now route through `InventoryPresentation` after readiness; they do not require enabling the retired controller. Before readiness, original null/false/no-op fallback behavior remains. Use capability checks, stable tab IDs, and the lifecycle rules in the tab guide. Custom tabs provide presentation and selection, not crafting transactions or game rules. Their Unity interaction is pending in-game validation; compilation and API contract checks do not prove visual behavior.

Built-in player tab indices in this port are 0 Player, 1 Crafting, 2 Skills, 3 Message Log, 4 PvP; custom tabs append from 5. Workbench custom tabs append after Craft (0) and Upgrade (1). Availability/visibility depends on game state. Returned buttons are **borrowed**: use `onClick.AddListener`, not replacement of `onClick`, and remove your listeners during cleanup. Do not destroy or reparent Auga/native controls. Factories return **mod-owned** objects; name them with your plugin ID and destroy them on teardown.

## Creating controls

```csharp
if (!Auga.API.IsInventoryReady()) return;
var panel = Auga.API.Panel_Create(Auga.API.Inventory_GetRoot(),
    new Vector2(300, 140), "my.mod.Panel", withCornerDecoration: true);
panel.SetActive(true);
var button = Auga.API.FancyButton_Create(panel.transform, "Action", "MY ACTION");
button.gameObject.SetActive(true);
button.onClick.AddListener(MyAction);
```

Position the panel for your mod's UX; do not cover standard controls. Factories preserve prefab sizing/activation unless stated, and button labels accept localization tokens. `Panel_Create` centers the panel with the requested size. Divider width `-1` retains prefab width; medium/large dividers return `(root, Content)` for your caption. Fonts are shared assets: do not destroy or modify them. `GetNorseTMPFont()` and `GetBoldTMPFont()` return shared TMP font assets. Native TMP and original legacy `Text` are different components; do not cast between them.

Palette fields (`Brown1`–`Brown7`, `Blue`, `Gold`, etc.) are HTML color strings. They are local values in the source shim, not a global theme-setting API. Mutating one does not recolor the game or other mods.

## Hover tooltips

```csharp
// item is the ItemDrop.ItemData this slot represents.
Auga.API.Tooltip_MakeItemTooltip(slot, item);

// Or a text-only tooltip:
Auga.API.Tooltip_MakeSimpleTooltip(button.gameObject);
var tooltip = button.GetComponent<UITooltip>();
tooltip.m_topic = "My heading";
tooltip.m_text = "My description";
```

The object needs a UI `Graphic` with `raycastTarget=true` under a Canvas with a GraphicRaycaster, and an EventSystem. Parent CanvasGroups must permit raycasts. Put hover targets above decorative outlines and labels, or turn those decorations' raycasts off. The API does not make arbitrary object hierarchies interactive. Native tooltip delay and positioning remain in effect.

Rich helpers initialize topic/text so the current game's hover gate opens; switching helper types clears old item/food/status/skill data. Bind on creation or when the represented value changes, **not every frame**: rebinding closes that object's open tooltip. For an upgrade preview, clone the item and change the clone's quality; never mutate the player's real item. Empty slots should have their tooltip disabled. Simple tooltip callers set their own text after selecting the simple presentation.

## Extending tooltip contents

```csharp
IDisposable subscription = Auga.API.ComplexTooltip_SubscribeItem((tooltip, item) => {
    var box = Auga.API.ComplexTooltip_AddTwoColumnTextBox(tooltip);
    Auga.API.TooltipTextBox_AddLine(box, "My stat", "12", localize: false);
});
// OnDestroy / feature teardown, on the main thread:
subscription?.Dispose();
```

Equivalent subscriptions exist for Food, StatusEffect, and Skill. Dispose is idempotent. Register once per plugin/feature lifetime, not once per inventory opening. Callbacks receive borrowed tooltip objects; do not retain them after the callback, and do not call `ComplexTooltip_SetItem` recursively from an item-generated callback. Generation hooks can also run for crafting details, not only floating hover tooltips. Keep callbacks fast and handle your own errors.

Legacy `ComplexTooltip_Add*CreatedListener` methods and `AddItemStatPreprocessor` are retained but have no removal handle; use them only for process-lifetime registration. Prefer the new subscriptions for reloadable integrations. Stat preprocessors receive `(item, label, value)` and return a label/value tuple; preserve unrelated stats.

Text-box `localize` defaults to true. `overwrite` overloads explicitly choose replacement versus append. Obtain text-box objects from `ComplexTooltip_Add*` helpers; passing unrelated objects is invalid. `RequirementsPanel_*` expects an original Auga requirements panel, not arbitrary inventory slots. Requirement wire enums are marshalled by value; tab DTOs are copied across the optional shim while preserving Unity-object references.

## Building the example and SDK

In an extracted SDK, with a .NET 8 SDK installed:

```powershell
dotnet build AugaApiExample/AugaApiExample.csproj -p:ValheimDir="X:/Steam/steamapps/common/Valheim"
```

References come from that installation and are not copied into the output. Install only your compiled example/mod DLL for testing alongside Auga. The example adds a clearly named demonstration panel and tooltip line; it is not part of the production Auga deployment.

In this source checkout, follow `README-SETUP.md` first to prepare the local SDK/game references, then:

```powershell
python scripts/api/generate.py
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-AugaApi.ps1 -Package
```

This builds the runtime, shim and example, compares compiled signatures/defaults/DTOs, runs isolated bridge tests, and produces `.build/AugaAPI-SDK-2.0.0.zip`. It does not deploy, restart the game or publish the SDK. Python 3 is required only for generation/checking in the source checkout.

## Validation and maintaining the contract

Edit the runtime methods in `API.cs`, then regenerate `API.External.cs` and `API-REFERENCE.md`. Contract types live in `API.Common.cs`; colors live in `API.Palette.cs`, compiled into both assemblies. Keep existing overloads/defaults when adding compatible functionality. Do not change enum numeric values or DTO field types in place. A new signature does not automatically mean its backing UI exists: update `SupportsFeature` and this support table together.

Automated checks cover absent/late runtime loading, exact overload and null selection, enum-array conversion, DTO conversion/reference identity, delegate forwarding, old-runtime probes, missing-method errors and exception propagation. Compiled parity checks cover the actual game-facing API surface. These do not simulate Unity pointer events.

Before publishing an integration, test without Auga, with this Auga build, after logout/rejoin, with repeated inventory/tab opening, and with mouse/controller input. Verify cleanup, hover targets and UI placement in game. The new example and API UI changes are build-verified; in-game example interaction remains a manual validation step.
