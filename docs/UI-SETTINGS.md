# UI settings

## Global scroll speed

Open **Settings ? Accessibility ? Scroll Speed**. The default is **10?** and the range is **1??20?** in whole steps. Save settings to apply the value without restarting; Back discards the unsaved slider change. The saved value persists across game launches.

The setting covers mouse-wheel events on Unity `ScrollRect` lists, including character portraits, crafting and upgrades, skills, compendium, building, server/save browsers, and mod-added lists. It replaces the previous separate portrait, skills, and compendium speeds. Dragging a scrollbar or content, controller navigation, and interfaces with their own non-ScrollRect input are not scaled.

The BepInEx configuration entry is in `BepInEx/config/randyknapp.mods.auga.cfg`:

```ini
[Accessibility]
ScrollSpeed = 10
```

One multiplier unit corresponds to 40 UI units of ScrollRect sensitivity; the default is 400. Wheel input from the operating system and trackpads can vary.

## Character creation

The current presentation reuses Auga's original panel, gender icons, gradient sliders, Hair/Beard tabs, and five-column portrait grid. Native Valheim logic still owns appearance selection, name validation, creation, and cancellation. The name viewport is inset to keep text and the caret clear of its decorative ends.

## Button feedback

Adapted confirmation and action buttons use Auga color tints for hover, keyboard/controller selection, press, and disabled states. Native sprite swapping is cleared where Auga replaces the button artwork.

## Validation status

The character portrait grid has been observed in-game. Builds pass. Final name-field geometry and global scroll-setting behavior still require in-game verification; compilation alone does not verify interactions or every screen size.
