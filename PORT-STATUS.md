# Valheim 1.0 port baseline

Recorded 2026-09-11. Main-menu and settings presentation have been tested; full runtime compatibility remains unverified.

## Detailed crafting reference restoration (latest)

InventoryPresentation now clones the original RightColumn under the retained native crafting controller, preserving rune artwork, outlined ingredient slots/wires, stat groups, description and original buttons. Native crafting bindings remain hidden but active; visible Craft/Cancel/Style buttons delegate to those callbacks. Original multicraft sample controls are hidden. Skill/stat tabs continue to use original content.

At user request the frame is 600x1030 logical units, scaled uniformly to about 24% of viewport width; its upper-right corner tracks the minimap border's upper-right corner via screen coordinates. The inventory footer was visually verified with original shield/bag sprites, readable native armor/weight and the original separator. The recipe list is inset, clipped by RectMask2D, with icon/text padding and the selected background extended to the black divider.

Ingredient prefab children were serialized inactive: activating the slot alone did not show its icon/text. These children are now explicitly activated from the current native requirement slots, copying their displayed names/counts/icons/colors so upgrader-only resources and other native filtering remain correct. Live recipe-parts-check.jpg verified ingredient visibility and the taller aligned frame; it exposed an extra upgrader ingredient from an earlier raw-resource display, which the latest native-slot binding replaces. The game also reset the crafting-container position; reference dimensions/position are now restored in LateUpdate.

Latest build/deploy: recipe-highlight-build.log, zero errors and four existing reference warnings; deployed only to Auga-Dev and relaunched. Live recipe-highlight-check.jpg verifies the list fully inside the panel, selection touching the divider, native Wood/Stone icons and counts, red missing Stone amount and disabled Craft action for Hammer. The corner alignment and taller frame are visible. The user appears to have crafted a Torch during review (inventory changed); the agent did not invoke crafting and this is not a recorded complete functional test. Earlier live original-column-check.jpg and reference-details-check.jpg show restored original geometry and caption progression. Actual crafting, cancellation, upgrading, repair, variant selection and controller navigation have not been functionally exercised. Do not describe the entire inventory/crafting port as complete.

## In-game inventory and build UI checkpoint

Original visual references reviewed: all 21 images in Auga/Screenshots. Screenshots 5-8 cover player inventory/status/tooltips/crafting/skills, 9 build UI, 13-14 compendium, 16-17 station craft/upgrades, 18 container. Approved health/stamina/food HUD is unchanged.

The current native BuildUi owns material/search/recent/favorite browsing. The obsolete hammer prefab destroyed this controller and caused Hud.UpdateBuild null references. LegacyBuildMenuSupported is false, and only the incompatible legacy build patches are skipped. BuildPresentation styles the retained controller. A populated build list was verified in hammer-open-verified.jpg; placement was not exercised.

InventoryPresentation retains native item grids and crafting callbacks. Added clipped slot backgrounds, hotbar-row separation, shared right panel with original diamond artwork, original player-status and skills content. Skills now display current definitions at level zero without writing player skill data. Live shared-panel-first.jpg verified skill content including modern skills. That check exposed legacy UI.Text tooltip fallthrough into a TMP-only native method; UITooltip_Patch now handles named legacy labels. Craft content is deactivated when another panel is selected to avoid overlapping native subgroups.

Latest compiled/deployed iteration: crafting-details-build.log, zero errors and four existing reference warnings. Right panel reduced to 24% screen width and 81% height. Footer uses original shield/bag sprites and separator with fresh native TMP value bindings. Crafting details reuse original ComplexTooltip stat presentation and arrange native requirement slots around the result icon. MinimapPresentation places the biome label below the retained map and lays out effects vertically. Deployment restricted to Auga-Dev.

Live compact-crafting-check.jpg verifies the compact panel, readable armor/weight footer with original icons/divider, radial native material counts, and grouped item stats. This revealed a clipped native recipe viewport and duplicate station heading; the subsequent crafting-layout-fix-build.log build passed with zero errors/four reference warnings and was deployed. Subsequent crafting-viewport-check.jpg verifies readable recipe names, corrected panel backgrounds and grouped stats; a reparented Cancel button was found covering Craft while idle. Latest deployed build is crafting-action-build.log (zero errors/four reference warnings): action visibility follows m_craftTimer, recipe rows stretch to their viewport, and list content starts at the top. inventory-action-final.jpg verifies the idle Craft button is visible. Both native and owned captions rendered after fixing Cancel visibility; the follow-up disables only the duplicate native caption. Footer and compact panel are visually verified; the remaining crafting spacing still differs from the source reference. The current session log contained no tooltip exceptions after the legacy Text fallback fix. Compendium restoration, full upgrade wire layout, quality diamonds, controller navigation, actual crafting/upgrading/repair and container transfer remain incomplete or untested. No crafting, item transfer or world edits were performed by the agent.

## Compact New Character panel

Added CharacterCreationPresentation.cs via FejdStartup setup. A single 500x850 UI-unit panel anchored upper-left contains the Norse heading, centered native appearance group, pointed name input, and equal Cancel/Done footer buttons. Brown panel/corners, bold option labels, diamond sex toggles, thin sliders with gold diamond handles and gold diamond ASCII arrow buttons match the approved menu styling. Native customization, name validation and creation callbacks remain intact. PlayerCustomizaton lives on the whole screen, so layout reparents only the common appearance-control ancestor, not the component root.

Build passed (0 errors, 4 existing reference warnings; `test-artifacts/character-creation-build.log`) and deployed only to Auga-Dev. `character-creation-verified.jpg` confirms the compact layout. Clicked the next hair arrow: `character-creation-hair-check.jpg` shows Gathered Locs and the changed preview. No character was created. Final log shows successful setup without a presentation exception. Female/beard visibility, all slider interactions, name validation, completed creation, other resolutions/UI scales and controller navigation remain unverified.

## Manage Saves presentation (previous checkpoint)

Added ManageSavesPresentation.cs with native menu Awake, save-row Start and backup-row creation hooks. Brown cornered panels, Norse heading, bold uppercase centered tabs with thin diamond dividers, dark list, blue selection/hover, shared scrollbar styling and gold cloud-usage fill. Four equal-width action buttons preserve their native callbacks and enabled states; Back and Expand/Collapse align along the footer. Owned action borders avoid native button texture resets. Removed native panel_separator images, including the old gold tab underline at user request.

Build passed with 0 errors and 4 existing reference warnings (`test-artifacts/manage-saves-build.log`); deployed only to Auga-Dev. Final live capture `manage-saves-final.jpg` verifies separator removal, selection and disabled actions. Earlier `manage-saves-review.jpg` shows expanded backup rows; final automated expansion capture picked up the BepInEx console instead, so final expansion/scroll and controller navigation remain unverified. User actively navigated both Worlds and Characters during review. No save move, restore or deletion was performed by the agent. Runtime log shows styling initialized without a presentation exception.

## Server dialogs and row delete controls (previous checkpoint)

Styled native Add Server dialog via JoinServerPresentation: brown panel/corners, Norse heading, original pointed input sprite, italic native placeholder and equal 200x48 Cancel/Add Server buttons. Added PopupPresentation.cs (UnifiedPopup.Show postfix) for warning/confirmation panels and Norse buttons; native popup stack and callbacks retained. Remove server confirmation visually verified (`test-artifacts/server-dialogs-ready.jpg`); agent clicked No, but concurrent user navigation means an isolated cancel cycle was not established. Incompatible-version warning uses the same popup styling but has not been separately captured after deployment. Other shared popup types need regression checks.

Added rightmost delete column, reusing the original trash sprite and native SetSelectedServer/OnRemoveServerButton confirmation path. Enabled for Favorite/Recent categories, disabled for public lists. This supersedes the briefly drafted favorites-only deletion handler; the user's final instruction was to relocate existing native Delete behavior. Standalone delete is hidden alongside the old favorite button. Status, crossplay and private icons are shifted left to make room. Delete glyph enlarged from 18 to 28 UI units at user request; column width remains 36. User accepted the result.

Latest build passed: 0 errors, 4 existing warnings (`test-artifacts/server-delete-icon-build.log`), deployed only to Auga-Dev. Verified larger row trash icons (`delete-icon-review.jpg`) and Add Server dialog (`add-server-review.jpg`), user replied "good". Agent did not confirm a deletion, enter an address or add/connect to a server. User actively changed selections and saved-server lists during review. Left Add Server open. Entry validation, successful add, confirmed deletion and full popup/controller regression remain unverified.

## Join server presentation, favorites and filter placement

Filter spacing approved: moved filter/refresh down 16 UI units (y=-222), with count and list/scrollbar top moved equally to preserve separation. Build passed (0 errors, 4 existing warnings; `test-artifacts/join-filter-gap-build.log`), deployed, visually verified in `test-artifacts/join-filter-gap.jpg`. User replied "great". No other presentation changes in this refinement.

Category divider refinement: added matching thin lines with hollow diamonds flanking Favorite/Recent/Friends/Community. Decorative images do not receive pointer events; existing category hit targets remain. Build passed (0 errors, 4 existing warnings; `test-artifacts/join-tabs-dividers-build.log`), deployed and visually verified in `test-artifacts/join-tab-dividers.jpg`. User opened Community during review; its populated list and scrollbar render. The populated screenshot exposes crowding among native right-side server status icons, which remains to be addressed separately.

Added JoinServerPresentation.cs with ServerListGui Awake, RecreateTabs and UpdateServerListGuiInternal hooks. Matches Select World with Norse title/footer, bold category tabs, full-width padded native rows, blue selection, scrollbar styling, pointed filter field and a separate brown warning panel. Connect has an independent Auga border and cached native-image suppression so disabled-state visuals cannot restore the old rectangle. Native selection, discovery and connection callbacks remain.

Added a per-row star column using native favorite list membership, Add/Remove and server-name metadata. Pooled row listeners are rebound to current entry data. Favorite mutations request native list/button refresh; removing from the Favorite category clears selection. Gold means favorite, muted means not. User accepted the favorite column and requested removal of the redundant standalone favorite control, which is now hidden after native tab updates. Favorite persistence uses the existing native lifecycle. User interacted concurrently during review; an agent click test did not establish an isolated reversible add/remove cycle, so do not claim that test passed. No agent connection or server addition was requested/invoked.

Add Server is centered in a 45-degree cutout, with the list background ending at its centerline (180; inset top 204, width 184). Filter and refresh are now at y=-206 below category tabs; list top=-252. Build passed with 0 errors and 4 existing warnings (`test-artifacts/join-server-build.log`), deployed to Auga-Dev. Final screenshot `test-artifacts/join-layout-review.jpg` verifies the requested layout, star column, absent standalone favorite button, inset and matching Connect/Back. Native Recent/Favorite contents were observed during user navigation. Logs show successful setup without new exceptions; an unavailable server-data lookup is logged by the native browser. Nested Add Server dialog, filter input/clear, long-list scrolling, controller navigation, online connection and all server status/icon combinations remain unverified. Left Join Game open for review.

## World tab click fix

WorldSelectionPresentation.Tab now creates an invisible full-size raycast Image and moves the tab above sibling content. Disabling the original button Image had removed its pointer hit target. Existing native TabHandler callbacks remain. Build passed with 0 errors and 4 existing warnings (`test-artifacts/world-tabs-build.log`), deployed to Auga-Dev. Verified mouse switching Join Game to Start Game (`tabs-return-check.jpg`) and back (`tabs-join-verified.jpg`). Join Game's inner server browser remains native and awaits styling; no server was connected or added. Left Join Game open. The Start Game screenshot also verifies the empty password placeholder now renders without overlapping Empty text.

## World-selection password field

WorldSelectionPresentation now reuses the original MainMenu StartGame/Panel/WorldPanel/ServerPassword sprite, slicing and pixels-per-unit for the pointed inset. An owned image avoids native initialization restoring the default texture. Removed the separate heading, added 28-unit text-area padding, and rendered a localized muted italic Server Password hint. A cached LateUpdate visibility check hides the native Empty placeholder and displays the owned hint only while input is empty; native input value, masking and validation remain unchanged.

Build passed (0 errors, 4 existing warnings; `test-artifacts/password-build.log`), deployed to Auga-Dev. Final live screenshot `test-artifacts/password-verified.jpg` shows the requested pointed frame, masked user-entered text and no overlapping hint. Earlier screenshot `password-final.jpg` established the frame but exposed native Empty overlap, subsequently corrected. Final empty-state rendering and focus/controller transitions have not been independently verified. User entered text and toggled server settings during review; the agent did not enter or collect the password or start a server.

## World Modifiers styling

Added WorldModifiersPresentation.cs, attached after ServerOptionsGUI.Awake. Retains native preset keys, slider/toggle values, tooltips and Done/Cancel behavior. Brown panels and corner ornaments, Norse title/footer, bold uppercase option labels and preset captions, thin slider tracks with gold diamonds, diamond toggles, and a padded description panel match the existing presentation. Presets now has flanking lines and hollow diamonds. Done required explicit styling through m_doneButton because the regular UI.Button pass did not cover its background; its owned Auga background is now visually verified alongside Cancel.

Final build passed with 0 errors and 4 existing warnings (`test-artifacts/modifiers-build.log`), deployed to Auga-Dev. Screenshot `test-artifacts/modifiers-done-check.jpg` verifies dividers, matching Done/Cancel, readable tooltip and active toggle mark. Log confirms successful setup without new exceptions. User interacted with modifier values during review; the agent did not intentionally change or apply values. The agent opened the dialog during inspection. Full preset/save/cancel semantics, controller navigation, localization, alternate resolutions and Done hover/disabled feedback remain unverified. Left World Modifiers open for review.

## World selection presentation and alignment

Added WorldSelectionPresentation.cs and its native UpdateWorldList postfix, registered from MainMenu_Setup.cs. Reuses CharacterSelectionPresentation's button/box/placement helpers and CharacterActionsInset mesh without changing the character screen's styling. World selection uses the same panel anchors, corners, Norse heading/footer, bold labels, blue selection and 45-degree action inset. Native list entries, callbacks, save indicators, modifier summaries, Crossplay, Manage Saves, World Modifiers and dialogs remain in place.

Corrected native button sibling order so Remove/New draw above the inset, native orange caption colors, and the disabled-active-tab tint. Rows now stretch to the content width, with 10-unit outer padding, explicitly aligned name/modifier/seed columns, and cloud/local icons at the right edge. The content height includes top/bottom padding for scrolling.

Build passed: 0 errors, 4 existing warnings (`test-artifacts/world-alignment-build.log`). Deployed only to Auga-Dev and visually verified with the Start world selected (`test-artifacts/world-alignment-check.jpg`): full-width blue row, aligned name/seed/icon, visible action buttons, bright active tab. Latest log records successful presentation setup without a new runtime exception. Game left at Select World. Long lists, controller navigation, tab transitions, nested dialogs and server option changes remain unverified; Join Game contents retain their native presentation. No world creation/deletion or server settings were invoked during this visual check.

## Current checkpoint: in-game recovery and FPS fix confirmed

The repaired build is deployed to Auga-Dev and running with Arjora in Start. User confirmed "fps is fixed" on 2026-09-11. The earlier recovery screenshot showed 17 FPS; `test-artifacts/ingame-fps-result.jpg` shows 120 FPS with a populated hotbar, working minimap and resource HUD, and no compatibility warning banner. These are observations, not a controlled performance benchmark. Latest build: 0 errors, 4 existing assembly-reference warnings (`test-artifacts/ingame-fps-build.log`). Latest log inspection found no repeating runtime exceptions; the misleading initial external augaassets lookup error remains, followed by successful embedded bundle loading.

Repairs: retain native chat input/focus, crosshair and key hints; skip incompatible InventoryPanel, PauseMenu, Minimap, TextInput and TextViewer replacement families including their dependent patches. Rebuild legacy TMP font assets/materials before prefab instantiation and cache fonts in LegacyText. Preserve the native hotbar slot prefab required by current HotkeyBar.UpdateIcons, and reparent the native hovered-piece author window before replacing build UI so Hud.UpdateCrosshair retains its reference. Those last two stale references caused the repeated errors and low FPS after the initial freeze was repaired. DamageText now uses the current string argument and native TMP initialization, with guarded recoloring only when a new entry is inserted.

Before recovery deployment, copied and hash-verified cloud `characters/arjora.fch` and all 10 files in the chunked `worlds/Start` directory into `test-artifacts/arjora-start-before-recovery`. The earlier first frozen run was not backed up beforehand. Recovery logs: `test-artifacts/20260911-104341.zip`. The game was closed normally for the final deployment and restarted for verification; it is left running for user review.

Native inventory/crafting, chat, map, pause and text dialogs remain intentional compatibility fallbacks, not completed Auga migrations. Combat/damage colors, building, ships, controller input and extended play remain unverified. Prior checkpoint sections below are historical; their pending DamageText/deployment notes are superseded by this recovery.

## First in-game test: frozen, terminated at user request

User selected Arjora character and Start world and entered manually on 2026-09-11. Logs confirm loading profile arjora. The game became unusable; the user requested process termination, and Valheim was force-closed and its exit verified. Diagnostics: `test-artifacts/20260911-103625.zip`; screenshot `test-artifacts/ingame-first.jpg`. No new build was deployed during this test. A planned pre-test backup had not completed before the user loaded the world; do not describe this run as backed up.

Observed root failures: legacy chat prefab has missing Fishlabs.GuiInputField and ChatWindowController.Awake fails; Chat.HasFocus then throws throughout Player input, camera, hotbar and minimap updates. InventoryGui_Awake_Patch fails in RectTransform after a replacement lookup; KeyHints replacement fails in SetGamePadBindings; Hud.UpdateCrosshair refers to a destroyed object. Legacy TMP fonts/materials fail in MaterialReference and layout. Pause-menu navigation also logs a null reference. These require repairs before the next world load.

Pending local change: DamageText_Setup.cs now binds the current string text argument, retains native TMP initialization instead of the incompatible replacement, preserves colors for newer message types, and uses a before/after count to avoid recoloring a previous entry when native code skips insertion. Build passed (0 errors, 4 existing warnings; `test-artifacts/ingame-damage-build.log`). This fix is NOT deployed or runtime verified. Gameplay work remains active; next prioritize the broken chat input and UI initialization chain. Do not restart the frozen build as if repaired.

## Character selection presentation

**Approved inset alignment:** User approved the final trapezoid on 2026-09-11. Both cuts are 45 degrees (horizontal run equals height). The list background and cutout now end at the Remove/New button centerline: bottom 140, top 164, inset width 332. This retains the top width and lets the buttons straddle the background edge. Build passed with 0 errors and 4 existing warnings (`test-artifacts/character-inset-midpoint-build.log`), deployed to Auga-Dev, visually verified in `test-artifacts/character-midpoint-start.jpg`. Character presentation initialized successfully.

Added CharacterSelectionPresentation.cs, registered it in Auga.csproj and MainMenu_Setup.cs, and exposed the existing settings corner/scrollbar helpers internally. The screen now has an Auga panel on the left, Norse heading/names, portrait rows, native death/build/craft stats, blue selection highlight, cloud/local source text, a thin scrollbar and styled native action buttons. Manage Saves remains available. Remove/New sit in an upward trapezoid cutout from the list background (narrow top, wide bottom), corrected after user review. Portraits use a temporary camera and in-memory textures; native preview setup is restored to the selected profile after capture. No portrait files are written to save folders.

Build passed with 0 errors and 4 existing warnings (`test-artifacts/character-build.log`) and deployed to Auga-Dev. Verified two character portraits, row selection and corresponding preview change, Back to main menu, and return from world selection. Final corrected inset screenshot: `test-artifacts/character-inset-corrected-start.jpg`; navigation evidence: `character-selection-tested.jpg`, `character-back-tested.jpg`. Logs show successful character presentation initialization and no portrait-capture failure. Local character directories were copied to ignored `test-artifacts/character-save-backup` before the first deployment; this does not establish a cloud-save backup. No world was loaded and no character creation/deletion/save-management operation was invoked by the agent.

Remaining validation: long lists/scrolling, controller navigation, empty profile list, New/Remove confirmation and Manage Saves workflows, other scales/languages, heavily equipped portraits. New heading/stat/source strings currently use English. Portrait capture briefly cycles native previews and caches by filename for the menu session; interruption and profile replacement deserve follow-up before release. Existing unrelated DamageText patch failure remains unresolved.

## Settings presentation

**More open chevron angles:** Increased only the previous/next ASCII glyphs' vertical scale to 1.5 in SettingsPresentation.cs, opening their angle while retaining their horizontal span and diamond backgrounds. Debug build passed (0 errors, 4 existing warnings; `test-artifacts/settings-chevron-angle-build.log`), deployed to Auga-Dev, and visually verified on the Controller settings page (`test-artifacts/settings-chevron-after.jpg`). Settings initialization logged success. No values changed during this check.

**Gameplay action widths and diamond arrows:** Updated SettingsPresentation.cs to give Delete PlayFab account and Radial Menu a shared minimum width of 240 units, expanding for the longest caption plus 48 units of padding. Existing caption size is retained. Previous/next controls now have square diamond outlines with centered ASCII `<`/`>` rendered in Source Sans Pro, preserving native callbacks and click areas. Debug build passed with 0 errors and 4 existing warnings (`test-artifacts/settings-actions-arrows-build.log`), deployed to Auga-Dev, and visually verified on Gameplay (`test-artifacts/settings-actions-arrows-start.jpg`). Both action captions fit with padding and the language arrows show diamond backgrounds. Settings setup logged success. Account deletion and language changes were not invoked; alternate languages and scales remain unverified.

**Bold uppercase settings typography:** SettingsPresentation.cs now uses Source Sans Pro Bold with TMP uppercase rendering for tab captions and option labels, retaining localized source strings. Norse headings and footer captions remain; dropdown entries, editable text and labels named as values retain their native case. Debug build passed with 0 errors and 4 existing warnings (`test-artifacts/settings-label-font-build.log`); deployed to Auga-Dev and restarted the menu-only session. Gameplay and Graphics visually verified (`settings-label-font-start.jpg`, `settings-label-font-graphics.jpg`): six tabs fit, option labels are bold uppercase, and dropdown/slider values such as Smooth and High retain their case. Selector captions such as Custom also render uppercase. Other languages and UI scales remain unverified. No setting was intentionally changed or applied during verification.

**Reference scrollbar, dropdown, tab and footer details:** Added thin dark scrollbar tracks with square beige thumbs (also applied to inactive dropdown templates), preserving native handle geometry and full click areas. Dropdowns now use compact dark fields, left-aligned text and small down-chevrons. Centered all six tab labels between decorative lines and grouped 160-unit Back/OK buttons below a thin diamond divider. Native tab callbacks and Back/OK behavior remain. Changed SettingsPresentation.cs; Debug build passed with 0 errors and 4 existing warnings (`test-artifacts/settings-reference-build.log`); deployed to Auga-Dev. Visually verified layout and scrollbar appearance (`settings-reference-closed.jpg`, `settings-reference-ready.jpg`, `settings-scrollbar-drag.jpg`). Dropdown opening was observed during inspection. Automated drag attempts did not establish a reliable scrolling result: the live UI later changed tab and scale during inspection, so further mouse automation was stopped. Scrollbar dragging, alternate-language tab fit and value application remain unverified. No settings value was intentionally changed or applied by the agent.

**Button border proportions corrected:** Settings button backgrounds now copy the original prefab's `pixelsPerUnitMultiplier` alongside its sprite and sliced-image mode. The missing scale made the ornamental ends twice as wide, distorting Back/OK and keybinding backgrounds. Existing button rectangles, labels and click areas are retained. Debug build passed with 0 errors and 4 existing warnings (`test-artifacts/settings-button-scale-build.log`), deployed to Auga-Dev, and visually verified on Keyboard & Mouse (`test-artifacts/settings-button-scale-final.jpg`). No settings or bindings changed; left this page open for review.

Added `Auga/SettingsPresentation.cs`, compiled through `Auga.csproj`, with an isolated Harmony postfix on the current `Settings.Awake`. Updated the startup message in `MainMenu_Setup.cs`. The obsolete Settings_Setup.cs remains excluded: current Valheim owns all six tabs, settings values, bindings, resolution handling and save/cancel callbacks.

Applied the supplied reference style to the current layout: flat brown panel, original corner ornaments, Norse heading/footer text, cream Source Sans labels, gold tab selection, decorative buttons, diamond toggles and slider handles, thin tracks, and dark dropdown fields/popups. Separate child graphics avoid native layout/animation overwrites; existing slider handle/fill geometry is retained. This preserves the current six-tab arrangement and scrollable graphics page rather than reproducing the old four-tab option list. Footer keeps the native Back/OK labels and behavior.

Debug build passed with 0 errors and 4 existing assembly warnings (`test-artifacts/settings-build.log`). Deployed to Auga-Dev. Visually inspected Gameplay, Keyboard & Mouse, Controller (including its diagram), Graphics, Audio and Accessibility. Verified tab switching, resolution-dropdown opening, Back to the main menu and reopening Settings. Final evidence: `settings-keyboard-verified.jpg`, `settings-back-verified.jpg`, `settings-review-start.jpg`, `settings-popup-review.jpg` under test-artifacts; earlier page captures use `settings-final-*.jpg` (some filenames lag the displayed tab, so inspect their actual contents). Final log confirms the patch and styling of 49 toggles and 23 sliders with no SettingsPresentation error. Left Graphics open with its resolution list for review. No resolution or settings changes were applied and no world was loaded.

Not verified: changing/saving/reverting values, rebinding controls, controller navigation, in-world settings, other languages/resolutions, nested Radial Menu, and timed resolution confirmation. Existing unrelated DamageText patch failure remains outstanding. Current styling is adapted to the native layout; it is not a pixel-identical recreation of the reference.

## Main-menu styling iteration

**Reference branding and text-only menu accepted:** Restored the large original Auga sprite with a spaced PROJECT heading and a thin diamond divider, using screenshot0.png as the composition reference. The user requested retaining the current Valheim 1.0 Deep North artwork; its native child bounds are measured and the entire logo scaled into the upper-right corner. No additional logo resource remains in the project. The user accepted the text-only menu after separator cleanup hid the previous backgrounds. Text-only presentation is now explicit, with transparent padded hit areas, Norse labels, gold hover/selection and amber pressed ColorTint states. Equal 179-unit hit widths and vertical spacing remain. Changed MainMenuPresentation.cs. Debug build: 0 errors, 4 existing warnings (`test-artifacts/menu-branding-build.log`); deployed to Auga-Dev. Main-menu appearance visually verified in `test-artifacts/menu-text-final.jpg`; user replied "perfect". Automated hover/press captures did not visibly establish the color transitions, so these captures alone are not interaction verification. Screen transitions, controller navigation and other resolutions/languages were not retested in this iteration. No world was loaded. Existing full-port limitations remain.

**Additional inline padding verified and accepted:** Increased horizontal padding from 24 to 32 UI units per side; common button width now uses the Norse Start Game label plus 64 units. Equal widths, centered labels and vertical spacing are preserved. Debug build passed with 0 errors and 4 existing warnings (`test-artifacts/menu-inline-padding-build.log`). Deployed to Auga-Dev and visually verified at the main menu (`test-artifacts/menu-inline-padding.jpg`). User confirmed the result is perfect. Other-language text fit remains unverified.

**Compact equal-width buttons verified:** The user's final sizing instruction replaces the temporary fixed 300-unit width. MainMenuPresentation measures the Norse Start Game label and adds 48 UI units for both ornamental ends, applying that common width to every button (163 units in the English test). Native horizontal content layout now centers the labels with equal padding. Norse font and the additional 6 units of vertical spacing are retained. Screenshot `test-artifacts/menu-compact-centered.jpg` verifies equal compact backgrounds and centered, readable labels. Changed MainMenuPresentation.cs; Debug build passed with 0 errors and 4 existing assembly warnings; deployed and verified at the main menu. No further screenshot is needed for this English-layout change. Other-language text fit remains unverified.

**Requested spacing and Norse font verified:** MainMenuPresentation now adds 6 UI units to the native vertical layout spacing and builds a fresh TMP font from the loaded Norse font source. An initial attempt to reuse a legacy TMP Norse asset failed because its material was missing; that attempt was replaced before completion. Final screenshot `test-artifacts/menu-norse-final.jpg` confirms readable Norse labels and larger gaps between all five visible menu buttons. The final run has no MainMenu styling failure or MaterialReference exception. Build: 0 errors, 4 existing warnings; deployed and left running at the main menu. Only MainMenuPresentation.cs changed for these two requests. No additional screenshot or test is required for the English menu appearance; other languages and controller behavior remain outside this visual check.

**Visible backgrounds verified in game:** `test-artifacts/menu-layout-check.jpg` shows dark Auga buttons with gold ornamental borders and Source Sans Pro labels. One-time runtime diagnostics established the actual blocker: the native layout reduced added backgrounds to 0 x 0. Setting `LayoutElement.ignoreLayout = true` lets the backgrounds stretch over their parent buttons. The new objects also inherit their parent's UI layer; that layer correction alone did not resolve the defect. An owned background alone likewise did not resolve it.

Changed `Auga/MainMenuPresentation.cs` (owned background, UI layer, layout exclusion, one-time geometry log) and `AugaUnityLib/LegacyText.cs` (inherit UI layer for generated TMP child). The final Debug build passed with 0 errors and 4 assembly-version warnings; a preceding rebuild also exposed the existing obsolete TMP word-wrapping warning. Deployed to Auga-Dev, restarted the menu-only test, verified the backgrounds visually, clicked Start Game (character selection confirmed by log), then clicked Back. No world was loaded and no saves were edited. The UI is left at the main menu. Controller navigation, other screens, and the full original Auga menu layout remain unverified/unported.

Follow-up visual inspection: `test-artifacts/main-menu-visible.jpg` shows the Source Sans Pro menu labels, but no visible Auga backgrounds. The initial styling code reused the native target Image, whose visibility can be controlled independently of the Button transition. Changed MainMenuPresentation to create an owned Image background under each current button, explicitly enabled and stretched to the button rectangle. Build passes with 0 errors and the same 4 warnings (`test-artifacts/menu-background-build.log`). Deployment and a second screenshot must be confirmed before marking this visual defect fixed.

The first user test covered menus only. Its captured log (`test-artifacts/20260911-085948/LogOutput.log`) confirms startup, embedded bundle/dependency loading, and reaching character selection on Unity 6000.0.75f1. The supplied source had its main-menu/character-selection replacement commented out, explaining the unchanged screens. DamageText_Setup failed to install because its postfix still expects the removed `dmg` parameter. The missing external augaassets message is misleading because embedded loading then succeeds. Both issues remain outstanding.

Added `Auga/MainMenuPresentation.cs`, wired from `MainMenu_Setup.cs` and included in `Auga.csproj`. It applies Auga's ButtonFancy background and Source Sans Pro semibold font to the current main-menu buttons, retaining their objects, labels, click listeners, layout and navigation. Setup logs the number of styled buttons or the full failure. This is menu styling, not the full legacy menu/character-selection replacement.

Build: 0 errors, 4 existing assembly-reference warnings (`test-artifacts/menu-style-build.log`). Deployed to Auga-Dev. Visual verification is pending: launch with Vortex, inspect the main-menu button backgrounds/font, exercise mouse hover and keyboard/controller selection, open Start/character selection and Settings, return to the main menu, then exit. Collect logs and a main-menu screenshot. Character selection and Settings remain native. Do not treat successful compilation as confirmation that the legacy button sprite or dynamic font renders correctly.

## Current checkpoint: compiled and deployed for a menu smoke test

The main projects now target **netstandard2.1** using the workspace-local .NET SDK 8.0.408. Debug builds with **0 errors and 4 warnings**. The warnings report installed game dependencies requesting newer System.IO.Compression and System.Net.Http assembly identities than the SDK's standard reference pack; runtime compatibility of those identities remains unverified. Full diagnostic build log: `test-artifacts/validated-debug.log`.

Changed framework/build files: both main `.csproj` files, `Directory.Build.props`, `Directory.Build.targets`, `NuGet.Config`, and build/restore/environment scripts. The old targeting pack and compiler downloads remain ignored local artifacts but are no longer used.

Source compatibility changes:

- `AugaCraftingPanel.cs`, `CraftingRequirementsPanel.cs`, `PlayerInventory_Setup.cs`: current recipe properties and inventory element positions/components.
- `AugaCharacterSelect.cs`, `SelectedCharacterInfo.cs`: profile GetStat, current cloud-storage status and local character folder API for portraits.
- `AugaBindingDisplay.cs`, `ChatWindowController.cs`: current input bindings and scroll API.
- `LegacyText.cs`, `CraftingRequirementsPanel.cs`, `Hud_Setup.cs`, `PlayerInventory_Setup.cs`: runtime TMP adapters preserve serialized legacy Text fields and supply the current game's TMP references. Rendering/font conversion requires in-game validation.
- `AugaLog_Hooks.cs`, `Hud_Setup.cs`, `PauseMenu_Setup.cs`: current spawn/biome/portal APIs, building category names and menu button fields.
- `Minimap_Setup.cs`: GUIFramework namespace and direct input-submit listener replace the old IL transpiler. The removed right-click method has no direct replacement wired here; map secondary actions require testing and further migration.
- `MainMenu_Setup.cs`, main project compile list: retain native settings, excluding legacy settings patches until the new Valheim.SettingsGui architecture is ported. `Settings_Setup.cs` is preserved on disk.
- `PlayerInventory_Setup.cs`: retain native SplitDialog instead of replacing it with the old prefab.
- `Auga.cs`: remove obsolete 0.217 version gate/sleep; log startup versions, explicit dependency/bundle failure, and patch-group outcomes. Patch installation failures remove that class's partial patches and log the full exception. Runtime failures inside successfully installed patches are not automatically repaired.

Verified: SDK restore's already-present path, environment validation, Debug compile, embedded `Auga.augaassets`, `Auga.Unity.Auga.dll`, `Auga.fastJSON.dll`, and no references to `_publicized` assembly identities. Script/project/task syntax checked. No runtime feature is marked working.

Deployed Debug `Auga.dll` and translations only to the configured `BepInEx/plugins/Auga-Dev` folder. Valheim was closed. No other Auga DLL was found; other installed plugins were DisplayBepInExInfo and CameraMod, so this is not yet a dependencies-only smoke test. No game launch or save loading was performed.

Next user test: launch normally, reach the main menu, open/close Settings, open character selection, return to the menu, and exit. Do not select or load an important save. Collect the complete BepInEx LogOutput.log using the workspace task and capture screenshots of any broken screen. Asset-bundle script bindings, font conversion, patch execution, input, HUD/inventory/crafting, external mod integrations and controller navigation all remain unverified. Settings and split dialogs intentionally remain native for this checkpoint. Earlier sections below are historical baseline evidence.

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

### Slot state indicators (2026-09-11)
- Restored blue equipped corners independently of octagonal selection highlighting, using native equipped bindings.
- Added green corners driven by the local player's active foods, and original gold diamond art behind native quality labels.
- Build: zero errors, four existing warnings; deployed to Auga-Dev and restarted. Inventory state transitions and rendered marker alignment await in-game inspection.
- Build menu also has inset Q/E/F prompts, octagonal slots, and increased panel height; final rendered layout check pending.

### First-hover item tooltip (2026-09-11)
- UITooltip.OnHoverStart now resolves the current inventory item and prepares the Auga prefab before native tooltip creation; cached vanilla tooltips are cleared for inventory item hovers.
- Covers player and container InventoryGrid slots and controller hover entry through the same method.
- Build passed (zero errors, four existing warnings); deployed and restarted. Fresh first-hover and subsequent item-switch runtime checks pending.

### Crash investigation and tooltip re-entry removal
- Preserved Player/BepInEx logs under test-artifacts/tooltip-crash-*.log. Minidump reports native access violation 0xC0000005 in UnityPlayer after spawn; no managed trace confirms the originating mod or method.
- Removed CreateItemTooltip/Set call from OnHoverStart prefix because Set can re-enter OnHoverStart. Item/prefab/text are now assigned directly before native creation.
- Build passed, zero errors/four existing warnings; redeployed and restarted. Crash resolution and first-hover behavior remain pending runtime verification.

### Second spawn crash: hover hook rolled back
- Second native crash reproduces the same UnityPlayer stack immediately after Arjora spawns; logs preserved as test-artifacts/tooltip-crash2-*.log.
- Removed UITooltip_ItemHover_Patch entirely, restoring pre-first-hover-fix tooltip behavior. No additional speculative fix included.
- Build passed (zero errors, four existing warnings); rollback deployed and game relaunched. Stability requires a fresh load check. First-hover vanilla tooltip issue remains open.

### Tooltip consistency through slot initialization
- InventoryElement.Initialize postfix assigns Auga tooltip prefab and ItemTooltip binding before pointer interaction. InventoryGrid.UpdateGui postfix refreshes/clears item bindings for populated/reused/empty slots.
- No OnHoverStart hook or tooltip destruction added. Existing CreateItemTooltip binding remains.
- Build passed with zero errors/four existing warnings; deployed and restarted. Verify first hover, moving between items, reopening inventory and container slots; stability and consistency not yet confirmed in game.

### Native crash isolation, 2026-09-11
- User reports third crash while walking/picking up items. This run had slot-initialized tooltips, no hover hook, and no new minimap changes deployed.
- Parsed exception records/module lists in all three minidumps: actual faults are mono-2.0-bdwgc.dll +0x4EDE70 (first/third) and +0x4EDFF9 (second), read access violations. Text log UnityPlayer stack does not identify the root cause. Earlier attribution to tooltip re-entry was unconfirmed.
- Temporarily renamed Auga-Dev/Auga.dll to Auga.dll.crash-isolation-disabled and launched comparison run. Other plugins untouched. Restore this exact DLL before any normal deployment; do not confuse locally built minimap changes with the last tested build.
- Minimap reference artwork changes compile but remain LOCAL/UNDEPLOYED pending stability diagnosis. First diagnostic: walk/pick up items without Auga, then inspect new logs/crash report.

### Auga restored for installed Valheim 1.0.12
- Latest no-Auga log shows character selection followed by normal shutdown, with no new crash report. No gameplay stability conclusion can be drawn from this latest run.
- Refreshed publicized build references from installed 1.0.12 assemblies; build passed with zero errors/five warnings (including existing obsolete TMP API warning). Log: test-artifacts/deploy-1.0.12-build.log.
- User requested deployment: deployed current Auga.dll, including pending minimap reference artwork, biome caption, wind indicator and effect placement. Launched via Steam. Prior isolation DLL remains preserved with non-DLL extension in Auga-Dev.
- Native-crash cause still unresolved; updated-game startup and gameplay/minimap verification pending.

### In-game minimap clock (2026-09-12)
- Added centered HH:MM display in the top-screen/minimap gap using EnvMan.GetDayFraction, with zero-padded 24-hour formatting and updates only when displayed minute changes.
- Follows actual small-map position and hides with it; uses Auga typography/color and ignores pointer input.
- Build passed (zero errors/four existing warnings), deployed and relaunched. Visual placement and runtime time progression await in-game verification.

### Clock setting and matching ornaments (2026-09-12)
- Gameplay settings now include SHOW CLOCK after Show Key Hints, styled with other toggles. Defaults enabled; saved through Settings.ApplyAndClose to Gameplay/ShowClock config. Back/cancel leaves saved state unchanged.
- Clock reuses the biome caption's authored line/diamond ornaments and hides the complete decoration when disabled.
- Build passed, zero errors/four existing warnings; deployed and relaunched. Gameplay row placement, save/cancel persistence and clock ornament rendering await visual/runtime verification.

### Combat mouse hint icons (2026-09-12)
- Added CombatMouseHints, retaining native KeyHints and replacing Mouse-1/2/3 keycap visuals with original HUD mouse sprites. Keyboard prompts remain native; if binding text changes to keyboard, original text/background return.
- Scoped to combat hints; no inventory, build or gamepad hint replacement.
- Build passed with zero errors/four existing warnings; deployed and restarted. Attack/secondary/block/dodge rendering and remapped-key fallback require in-game verification.

### Inventory/crafting mouse hint icons (2026-09-12)
- Reused mouse hint renderer for native inventory and inventory-with-container hint groups, including crafting-screen hints. Keyboard modifiers remain native.
- Build passed (zero errors/four existing warnings); deployed and relaunched. Visual verification pending.

### Hammer/build mouse hint icons
- Extended the shared Mouse-1/2/3 icon renderer to native build hints, retaining keyboard prompts.
- Build passed (zero errors/four existing warnings), deployed and restarted. Build-screen visual check pending.

### Pickup crash targeted isolation (2026-09-12)
- User identifies item pickup as consistent trigger. Latest inspected running-session log contains no managed exception; latest existing crash folder is Crash_2026-09-11_213245876.
- MessageHud now retains native Awake, serialized TMP bindings and ShowMessage notification queue. Removed legacy two-object replacement and duplicate Auga top-left popup controller dispatch. AugaMessageLog attached to native MessageHud so existing log hooks continue functioning.
- Targeted isolation, not confirmed crash fix. Build passed (zero errors/four warnings), deployed/restarted. User must repeat item pickups to establish result.
- Lighter mouse icons and compact plus-sign spacing remain pending; no changes for those were deployed in this iteration.

### Auga notifications restored without replacing MessageHud
- Native MessageHud retains Awake, queue, TMP bindings, unlock/biome handlers and logs. Only Auga's visual notification prefab is instantiated through its controller on the native object; duplicate native top-left graphics are hidden.
- Fixed stack-cap eviction before merging, matching by text plus icon, clearing stale icons, and immediate fade reset on repeated pickup.
- Build passed (zero errors/five existing warnings), deployed/restarted. Pickup crash resolution remains unconfirmed; repeat individual and rapid pickups, verify merged counts/fade and HUD hide behavior.
- Lighter mouse graphics and plus-sign spacing remain pending from before crash investigation.

### Notification visibility correction
- User reports no crash but no pickup popups. Notification container now explicitly attaches to the canvas rendering native message text, rather than MessageHud's parent. If canvas unavailable, retain native visuals with diagnostic error.
- Log also contains native occupied-slot -1,-1 errors during some pickup attempts; inventory insertion issue remains separate/unresolved.
- Build passed, deployed/restarted. Verify visible notifications on successful pickups; no runtime confirmation yet.

### Notification visibility confirmed; center text styled
- User confirms pickup notifications work after canvas attachment fix. Earlier pickup test also reported no crash.
- Center-screen messages now use Auga SourceSansProBold and TMP uppercase styling (including NO ROOM IN INVENTORY). Native message content, timing and color retained.
- Build passed (zero errors/four warnings), deployed/restarted. Center text visual check pending.

### Build hint column, portal tag and workbench presentation
- Build keyboard/mouse hints arranged vertically with controls before uppercase captions. Existing native bindings/input-layout visibility retained. Bright mouse sprite selection and compact keycap width work remain pending.
- Portal/sign TextInput styled as compact brown panel with title separators and pointed input; native text receiver/submit/cancel retained.
- Shared inventory frame switches to station header/icon/level, repair control and original Craft/Upgrade tab art when station open. Native button callbacks retained. Original crafting/upgrade/max-quality requirement panels switch with native mode, with quality labels, station icon and resource wire states. ComplexTooltip retains existing item comparison behavior.
- Build passed (zero errors/four warnings), deployed/restarted. All three presentations need live review; no actual craft/upgrade/repair action was automated.
- Compendium reference screenshots received; async question whether to restore these screens remains unanswered. Pickup notifications user-confirmed working; center notification font change deployed earlier.

- Workbench repair button: increased whole control scale to 1.6, enlarging diamond, glyph and hit area together. Build passed; deployed/restarted; visual check pending.

### Chest reference layout and footer
- Chest uses a compact panel sized to its native grid, centered octagonal slots, uppercase Auga title with separators and enclosed footer.
- Footer: TAKE ALL left, centered weight/bag between equal divider segments, PLACE STACKS right. Native inventory/button bindings retained; previous protruding weight visuals hidden.
- Build passed (zero errors/four warnings), deployed/restarted. Visual layout, larger-container sizing and transfer controls await runtime verification.

### Chest alignment and clipping correction
- Grid content now directly parents to its grid rect to avoid the old intermediate viewport offset/clipping. Explicitly activated divider clones and selected actual small-button sprite image.
- Chest matches inventory world-space left/right bounds and sits 20 UI units below it.
- Build passed (zero errors/four warnings), deployed/restarted. Live alignment/clipping/artwork verification pending.

- Chest remaining clipping: inspected native InventoryGrid.UpdateGui IL and identified cached creation-time centering offset (old grid width /2 minus widget width /2). LayoutChest now assigns each element's top-left anchors/pivot and position from Position.x/y * m_elementSpace after centering the grid. Build passed, deployed/restarted; final visual check pending.

### Split Stack presentation
- Restyled current SplitDialog panel/title/text, action buttons and slider with Auga colors, corner artwork and gold diamond handle. Native split limits, keyboard/controller input and accept/cancel callbacks retained.
- Build passed (zero errors/four warnings), deployed/restarted; appearance and split action verification pending.

### Split stack slot / chest weight alignment
- Split-stack buttons now use Nordic font at 24; the native icon backing plate uses the inventory octagon sprite, preserving the item icon.
- Chest weight and bag are centered as a group using the current text width, maintaining equal footer divider gaps.
- Build: test-artifacts/split-slot-weight-build.log, zero errors and four existing assembly warnings. Deployed to Auga-Dev and restarted; in-game visual confirmation pending.

### Workbench repair diamond alignment
- Enlarged repair control by 25 percent (scale 1.6 to 2), moved its center up to the header divider centerline (-98), and shifted left 6 units to account for the wider diamond.
- Build passed: test-artifacts/repair-diamond-alignment-build.log (0 errors, 4 existing assembly warnings). Deployed and restarted; visual confirmation pending.

### Minimap size increase
- Increased map from 240x240 to 264x264 (10 percent per dimension), preserving top-right margin. Recentered biome caption and moved status effects down to preserve spacing; clock follows the map center.
- Build passed with 0 errors and 4 existing warnings (test-artifacts/minimap-size-build.log). Deployed and restarted; in-game visual verification pending.

### Status effects spacing below minimap
- Moved the status-effect container down 20 UI units to increase clearance below the biome label.
- Build passed (0 errors, 4 existing warnings): test-artifacts/minimap-effects-spacing-build.log. Deployed and restarted; visual confirmation pending.

### Status effects position override fix
- Found MovableHudElement.Update resetting the minimap effect position every frame to saved baseline (-40,-330), overriding earlier one-time changes.
- Added runtime LayoutOffset (zero for other HUD controls), set only for status effects to preserve minimap clearance and custom position adjustments.
- Build passed: test-artifacts/status-effects-offset-build.log, 0 errors and 4 existing warnings. Deployed and restarted; visual confirmation pending.

### Crafting footer space
- Moved Craft and progress/cancel group down 50 UI units into available bottom space. Expanded TooltipScrollContainer downward by 40 while retaining its top edge; auto-hide its scrollbar when content fits.
- Checked source prefab bounds before adjusting. Build passed: test-artifacts/craft-footer-space-build.log, 0 errors and 4 existing warnings. Deployed and restarted; actual recipe overflow visual confirmation pending.

### Restore original message-log tab
- Hourglass now selects a clone of original TabContent_MessageLog, with original AugaMessageLogController and MessageLogElement visuals; removed erroneous native Compendium callback. Shared header uses localized Message Log title; workbench and other tabs hide the log.
- Guarded original log controller unsubscribe when the message HUD is unavailable during teardown.
- User reiterated preserving and adapting as much original Auga as possible; continue preferring original panels, controllers and assets with narrow current-Valheim compatibility changes.
- Build passed: test-artifacts/message-log-tab-build.log (0 errors, 5 existing warnings). Deployed and restarted; in-game tab interaction validation pending.

### Original Auga pause menu
- Added independent PauseMenuPresentation patch. Clones only original MenuEntries (Nordic text, pale dividers, bottom Close Menu) while retaining current native Menu controller, confirmations and action callbacks.
- Rebound native button fields for save/cooldown, intro skipping, player list, settings and exits. Adapted Invite with original Auga button visuals. Compendium entry opens current InventoryGui texts dialog; original Auga Compendium content still needs restoration separately.
- Layout follows active native actions; navigation includes Compendium. Original native saved-time text remains bound but hidden with old menu visuals.
- Build passed: test-artifacts/pause-menu-original-build.log, 0 errors and 4 existing warnings. Deployed and restarted; pause menu rendering/actions need in-game verification.

### Pause menu top divider spacing
- Lowered the original top divider by 40 UI units to tighten its gap above the first visible menu action.
- Build passed: test-artifacts/pause-divider-spacing-build.log (0 errors, 4 existing warnings). Deployed and restarted; visual confirmation pending.

### Pause menu crosshair visibility
- Hide normal and bow crosshair after native HUD updates while the pause root is active, including pause dialogs. Native UpdateCrosshair restores its normal gameplay state after closing.
- Build passed: test-artifacts/pause-crosshair-build.log (0 errors, 4 existing warnings). Deployed and restarted; visual confirmation pending.

### Original Auga Compendium restoration
- Replaced pause Compendium callback to InventoryGui.OnOpenTexts with lazy creation/showing of the original AugaCompendiumController panel from MenuPrefab.
- Clone is staged inactive while adapting native TextsDialog TMP title/body bindings, private list-row prefab TMP names and scroll references. Keeps original Hugin/Lore filters, tab controller, trophies, drops, stat blocks and close behavior. Native Menu remains active and its settings-instance guard is used by the original controller.
- Scoped UpdateTextsList prefix restores original known-text filtering only for AugaTextsDialogFilter dialogs. Added legacy-layout preferred heights for TMP-backed text.
- Build passed: test-artifacts/auga-compendium-build.log, 0 errors and 4 existing warnings. Deployed and restarted; original Compendium opening, tab switching and closure await in-game validation.

### Compendium inactive scroll lookup fix
- User reported no action; live BepInEx log recorded NullReferenceException in CompendiumPresentation.Create on each click.
- Found the staged inactive panel was searched using GetComponentInParent without includeInactive; its parent ScrollRect was excluded. Resolve the left scroll from GetComponentsInChildren(true) by its content binding instead.
- Build passed: test-artifacts/compendium-inactive-scroll-build.log (0 errors, 4 existing warnings). Deployed and restarted. Runtime check in progress.

### Compendium live verification and follow-up fixes
- Confirmed in live capture compendium-open-check.jpg that Compendium opens and Hugin article renders after inactive-scroll lookup fix. Observed blank/narrow left rows and unresponsive tabs.
- Added scoped FillTextList postfix restoring local row scale/width/height and TMP visibility; list labels allow overflow within their single-line rows.
- Activated original Compendium UIGroupHandler, assigned first tab as default, set priority above pause-menu groups, and ignored ancestor CanvasGroups. Rebound native pause group's stale Continue default to original Auga Close Menu button.
- Latest build/deploy: test-artifacts/compendium-focus-rows-build.log, 0 errors, 4 existing assembly warnings plus one obsolete TMP wrapping API warning. Restarted game. Follow-up row/focus changes still require live verification.
- Separate existing startup error observed: Auga.BuildHintLayout.Start NullReferenceException, unrelated to Compendium; not addressed in this change.

### Compendium scroll-list input groups
- User confirmed tabs work but trophy/lore rows cannot be clicked. Original Compendium prefab contains four UIGroupHandlers: prior fix only activated/raised the root, leaving nested list groups below its priority.
- Set every original Compendium group to the same priority above pause-menu groups and explicitly activate all groups. Current UIGroupHandler.IsHighestPriority accepts equal priorities, preserving simultaneous header/list input.
- Build passed: test-artifacts/compendium-list-input-build.log (0 errors, 5 warnings). Deployed and restarted; list-click validation pending.

### Compendium list disappearing after selection
- Inspected current TextsDialog.FocusOnCurrentLevel/SnapTo IL: after two frames, native SnapTo assigns both list axes from the selected image position and width. Original Auga selected child is centered in a vertical row, so this introduces a horizontal shift.
- Scoped SnapTo prefix for AugaTextsDialogFilter dialogs fixes list x at zero, adjusts y only to reveal a selected row outside the viewport, clamps vertical bounds and stops old scroll velocity. Native dialogs remain unaffected.
- Build passed: test-artifacts/compendium-selection-scroll-build.log (0 errors, 5 warnings). Deployed and restarted; in-game repeated-selection validation pending.

### Original Auga logout and quit confirmations
- Clone original MenuPrefab LogoutConfirm and ExitConfirm visual roots. Rebind Yes/No to current native Menu callbacks and update native dialog references, keeping current save/logout/quit handling.
- Activate original dialog input groups above pause groups and select No by default. Preserves original localized wording, fonts, border art and buttons.
- Build passed: test-artifacts/auga-exit-confirmations-build.log (0 errors, 5 warnings). Deployed and restarted. Dialog appearance/cancel behavior needs in-game validation; accept actions were not exercised.

### Compendium mouse-wheel speed
- Increased all original Compendium ScrollRects from 40 to 120 sensitivity (3x), covering entry lists and detail panes.
- Build passed: test-artifacts/compendium-wheel-speed-build.log (0 errors, 5 warnings). Deployed and restarted; scroll feel awaits user confirmation.

### Skills mouse-wheel speed
- Increased original Skills-tab ScrollRect sensitivity from 40 to 120, matching the Compendium's 3x wheel speed.
- Build passed: test-artifacts/skills-wheel-speed-build.log (0 errors, 5 warnings). Deployed and restarted; scroll feel awaits user confirmation.

### Skills scroll speed 6x
- Raised Skills scroll sensitivity to 240 (6x original 40); Compendium remains at 120.
- Build passed: test-artifacts/skills-wheel-speed-6x-build.log (0 errors, 5 warnings). Deployed and restarted; user confirmation of scroll feel pending.

### Slot reference details
- Shared native slot styling now uses Norsebold 18 for binding numbers, inset at upper left; stack counts use SourceSansProBold 12 centered near the bottom.
- Durability bar is a 42x3 cream line, centered 10 units above slot bottom. Retains native GuiBar value and visibility updates, removes track artwork, updates cached full width and template width for clone initialization.
- Build passed: test-artifacts/slot-reference-details-build.log (0 errors, 5 warnings). Deployed and restarted; inventory/hotbar visual confirmation pending.

### Item artwork inside octagonal slots
- User's arrow screenshot shows native item artwork extending past the lower-left corner. Inset the native icon Image to 14-86 percent on both axes, reset local scale and preserve sprite aspect ratio. The resulting square fits within the slot octagon's clipped corners without reparenting native icon bindings or clipping badges/text.
- Build passed: test-artifacts/slot-icon-inset-build.log (0 errors, 5 warnings). Deployed and restarted; visual confirmation pending.

### Mouse prompt contrast and spacing
- Added pale UI Outline to original Auga mouse icon artwork for visibility over terrain. This uses the existing Auga sprites, not replacement native map sprites.
- Mouse-only bindings now use compact 26-unit text / 30-unit keycap layout widths, reducing whitespace beside labels and plus signs. Original layout widths and rect sizing are restored for keyboard rebindings.
- Build passed: test-artifacts/mouse-hint-outline-spacing-build.log (0 errors, 5 warnings). Deployed and restarted; visual spacing confirmation pending.

### Combat prompts as a column
- Added CombatHintColumn to the native combat hints only. Keyboard/mouse prompts now use a vertical layout with 30-unit rows and 6-unit gaps, anchored at the lower right of their existing container. Each action keeps its binding/combo row intact.
- Replaced only the old layout component to avoid Unity's conflicting LayoutGroup restriction.
- Build passed: test-artifacts/combat-hint-column-build.log (0 errors, 5 warnings). Deployed and restarted; in-game layout verification pending.

### Crafting and inventory prompts as a column
- Applied the existing combat column layout to native inventory and inventory-with-container hints. Each action retains its complete key combination; column height now reserves 30 units per row plus 6-unit gaps for the larger action list.
- Build passed: test-artifacts/crafting-hint-column-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; in-game appearance awaits verification.

### Right-aligned keys and action columns
- Combat, inventory and container hint rows retain native key combinations, followed by right-aligned action captions. Shared caption width adapts to localized text, aligning the right edges of both columns.
- Build passed: test-artifacts/hint-two-columns-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; visual verification pending.

### Nordic action captions
- Combat, inventory and container action captions now use Norsebold with uppercase styling. Shared right-aligned column widths continue to measure the styled captions.
- Build passed: test-artifacts/hint-nordic-actions-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; in-game font appearance awaits verification.

### Left-aligned hint columns
- Keys and uppercase Nordic action captions now share separate left edges. Per-row spacers pad shorter key combinations to the widest combination while preserving native binding objects; widths adapt to localization and binding changes.
- Build passed: test-artifacts/hint-left-columns-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; in-game alignment awaits verification.

### Chest combo action identification
- Screenshot showed plus separators styled and moved into the action column while Split stack/Transfer remained among keys. Caption selection now excludes standalone plus separators and selects the final direct action text. Native modifier/plus/mouse ordering is preserved.
- Build passed: test-artifacts/chest-hint-captions-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; chest appearance needs in-game verification.

### Hoe and building hint columns
- Replaced the separate BuildHintLayout (live log confirmed Start NullReferenceException) with the shared two-column layout used by combat/inventory. Hoe and hammer bindings now use separate left-aligned key/action columns and uppercase Norsebold captions.
- Removed the obsolete layout implementation that attempted to add a competing LayoutGroup while leaving the previous one attached.
- Build passed: test-artifacts/hoe-hint-columns-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; in-game hoe/building appearance awaits verification.

### Build menu filter and repair clearance
- Category ScrollRect now reserves space below the active native search field and repair button, using their actual bounds plus a 12-unit gap. The list and scrollbar move together; the bottom edge is preserved. Insets are recalculated from the original offset to avoid accumulated movement.
- Build passed: test-artifacts/build-filter-repair-clearance-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; overlap correction needs in-game verification.

### Minimap wind slot alignment
- Original MapBorder retained a centered 240-unit rectangle after the map grew to 264, shifting WindCorner away from the separately anchored indicator. Stretch the border to the map and parent the indicator at WindCorner's center; native wind rotation remains bound to the indicator alone.
- Build passed: test-artifacts/minimap-wind-slot-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; in-game visual verification pending.

### Main menu and black loading reference
- Main menu uses a larger upper Auga logo, Project heading, lower Nordic menu, and original MenuButton Knots on hover/selection. Native callbacks and newer entries retained; native corner logo hidden to match reference.
- Main-menu loading root now contains the original Auga Loading prefab visuals, including the Nordic label and dividers, while native transition ownership remains unchanged.
- Build passed: test-artifacts/menu-loading-reference-build.log (0 errors, 5 warnings). Deployed and restarted. Initial main-menu screenshot verified ornaments and layout (main-menu-reference-check.jpg); final larger logo and loading transition verification pending.

### Crafting and upgrade list reference
- Shared recipe rows use aspect-preserving 28-unit icons, a fixed 36-unit text inset, left-aligned SourceSansProBold 14 names, and two-line wrapping within the existing 34-unit rows. Exceptionally long names ellipsize. Name styling now targets only native name text, preserving separate QualityLevel badge geometry. Selection uses a muted brown fill matching the reference.
- Build passed: test-artifacts/recipe-list-reference-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; in-game list appearance needs verification.
- Final main-menu capture encountered character selection because the user had already navigated away; it does not verify the final logo dimensions or loading transition.

### Equal recipe highlight margins
- Moved the 166-unit recipe list to x=0 inside the already 18-unit-inset crafting panel. Highlight now has 18 units to the outer panel edge and 18 units to the divider at crafting x=184, shared by crafting and upgrading.
- Build passed: test-artifacts/recipe-highlight-margins-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; visual confirmation pending.

### Crafting and upgrade diagram item tooltips
- Added native UITooltip plus the existing Auga ItemTooltip/InventoryTooltip presentation to populated diagram item and resource icons. Empty icons do not receive hover hits. Current items retain live item data; upgraded quality/selected craft variant use cached preview clones, never mutate inventory items. Tooltip closes when its binding changes.
- Build passed: test-artifacts/crafting-diagram-tooltips-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; hover behavior and upgraded stats need in-game verification.

### Equipped triangle alignment
- Blue equipped marker now uses the same two-unit lower-left inset as the octagonal slot background. Set pivot before offsets to prevent pivot changes shifting the marker after placement; marker remains 9x9.
- Build passed: test-artifacts/equipped-triangle-alignment-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; in-game alignment confirmation pending.

### Crafting tooltip repeat hover
- Diagram tooltip components now reacquire native hover when the pointer is over the actual topmost slot after binding/tab changes, without requiring a fresh pointer-enter event. Uses a scoped UI raycast and native OnHoverStart (no global hover patch). Clears owned tooltip on disable and makes its display graphics non-raycasting to prevent pointer interception.
- Build passed: test-artifacts/crafting-tooltip-repeat-hover-build.log (0 errors, 5 warnings). Deployed to Auga-Dev and restarted Valheim; repeated hover across craft/upgrade tabs needs user verification.

### Dedicated diagram hover targets
- Original GenericCraftingRequirements contains raycasting outline/background graphics after its icon. Moved tooltip ownership to transparent last-sibling hover targets matching icon bounds, and disabled raycasts/legacy tooltips on cloned requirement artwork. Reuses scoped native hover recovery.
- Added workbench icon tooltip using original Auga SimpleTooltip with station name and localized level. Item/resource slots continue using Auga InventoryTooltip and upgrade preview data.
- Build passed: test-artifacts/diagram-hover-targets-build.log (0 errors, 5 warnings). Deployed and restarted; repeated hover and tab-switch runtime verification still pending.

### Auga API 2.0.0 SDK synchronization
- Replaced the external Harmony self-patching shim with lazy optional runtime discovery, exact-overload reflection forwarding, API DTO/enum-array conversion, and original exception propagation. Generated external signatures/reference from runtime API.cs; common contract and palette compile into both builds.
- Added readiness/version/capability probes, native inventory visibility root and built-in tab access, Nordic/TMP font helpers, removable tooltip subscriptions, and current-game tooltip topic/text setup with stale typed data cleared.
- Documented all public signatures, optional/direct integration, ownership, lifetime, tooltip hover requirements, builds, and compatibility. Legacy custom tab/results/variant injection still depends on the retired controller and is explicitly unavailable in this port; preserved old return behavior and exposed capability checks.
- Converted AugaApiExample to an SDK project with source-embedded bridge, readiness-based setup, scene recreation, and cleanup; removed obsolete ILRepack wiring.
- scripts/Build-AugaApi.ps1 -Package passed: runtime/shim/example builds, compiled signature/default/DTO/palette parity, 13 isolated bridge checks. SDK-contained example also built against installed non-publicized managed references. Existing assembly conflict warnings remain; runtime build also has existing TMP wrapping obsolete warning.
- Shareable local artifact: .build/AugaAPI-SDK-2.0.0.zip. Package excludes game/runtime DLLs and uses an explicit manifest. No publication or API/example deployment performed. Unity runtime interaction of new API/example remains to be checked in-game.

### Useful legacy API restoration
- Supersedes the unavailable-tab statement above: custom player/workbench tabs, vanilla-mirrored workbench tabs, results panels, and custom variant dialogs now have implementations in the current InventoryPresentation. The retired controller stays disabled. Custom workbench tabs hide the native craft action to avoid crafting a previously selected native recipe.
- Registration uses stable IDs, returns existing registrations on duplicate calls, isolates custom workbench content, and preserves the legacy signatures and writable Text title contract. Capability probes now report these extensions when inventory is ready.
- docs/LEGACY-API.md documents restored behavior, ownership, limits, lifecycle, and genuinely superseded integration mechanisms. The SDK example demonstrates player/workbench tabs and variants.
- Full SDK validation passed: runtime/shim/example builds, compiled contract parity, and 13 bridge checks (test-artifacts/api-restored-sdk-validation.log). Packaged example also built against installed non-publicized references (test-artifacts/api-restored-sdk-example.log). Existing assembly/TMP warnings remain.
- No API deployment or publication performed. Valheim is closed; restored Unity tab/dialog interactions have not been tested in-game.

### Remove-character confirmation presentation
- Styled the separate native remove-character dialog with Auga background/corners, Nordic text, and original fancy button artwork. Native labels, character name, button placement, navigation, and callbacks are retained.
- Build passed with zero errors and six warnings (test-artifacts/remove-character-dialog-build.log). Deployed to Auga-Dev and restarted Valheim. Visual confirmation remains pending; no character deletion was exercised.

### Remove-character confirmation hover
- Replaced native SpriteSwap with Auga color tint for hover, selection, and press; cleared native sprite overrides so both buttons retain ornate artwork.
- Build passed (zero errors, five existing warnings), deployed to Auga-Dev and restarted. Hover appearance still requires visual verification.

### Button hover style audit
- Centralized ornate button states: clear native SpriteSwap overrides and use Auga hover/selection/press tints. Applied to shared character/world actions, character deletion, UnifiedPopup confirmations, save actions, inventory actions, and Add Server dialog actions. Character creation now targets its visible border.
- Build passed with zero errors and five existing warnings (test-artifacts/hover-style-audit-build.log). Deployed to Auga-Dev and restarted. Visual checks across these screens remain pending.
