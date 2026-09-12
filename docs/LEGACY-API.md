# Tabs, results panels, and custom variants

These APIs are **implemented in the current native-UI port**, using `InventoryPresentation`. They are useful extension points, not obsolete methods. The file name is retained for existing documentation links. The retired `WorkbenchPanelController` is not re-enabled.

All calls require Unity's main thread. Wait for `IsInventoryReady()` and the relevant capability before registration. Before readiness, legacy fallback normally returns null/false or does nothing. Implementation and example builds pass; native tab selection, rendering, and variant interaction still require in-game validation.

## Shared tab arguments

| Argument | Meaning |
| --- | --- |
| `tabID` | Non-empty, case-sensitive, stable identifier such as `your.plugin.tools`. Separate namespaces exist for player and workbench tabs. |
| `tabIcon` | Icon sprite. Null retains the template icon. |
| `tabTitleText` | Initial title, localized at creation. May be a localization token. |
| `onTabSelected` | Callback receiving the assigned index when selection changes to this tab. Null is allowed. Not called during registration or deselection. |

Custom IDs already registered by your mod return the existing data; this does not replace their original title, icon, callback, or mode. IDs colliding with built-in button names throw `ArgumentException`. Empty IDs also throw `ArgumentException`. The current single-row bars support 12 player tabs total (5 built-ins plus 7 custom) and 10 workbench tabs total (2 built-ins plus 8 custom); exceeding these limits throws `InvalidOperationException` before creation. Limits keep buttons within the existing header without overlap.

Buttons are laid out together and given left/right controller navigation. Content is hidden until selected. Player tabs are visible outside crafting stations; workbench tabs are shown at a station. Switching stations resets workbench selection to the native Craft/Upgrade state. Native Craft/Upgrade buttons restore native details and recipe list after a custom tab.

Register once per inventory scene, not once per inventory opening. Indices are assigned in registration order; use IDs for lookup and do not persist indices across scenes. There is no remove-tab API in this contract. Auga owns registered titles/buttons/containers until the inventory scene is destroyed; your mod owns the children it adds and should avoid callbacks that outlive their plugin. Do not independently destroy or reparent registered objects.

## Player panel tabs

### `bool PlayerPanel_HasTab(string tabID)`

Checks exact button names in the current player bar, including registered custom tabs. Null/empty IDs return false. No selection or creation occurs. Before inventory readiness, the method falls back to the old controller if present.

### `PlayerPanelTabData PlayerPanel_AddTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)`

Adds a player tab and a content container under the current inventory frame. Content stretches to the same inset region as Skills/Message Log; do not assume a fixed pixel height across future layouts. Auga manages its visibility and hides built-in content when your tab is selected. Populate the returned `ContentGO` with your controls.

| Return field | Meaning |
| --- | --- |
| `Index` | Assigned player-tab index, starting at 5 for custom tabs. |
| `TabTitle` | Writable legacy `UI.Text` title model. The current TMP header renders this value; the model's GameObject is intentionally inactive. |
| `TabButtonGO` | Registered Auga button; query selection with `PlayerPanel_IsTabActive`. |
| `ContentGO` | Per-tab content container, initially inactive. |

Capability: `player-tabs`. Returns null when no ready native presentation or legacy controller exists.

### Built-in access and selection

`PlayerPanel_GetTabButton(int index)` returns any registered player button, including custom tabs, or null for an invalid index. Built-ins: 0 Player, 1 Crafting, 2 Skills, 3 Message Log, 4 PvP. PvP is a toggle action, not a selectable content page.

`PlayerPanel_IsTabActive(GameObject tabButton)` checks selection and actual button visibility. Null, hidden, and unrecognized buttons return false. Do not infer custom selection from `Button.interactable`; custom buttons remain clickable.

## Workbench tabs

### `bool Workbench_HasWorkbenchTab(string tabID)`

Checks exact names in the current workbench bar. Null/empty IDs return false. This is not a test for whether the player is at a station; registration can happen as soon as inventory setup is ready.

### `WorkbenchTabData Workbench_AddWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)`

Creates a custom workbench tab with its **own** cloned original requirements panel and complex item-information panel. Native recipe refresh is excluded from the selected custom display. This differs deliberately from the old controller, which shared these panels between integrations.

| Return field | Meaning |
| --- | --- |
| `Index` | Workbench index, starting at 2 for custom tabs. |
| `TabTitle` | Writable legacy `UI.Text` title model; copied into the current station-mode header. |
| `TabButtonGO` | Registered button. |
| `RequirementsPanelGO` | Per-tab `CraftingRequirementsPanel`, with its native updater disabled. Use `RequirementsPanel_*` to access icons, resources, and wires. |
| `ItemInfoGO` | Per-tab `ComplexTooltip`. Use `ComplexTooltip_*` to populate it. |

The caller owns displayed data and feature behavior. This does not create recipes, consume resources, award items, or implement crafting transactions. The native recipe list/details are hidden while a custom workbench tab is selected; the original game model and callbacks stay intact. A mod needing a custom recipe list can add its own UI under its tab's content parent.

Capability: `workbench-tabs`. Returns null before readiness without a legacy controller.

### `WorkbenchTabData Workbench_AddVanillaWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)`

Same registration/return contract, with `MimicVanillaCraftingTab` attached to that tab's content. While active, it copies the native recipe topic/description into the tab's item-information panel. This is useful for integrations that already update native description fields.

It does not create a native recipe-engine tab. Mirroring clears/rebuilds the item's text boxes each update, so use the regular custom-tab method when your mod owns rich tooltip sections. Registering the same ID again with the other method does not change its mode.

### `GameObject Workbench_CreateNewResultsPanel()`

Clones the original results-panel prefab under the currently selected custom workbench tab, or under the standard crafting root when no custom tab is selected. The caller configures and destroys the returned clone. Activation/layout initially follow the prefab; set them as needed. The method only creates UI, not a crafting transaction. Returns null before readiness without a legacy controller. Capability: `workbench-tabs`.

### Built-in workbench controls

`Workbench_GetCraftingTabButton()` and `Workbench_GetUpgradeTabButton()` return the current built-in buttons after readiness. Add your own listeners if needed; never replace their `onClick` events. `Workbench_IsTabActive(GameObject tabButton)` supports both built-in and custom workbench buttons and checks visibility as well as selection.

## Custom variant dialog

### `Text CustomVariantPanel_Enable(string buttonLabel, Action<bool> onShow)`

Creates/reuses an original Auga custom-variant button and scrollable text dialog under the current crafting root. It hides the normal variant button while enabled, localizes `buttonLabel`, and installs `onShow`.

Returns the dialog's existing `UI.Text`, which your mod can populate. The button toggles the dialog and calls `onShow(true)` when opened or `onShow(false)` when closed by that button. Enabling does not automatically open it or invoke the callback. Null callbacks are allowed. Re-enabling replaces the callback; there is one shared custom-variant feature per inventory, so coordinate with other mods.

The dialog overlays the crafting detail region; native/custom item details are hidden while it is open. Switching tabs/stations closes it. Closing through these lifecycle paths does not synthesize callback notifications, matching the historical disable semantics. Populate and clean up your own state accordingly.

Returns null before readiness without a legacy controller. Capability: `custom-variants`.

### `void CustomVariantPanel_SetButtonLabel(string buttonLabel)`

Localizes and updates an existing custom button label. Does not create, enable, or open a dialog. No-op before Enable has created the button.

### `void CustomVariantPanel_Disable()`

Closes the dialog, removes its callback, hides the custom button, and restores the native variant button's game-controlled visibility. Does not invoke `onShow(false)`. Safe before Enable; no-op without a presentation/controller.

## Registration example

```csharp
if (!Auga.API.IsInventoryReady()) return;
if (Auga.API.SupportsFeature("player-tabs"))
{
    var tab = Auga.API.PlayerPanel_AddTab("your.plugin.tools", null, "My tools",
        index => UnityEngine.Debug.Log("Selected tools tab " + index));
    // Populate tab.ContentGO once. Repeated registration returns the existing tab.
}
```

For workbench data that references itself in the selection callback, assign a local `WorkbenchTabData` before any user selection can occur. Registration itself does not invoke the callback. The [example plugin](../AugaApiExample/AugaApiExample.cs) demonstrates this pattern.

## Superseded mechanisms and historical differences

| Mechanism | Status / replacement |
| --- | --- |
| Old Harmony self-patching external shim | Superseded by the generated reflection bridge; do not copy it from historical API ZIPs. |
| Re-enabling `WorkbenchPanelController` / hard-coded legacy hierarchy paths | Unsupported implementation technique. Call the public API; current registration is owned by `InventoryPresentation`. |
| `ComplexTooltip_Add*CreatedListener` | Retained process-lifetime API. For removable callbacks, prefer `ComplexTooltip_Subscribe*` and dispose its handle. |
| `ComplexTooltip_AddItemStatPreprocessor` | Still useful, process-lifetime only; no removal handle. Not marked obsolete. |
| UI.Text tab-title return fields | Retained for compatibility as writable title models; current visual headers use TMP. Do not activate/reparent the model or expect it to be the rendered label. |

Historical duplicate IDs could leave unassigned out-parameters and make the old wrapper throw; native registration is now idempotent. Historical workbench panels were shared; current tabs isolate their presentation. Historical content used fixed dimensions; current player content follows the adapted frame. None of the public tab/results/variant method signatures is removed or classified as obsolete merely because its old controller was retired.

## Manual runtime checks before shipping an integration

Register twice with the same ID; confirm one button and the same underlying content references. Switch between custom and built-in tabs and verify visibility, title updates, callback indices, and keyboard/controller navigation. At a workbench, verify native recipe changes do not overwrite custom details and that returning to Craft/Upgrade restores the original list and actions. Open/close custom variants, disable them, change stations, and close/reopen inventory. Rejoin a world and reacquire/register against the new scene. These checks are not replaced by build or bridge tests.
