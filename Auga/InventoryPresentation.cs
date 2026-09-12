using System;
using System.Collections.Generic;
using System.Linq;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Auga.CharacterSelectionPresentation;

namespace Auga
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    public static class InventoryPresentationPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (__instance.GetComponent<InventoryPresentation>() == null)
                __instance.gameObject.AddComponent<InventoryPresentation>();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
    public static class InventoryRecipePresentationPatch
    {
        private static void Postfix(InventoryGui __instance) => __instance.GetComponent<InventoryPresentation>()?.StyleRecipes();
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class InventoryGridLayoutPatch
    {
        private static void Postfix(InventoryGrid __instance)
        {
            __instance.GetComponent<InventoryReferenceLayout>()?.Apply(__instance);
            foreach (var element in __instance.m_elements)
            {
                var tooltip = element.m_tooltip;
                if (tooltip == null) continue;
                var data = tooltip.GetComponent<ItemTooltip>();
                if (data == null) continue;
                data.Item = __instance.m_inventory?.GetItemAt(element.Position.x, element.Position.y);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryElement), nameof(InventoryElement.Initialize))]
    public static class InventorySlotTooltipPatch
    {
        private static void Postfix(InventoryElement __instance)
        {
            var tooltip = __instance.m_tooltip;
            if (tooltip == null || Auga.Assets.InventoryTooltip == null) return;
            tooltip.m_tooltipPrefab = Auga.Assets.InventoryTooltip;
            if (tooltip.GetComponent<ItemTooltip>() == null)
                tooltip.gameObject.AddComponent<ItemTooltip>();
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
    public static class InventoryItemPresentationPatch
    {
        private static void Prefix(ItemDrop.ItemData item, UITooltip tooltip)
        {
            var data = tooltip.GetComponent<ItemTooltip>() ?? tooltip.gameObject.AddComponent<ItemTooltip>();
            data.Item = item;
            tooltip.m_tooltipPrefab = Auga.Assets.InventoryTooltip;
        }
    }

    public sealed class CraftingDiagramTooltip : MonoBehaviour
    {
        private ItemDrop.ItemData _source;
        private int _quality, _variant;
        private UITooltip _tooltip;
        private ItemTooltip _data;
        private readonly List<UnityEngine.EventSystems.RaycastResult> _hits = new List<UnityEngine.EventSystems.RaycastResult>();
        private UnityEngine.EventSystems.PointerEventData _pointer;
        private UnityEngine.EventSystems.EventSystem _events;

        private void LateUpdate()
        {
            if (_tooltip == null || !_tooltip.enabled ||
                ZInput.IsExclusiveGamepadActive() || ZInput.IsTouchActive()) return;
            if (UITooltip.m_current == _tooltip)
            {
                // A displayed tooltip must not steal the next enter/exit from its source slot.
                if (UITooltip.m_tooltip != null)
                    foreach (var graphic in UITooltip.m_tooltip.GetComponentsInChildren<Graphic>(true))
                        graphic.raycastTarget = false;
                return;
            }

            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events == null || !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, ZInput.pointerPosition)) return;
            if (_pointer == null || _events != events)
            {
                _events = events;
                _pointer = new UnityEngine.EventSystems.PointerEventData(events);
            }
            _pointer.position = ZInput.pointerPosition;
            _hits.Clear();
            events.RaycastAll(_pointer, _hits);
            // Only reacquire the actual topmost slot, never through another panel or modal.
            if (_hits.Count > 0 && _hits[0].gameObject.transform.IsChildOf(transform))
                _tooltip.OnHoverStart(gameObject);
        }

        private void OnDisable()
        {
            if (_tooltip != null && UITooltip.m_current == _tooltip) UITooltip.HideTooltip();
            _hits.Clear();
        }

        public void Bind(ItemDrop.ItemData item, int quality, int variant)
        {
            if (_tooltip == null)
            {
                _tooltip = GetComponent<UITooltip>() ?? gameObject.AddComponent<UITooltip>();
                _tooltip.m_tooltipPrefab = Auga.Assets.InventoryTooltip;
                _data = GetComponent<ItemTooltip>() ?? gameObject.AddComponent<ItemTooltip>();
            }
            bool changed = _source != item || _quality != quality || _variant != variant;
            if (changed && UITooltip.m_current == _tooltip) UITooltip.HideTooltip();
            if (changed)
            {
                _source = item; _quality = quality; _variant = variant;
                _data.Item = item;
                if (item != null && ((quality > 0 && quality != item.m_quality) || (variant >= 0 && variant != item.m_variant)))
                {
                    // Preview data only: never change the selected inventory item.
                    _data.Item = item.Clone();
                    if (quality > 0) _data.Item.m_quality = quality;
                    if (variant >= 0) _data.Item.m_variant = variant;
                    _data.Item.m_durability = _data.Item.GetMaxDurability();
                }
            }
            _tooltip.m_topic = item?.m_shared.m_name ?? "";
            _tooltip.m_text = item?.m_shared.m_description ?? "";
            _tooltip.enabled = item != null;
            GetComponent<Image>().raycastTarget = item != null;
        }

        public void BindStation(CraftingStation station)
        {
            if (_tooltip == null) _tooltip = GetComponent<UITooltip>() ?? gameObject.AddComponent<UITooltip>();
            _tooltip.m_tooltipPrefab = Auga.Assets.SimpleTooltip;
            string topic = station != null ? station.m_name : "";
            string description = station != null ? Localization.instance.Localize("$level") + " " + station.GetLevel() : "";
            if ((_tooltip.m_topic != topic || _tooltip.m_text != description) && UITooltip.m_current == _tooltip)
                UITooltip.HideTooltip();
            _tooltip.m_topic = topic; _tooltip.m_text = description;
            _tooltip.enabled = station != null;
            GetComponent<Image>().raycastTarget = station != null;
        }
    }

    public sealed partial class InventoryPresentation : MonoBehaviour
    {
        internal bool ApiReady => _ready;
        internal Button ApiPlayerTab(int index) => index >= 0 && index < _referenceTabs.Count ? _referenceTabs[index].Button : null;
        internal Button ApiWorkbenchTab(int index) => index >= 0 && index < _stationTabs.Count ? _stationTabs[index].Button : null;
        internal bool ApiIsTabActive(GameObject button, bool station)
        {
            if (button == null || !button.activeInHierarchy) return false;
            var tabs = station ? _stationTabs : _referenceTabs;
            int index = tabs.FindIndex(tab => tab.gameObject == button);
            return index >= 0 && index == (station ? ApiWorkbenchIndex : _selectedTab);
        }
        private static readonly Color Cream = new Color(.918f, .882f, .851f);
        private static readonly Color Muted = new Color(.59f, .56f, .49f);
        private static readonly Color Panel = new Color(.22f, .20f, .165f, 1f);
        private static readonly Color Dark = new Color(.12f, .11f, .09f, .85f);
        private static readonly Color Blue = new Color(.29f, .265f, .22f);
        private static TMP_FontAsset _slotNumberFont;
        private readonly List<Image> _hidden = new List<Image>();
        private readonly List<(Button button, Image border)> _actions = new List<(Button, Image)>();
        private InventoryGui _gui;
        private TMP_FontAsset _body, _norse;
        private bool _ready;
        private GameObject _statusPanel, _skillsPanel, _messageLogPanel;
        private TMP_Text _panelTitle;
        private int _selectedTab = 1;
        private readonly List<TabButton> _referenceTabs = new List<TabButton>();
        private CanvasGroup _craftVisibility;
        private ComplexTooltip _recipeInfo;
        private TMP_Text _craftCaption;
        private RectTransform _frame;
        private readonly Vector3[] _inventoryCorners = new Vector3[4];
        private RectTransform _chestWeightIcon;
        private CraftingRequirementsPanel _craftRequirements, _upgradeRequirements, _maxRequirements;
        private GameObject _stationHeader;
        private Image _stationIcon;
        private UnityEngine.UI.Text _stationLevel;
        private Button _stationRepair;
        private TMP_Text _stationMode;
        private readonly List<TabButton> _stationTabs = new List<TabButton>();
        private readonly Vector3[] _mapCorners = new Vector3[4];
        private CraftingRequirementsPanel _referenceRequirements;
        private Button _referenceCraft, _referenceCancel, _referenceVariant;

        private static bool Hint(Transform t)
        {
            for (; t != null; t = t.parent) if (t.name.StartsWith("gamepad_hint")) return true;
            return false;
        }

        private void Text(TMP_Text text, bool heading = false, float size = 0)
        {
            text.font = heading ? _norse : _body; text.fontSharedMaterial = text.font.material;
            if (size > 0) text.fontSize = size;
            text.fontStyle = FontStyles.Normal; text.color = Cream;
        }

        private void Start()
        {
            try
            {
                _gui = GetComponent<InventoryGui>();
                _body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                _norse = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                // Retain all current native grids, references, callbacks and container sizing.
                foreach (var root in new[] { _gui.m_player, _gui.m_crafting, _gui.m_info, _gui.m_container })
                {
                    foreach (var image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite == null) continue;
                        if (image.sprite.name == "panel_separator") { _hidden.Add(image); continue; }
                        if (!image.sprite.name.Contains("woodpanel")) continue;
                        _hidden.Add(image);
                        var bg = Box(image.transform, "Auga Inventory Panel", Panel); bg.SetAsFirstSibling();
                        SettingsPresentation.AddCorners(bg);
                    }
                    foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                        if (!Hint(label.transform)) Text(label);
                    foreach (var scrollbar in root.GetComponentsInChildren<Scrollbar>(true)) SettingsPresentation.StyleScrollbar(scrollbar);
                }
                Text(_gui.m_playerName, true, 32);
                Text(_gui.m_craftingStationName, true, 30);
                Text(_gui.m_containerName, true, 28);
                Text(_gui.m_recipeName, false, 26);
                Text(_gui.m_recipeDecription, false, 17);
                Text(_gui.m_weight, false, 16); Text(_gui.m_armor, false, 16);
                CreateStatusPanel();
                CreateCraftingDetails();
                foreach (var button in new[] { _gui.m_craftButton, _gui.m_craftCancelButton, _gui.m_takeAllButton, _gui.m_stackAllButton, _gui.m_variantButton })
                    if (button != null) Action(button);
                foreach (var button in new[] { _gui.m_tabCraft, _gui.m_tabUpgrade })
                {
                    _hidden.AddRange(button.GetComponentsInChildren<Image>(true).Where(i => !Hint(i.transform)));
                    var hit = Box(button.transform, "Auga Recipe Tab Hit Area", Color.clear); hit.SetAsFirstSibling();
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    Text(label, false, 18); label.fontStyle = FontStyles.UpperCase;
                    var tint = button.GetComponent<ButtonTextColor>(); if (tint != null) tint.enabled = false;
                    button.targetGraphic = label; button.transition = Selectable.Transition.ColorTint;
                    var colors = button.colors; colors.normalColor = colors.disabledColor = Color.white;
                    colors.highlightedColor = colors.selectedColor = new Color(1, .83f, .5f); button.colors = colors;
                }
                foreach (var grid in new[] { _gui.m_playerGrid, _gui.m_containerGrid })
                {
                    StyleSlots(grid.m_elementPrefab.transform);
                    foreach (Transform slot in grid.m_gridRoot) StyleSlots(slot);
                }
                var layout = _gui.m_playerGrid.gameObject.AddComponent<InventoryReferenceLayout>();
                layout.Panel = _gui.m_player;
                CreateInventoryFooter();
                CreateChestPresentation();
                CreateSplitPresentation();
                _ready = true; StyleRecipes();
                foreach (var image in _hidden) image.enabled = false;
                Debug.Log("[Auga] Native inventory and crafting presentation applied; item and recipe behavior retained.");
            }
            catch (Exception e) { Auga.LogError($"Inventory presentation failed: {e}"); enabled = false; }
        }

        private void CreateCraftingDetails()
        {
            // Keep the native model/controllers alive, but render the original right-column prefab.
            var legacy = Box(_gui.m_crafting, "Native Crafting Bindings", Color.clear);
            foreach (Transform child in _gui.m_crafting.Cast<Transform>().ToArray())
                if (child != legacy) child.SetParent(legacy, false);
            var hidden = legacy.gameObject.AddComponent<CanvasGroup>(); hidden.alpha = 0;
            hidden.interactable = hidden.blocksRaycasts = false;
            foreach (var group in legacy.GetComponentsInChildren<CanvasGroup>(true)) group.ignoreParentGroups = false;
            var source = Auga.Assets.InventoryScreen.GetComponentInChildren<AugaCraftingPanel>(true);
            var right = UnityEngine.Object.Instantiate(source.ItemInfo.transform.parent, _gui.m_crafting, false);
            _apiNativeDetails = right.gameObject;
            _apiCraftSource = source;
            Localization.instance.Localize(right);
            _gui.m_recipeListSpace = 34;
            _recipeInfo = right.GetComponentInChildren<ComplexTooltip>(true);
            _referenceRequirements = right.GetComponentsInChildren<CraftingRequirementsPanel>(true).First(r => r.name == source.CraftingRequirementsPanel.name);
            _craftRequirements = _referenceRequirements;
            _upgradeRequirements = right.GetComponentsInChildren<CraftingRequirementsPanel>(true).First(r => r.name == source.UpgradeRequirementsPanel.name);
            _maxRequirements = right.GetComponentsInChildren<CraftingRequirementsPanel>(true).First(r => r.name == source.MaxQualityUpgradeRequirementsPanel.name);
            foreach (var requirements in right.GetComponentsInChildren<CraftingRequirementsPanel>(true))
            {
                requirements.enabled = false; requirements.gameObject.SetActive(requirements == _referenceRequirements);
                // Diagram artwork is decorative; dedicated slot targets own hover input.
                foreach (var graphic in requirements.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                foreach (var tooltip in requirements.GetComponentsInChildren<UITooltip>(true)) tooltip.enabled = false;
            }
            _recipeInfo.Icon = _referenceRequirements.Icon;
            foreach (var button in right.GetComponentsInChildren<Button>(true)) button.onClick = new Button.ButtonClickedEvent();
            _referenceCraft = right.GetComponentsInChildren<Button>(true).First(b => b.name == source.CraftButton.name);
            _referenceCancel = right.GetComponentsInChildren<Button>(true).First(b => b.name == source.CraftCancelButton.name);
            _referenceVariant = right.GetComponentsInChildren<Button>(true).First(b => b.name == source.VariantButton.name);
            _referenceCraft.onClick.AddListener(() => _gui.m_craftButton.onClick.Invoke());
            _referenceCancel.onClick.AddListener(() => _gui.m_craftCancelButton.onClick.Invoke());
            _referenceVariant.onClick.AddListener(() => _gui.m_variantButton.onClick.Invoke());
            foreach (var original in new[] { source.PlusButton.transform, source.MinusButton.transform, source.CraftAmountText.transform, source.CraftAmountBG.transform })
            {
                var path = original.name;
                for (var parent = original.parent; parent != source.ItemInfo.transform.parent; parent = parent.parent) path = parent.name + "/" + path;
                var clone = right.Find(path); if (clone != null) clone.gameObject.SetActive(false);
            }
            var craftRect = (RectTransform)_referenceCraft.transform;
            craftRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 272);
            craftRect.anchoredPosition = new Vector2(0, craftRect.anchoredPosition.y - 50);
            // Use the spare footer space for the stats, keeping their top edge fixed.
            var statsRect = (RectTransform)_recipeInfo.transform.Find("TooltipScrollContainer");
            statsRect.sizeDelta += new Vector2(0, 40);
            statsRect.anchoredPosition -= new Vector2(0, 20);
            var statsScroll = statsRect.GetComponent<ScrollRect>();
            if (statsScroll != null) statsScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            // Keep the progress/cancel state in the same position as the Craft button.
            var progressRect = right.Find("CraftProgress") as RectTransform;
            if (progressRect != null) progressRect.anchoredPosition -= new Vector2(0, 50);
            foreach (var dialog in right.GetComponentsInChildren<VariantDialog>(true)) dialog.gameObject.SetActive(false);
            var list = _gui.m_recipeListRoot.GetComponentInParent<ScrollRect>();
            _apiRecipeList = list != null ? list.gameObject : null;
            if (list != null)
            {
                list.transform.SetParent(_gui.m_crafting, false);
                foreach (var image in list.GetComponentsInChildren<Image>(true))
                    if (!image.transform.IsChildOf(_gui.m_recipeListRoot) && image.GetComponentInParent<Scrollbar>() == null) image.color = Color.clear;
                if (list.GetComponent<RectMask2D>() == null) list.gameObject.AddComponent<RectMask2D>();
                var rect = (RectTransform)list.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
                // Crafting already sits 18 units inside the outer panel. Keep that same
                // 18-unit gap between the 166-unit rows and the divider at x=184.
                rect.anchoredPosition = new Vector2(0, -72); rect.sizeDelta = new Vector2(166, 777);
                if (list.viewport != null) Place(list.viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _gui.m_recipeListRoot.pivot = new Vector2(0, 1);
                _gui.m_recipeListRoot.anchorMin = _gui.m_recipeListRoot.anchorMax = new Vector2(0, 1);
                _gui.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 166);
            }
            var separator = Box(_gui.m_crafting, "Auga Recipe Divider", Dark);
            _apiRecipeDivider = separator.gameObject;
            Place(separator, new Vector2(0, 0), new Vector2(0, 1), new Vector2(184, 8), new Vector2(185, -72));
            separator.GetComponent<Image>().raycastTarget = false;
            CreateStationHeader(source);
        }

        private void CreateStationHeader(AugaCraftingPanel source)
        {
            _stationHeader = Box(_frame, "Auga Station Header", Color.clear).gameObject;
            _stationHeader.GetComponent<Image>().raycastTarget = false;
            _stationIcon = Instantiate(source.WorkbenchIcon, _stationHeader.transform, false);
            Place(_stationIcon.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -82), new Vector2(96, -14));
            var level = Instantiate(source.WorkbenchLevelRoot, _stationHeader.transform, false);
            Place(level, Vector2.one, Vector2.one, new Vector2(-86, -82), new Vector2(-22, -18));
            _stationLevel = level.GetComponentInChildren<UnityEngine.UI.Text>(true);
            level.gameObject.SetActive(true);
            _stationRepair = Instantiate(source.RepairButton, _stationHeader.transform, false);
            _stationRepair.onClick = new Button.ButtonClickedEvent();
            _stationRepair.onClick.AddListener(() => _gui.m_repairButton.onClick.Invoke());
            _stationRepair.gameObject.SetActive(true);
            var repairRect = (RectTransform)_stationRepair.transform;
            repairRect.pivot = new Vector2(.5f, .5f);
            // Match the header divider's -98 centerline; offset left as the diamond grows.
            Place(repairRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-34, -126), new Vector2(22, -70));
            repairRect.localScale = Vector3.one * 2f;
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var tab = Instantiate(source.TabController.TabButtons[i], _stationHeader.transform, false);
                Place((RectTransform)tab.transform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2((i == 0 ? -43 : 3), -119), new Vector2((i == 0 ? -3 : 43), -79));
                tab.Button.onClick = new Button.ButtonClickedEvent();
                tab.Button.onClick.AddListener(() => {
                    _apiWorkbenchIndex = -1;
                    ApiCloseVariant();
                    (index == 0 ? _gui.m_tabCraft : _gui.m_tabUpgrade).onClick.Invoke();
                    ApiRefreshExtensions();
                });
                _stationTabs.Add(tab);
            }
            _stationHeader.SetActive(false);
            var mode = new GameObject("Station Mode", typeof(RectTransform), typeof(TextMeshProUGUI));
            mode.layer = _frame.gameObject.layer; mode.transform.SetParent(_stationHeader.transform, false);
            _stationMode = mode.GetComponent<TMP_Text>(); Text(_stationMode, false, 14);
            _stationMode.alignment = TextAlignmentOptions.Center;
            Place(_stationMode.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-90, -78), new Vector2(90, -56));
        }

        private void UpdateReferenceRequirements()
        {
            if (_referenceRequirements == null) return;
            var selectedItem = _gui.m_selectedRecipe.ItemData;
            bool upgrade = _gui.m_tabCraft.interactable;
            var active = !upgrade ? _craftRequirements : selectedItem != null && selectedItem.m_quality >= selectedItem.m_shared.m_maxQuality ? _maxRequirements : _upgradeRequirements;
            foreach (var panel in new[] { _craftRequirements, _upgradeRequirements, _maxRequirements }) panel.gameObject.SetActive(panel == active);
            _referenceRequirements = active;
            _recipeInfo.Icon = active.Icon;
            var recipe = _gui.m_selectedRecipe.Recipe;
            if (active.Icon != null)
            {
                active.Icon.enabled = recipe != null;
                if (recipe != null) active.Icon.sprite = (selectedItem ?? recipe.m_item.m_itemData).GetIcon();
            }
            active.Update();
            if (active.WorkbenchIcon != null)
                DiagramHoverTarget(active.WorkbenchIcon).BindStation(Player.m_localPlayer != null ? Player.m_localPlayer.GetCurrentCraftingStation() : null);
            var displayedItem = recipe != null ? selectedItem ?? recipe.m_item.m_itemData : null;
            BindDiagramTooltip(active.Icon, displayedItem, selectedItem == null ? 1 : selectedItem.m_quality,
                selectedItem == null ? _gui.m_selectedVariant : selectedItem.m_variant);
            BindDiagramTooltip(active.UpgradedIcon, displayedItem, selectedItem == null ? 1 :
                Mathf.Min(selectedItem.m_quality + 1, selectedItem.m_shared.m_maxQuality),
                selectedItem == null ? _gui.m_selectedVariant : selectedItem.m_variant);
            if (active.WorkbenchLevel != null) active.WorkbenchLevel.text = _gui.m_minStationLevelText.text;
            for (int i = 0; i < _referenceRequirements.RequirementList.Length; i++)
            {
                var target = _referenceRequirements.RequirementList[i];
                var native = i < _gui.m_recipeRequirementList.Length ? _gui.m_recipeRequirementList[i] : null;
                var nativeIcon = native != null ? native.transform.Find("res_icon")?.GetComponent<Image>() : null;
                bool shown = native != null && native.activeSelf && nativeIcon != null && nativeIcon.gameObject.activeSelf;
                target.SetActive(true);
                foreach (var text in target.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {
                    var nativeText = native != null ? native.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == text.name) : null;
                    text.gameObject.SetActive(shown); text.enabled = true;
                    text.text = shown && nativeText != null ? nativeText.text : "";
                    if (nativeText != null) text.color = nativeText.color;
                }
                var icon = target.GetComponentsInChildren<Image>(true).FirstOrDefault(image => image.name == "res_icon");
                if (icon != null) { icon.gameObject.SetActive(shown); icon.enabled = shown; if (nativeIcon != null) { icon.sprite = nativeIcon.sprite; icon.color = nativeIcon.color; } }
                var resource = shown && recipe != null ? recipe.m_resources.FirstOrDefault(r =>
                    r.m_resItem != null && r.m_resItem.m_itemData.GetIcon() == nativeIcon.sprite) : null;
                BindDiagramTooltip(icon, resource?.m_resItem.m_itemData);
            }
            foreach (var label in _referenceCraft.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                label.text = Localization.instance.Localize(_gui.m_tabCraft.interactable ? "$inventory_upgradebutton" : "$inventory_craftbutton");
            bool crafting = _gui.m_craftTimer >= 0;
            _referenceCraft.gameObject.SetActive(!crafting); _referenceCancel.gameObject.SetActive(crafting);
            _referenceCraft.interactable = _gui.m_craftButton.interactable;
            _referenceVariant.gameObject.SetActive(!_apiVariantEnabled && _gui.m_variantButton.gameObject.activeSelf);
            if (active.WireFrame != null && recipe != null && Player.m_localPlayer != null)
            {
                var wires = new WireState[active.WireFrame.Wires.Length];
                for (int i = 0; i < wires.Length && i < active.RequirementList.Length; i++)
                {
                    var icon = active.RequirementList[i].transform.Find("res_icon")?.GetComponent<Image>();
                    if (icon == null || !icon.gameObject.activeSelf) continue;
                    var resource = recipe.m_resources.FirstOrDefault(r => r.m_resItem != null && r.m_resItem.m_itemData.GetIcon() == icon.sprite);
                    if (resource != null) wires[i] = Player.m_localPlayer.GetInventory().CountItems(resource.m_resItem.m_itemData.m_shared.m_name) >= resource.GetAmount(selectedItem == null ? 1 : selectedItem.m_quality + 1) ? WireState.Have : WireState.DontHave;
                }
                active.WireFrame.Set(wires, _gui.m_craftButton.interactable);
            }
        }

        private static void BindDiagramTooltip(Image icon, ItemDrop.ItemData item, int quality = -1, int variant = -1)
        {
            if (icon == null) return;
            var binding = DiagramHoverTarget(icon);
            binding.Bind(item, quality, variant);
        }

        private static CraftingDiagramTooltip DiagramHoverTarget(Image icon)
        {
            var slot = icon.transform.parent;
            var target = slot.Find("Auga Diagram Hover " + icon.name);
            if (target == null)
            {
                var rect = Box(slot, "Auga Diagram Hover " + icon.name, Color.clear);
                // Follow the icon bounds without changing the original slot hierarchy.
                rect.anchorMin = icon.rectTransform.anchorMin; rect.anchorMax = icon.rectTransform.anchorMax;
                rect.pivot = icon.rectTransform.pivot;
                rect.sizeDelta = icon.rectTransform.sizeDelta;
                rect.anchoredPosition = icon.rectTransform.anchoredPosition;
                rect.gameObject.AddComponent<CraftingDiagramTooltip>();
                target = rect;
            }
            target.SetAsLastSibling();
            icon.raycastTarget = false;
            return target.GetComponent<CraftingDiagramTooltip>();
        }

        private void CreateSplitPresentation()
        {
            var dialog = _gui.m_splitDialog;
            if (dialog == null) return;
            var panel = dialog.m_panel;
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
                if (image.sprite != null && image.sprite.name.Contains("woodpanel")) _hidden.Add(image);
            var background = Box(panel, "Auga Split Background", Panel);
            background.SetAsFirstSibling(); SettingsPresentation.AddCorners(background);
            foreach (var label in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Hint(label.transform)) continue;
                bool title = label != dialog.m_splitAmount && label != dialog.m_splitIconName && label.GetComponentInParent<Button>() == null;
                Text(label, title, title ? 30 : 20);
                if (title) label.fontStyle = FontStyles.UpperCase;
            }
            foreach (var button in new[] { dialog.m_splitCancelButton, dialog.m_splitOkButton })
            {
                Action(button);
                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                    if (!Hint(label.transform)) { Text(label, true, 24); label.fontStyle = FontStyles.UpperCase; }
            }
            // Keep the native item image; only replace its backing plate.
            var icon = dialog.m_splitIcon;
            if (icon != null)
            {
                Image plate = null;
                for (var parent = icon.transform.parent; parent != null && parent != panel; parent = parent.parent)
                {
                    plate = parent.GetComponent<Image>() ?? parent.Find("bkg")?.GetComponent<Image>();
                    if (plate != null) break;
                }
                if (plate != null)
                {
                    plate.sprite = Auga.Assets.InventoryScreen.transform.Find("root/Player/PlayerGrid")
                        .GetComponent<InventoryGrid>().m_elementPrefab.GetComponent<Image>().sprite;
                    plate.type = Image.Type.Simple; plate.color = Dark;
                }
            }
            var slider = dialog.m_splitSlider;
            foreach (var image in slider.GetComponentsInChildren<Image>(true)) _hidden.Add(image);
            if (slider.handleRect != null)
            {
                var handle = Box(slider.handleRect, "Auga Split Slider Handle", new Color(.72f, .56f, .19f));
                Place(handle, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-6, -6), new Vector2(6, 6));
                handle.localRotation = Quaternion.Euler(0, 0, 45);
                slider.targetGraphic = handle.GetComponent<Image>();
            }
            var track = Box(slider.transform, "Auga Split Slider Track", Muted);
            track.SetAsFirstSibling();
            Place(track, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, -1), new Vector2(0, 1));
            track.GetComponent<Image>().raycastTarget = false;
            if (slider.fillRect != null)
            {
                var fill = Box(slider.fillRect, "Auga Split Slider Fill", Cream);
                Place(fill, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, -1), new Vector2(0, 1));
                fill.GetComponent<Image>().raycastTarget = false;
            }
        }

        private void CreateChestPresentation()
        {
            var panel = _gui.m_container;
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
                if (!image.transform.IsChildOf(_gui.m_containerGrid.transform) && image.GetComponentInParent<Button>() == null)
                    _hidden.Add(image);
            var background = Box(panel, "Auga Chest Background", Panel);
            background.SetAsFirstSibling(); background.GetComponent<Image>().raycastTarget = false;
            _gui.m_containerName.transform.SetParent(panel, false);
            Text(_gui.m_containerName, false, 28);
            _gui.m_containerName.fontStyle = FontStyles.UpperCase;
            _gui.m_containerName.alignment = TextAlignmentOptions.Center;
            Place(_gui.m_containerName.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(140, -58), new Vector2(-140, -10));
            var divider = Auga.Assets.InventoryScreen.transform.Find("root/Player/StandardDivider");
            for (int side = 0; side < 2; side++)
            {
                var line = Instantiate(divider, panel, false);
                line.gameObject.SetActive(true);
                Place((RectTransform)line, new Vector2(side == 0 ? 0 : .5f, 1), new Vector2(side == 0 ? .5f : 1, 1),
                    new Vector2(side == 0 ? 22 : 100, -40), new Vector2(side == 0 ? -100 : -22, -28));
                foreach (var graphic in line.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            }
            int index = 0;
            foreach (var button in new[] { _gui.m_takeAllButton, _gui.m_stackAllButton })
            {
                button.transform.SetParent(panel, false);
                var anchor = new Vector2(index == 0 ? 0 : 1, 0);
                Place((RectTransform)button.transform, anchor, anchor, new Vector2(index == 0 ? 16 : -128, 14), new Vector2(index == 0 ? 104 : -16, 44));
                var border = button.transform.Find("Auga Inventory Action").GetComponent<Image>();
                var source = Auga.Assets.ButtonSmall.GetComponentsInChildren<Image>(true).First(image => image.sprite != null);
                border.sprite = source.sprite; border.type = source.type; border.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                border.enabled = true; border.color = Color.white; border.canvasRenderer.SetAlpha(1);
                _hidden.Remove(border);
                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) { Text(label, false, 14); label.fontStyle = FontStyles.UpperCase; }
                index++;
            }
            var oldWeight = _gui.m_containerWeight;
            oldWeight.gameObject.SetActive(false);
            var weightObject = new GameObject("Auga Chest Weight", typeof(RectTransform), typeof(TextMeshProUGUI));
            weightObject.layer = panel.gameObject.layer; weightObject.transform.SetParent(panel, false);
            _gui.m_containerWeight = weightObject.GetComponent<TMP_Text>();
            Text(_gui.m_containerWeight, false, 16); _gui.m_containerWeight.alignment = TextAlignmentOptions.Right;
            _gui.m_containerWeight.raycastTarget = false;
            Place(_gui.m_containerWeight.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-38, 14), new Vector2(14, 44));
            var bag = Box(panel, "Auga Chest Weight Icon", Muted).GetComponent<Image>();
            _chestWeightIcon = bag.rectTransform;
            bag.sprite = Auga.Assets.InventoryScreen.transform.Find("root/Player/Weight/Icon").GetComponent<Image>().sprite;
            bag.preserveAspect = true; bag.raycastTarget = false;
            Place(bag.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(22, 20), new Vector2(38, 38));
            for (int side = 0; side < 2; side++)
            {
                var footer = Instantiate(divider, panel, false);
                footer.gameObject.SetActive(true);
                Place((RectTransform)footer, new Vector2(side == 0 ? 0 : .5f, 0), new Vector2(side == 0 ? .5f : 1, 0),
                    new Vector2(side == 0 ? 144 : 50, 23), new Vector2(side == 0 ? -50 : -144, 35));
                foreach (var graphic in footer.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            }
        }

        private void LayoutChest()
        {
            if (!_gui.m_container.gameObject.activeInHierarchy) return;
            // Center the visible number and icon together, including when digit count changes.
            float textWidth = _gui.m_containerWeight.GetPreferredValues(_gui.m_containerWeight.text).x;
            float left = -(textWidth + 8f + 16f) * .5f;
            Place(_gui.m_containerWeight.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(left, 14), new Vector2(left + textWidth, 44));
            Place(_chestWeightIcon, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(left + textWidth + 8, 20), new Vector2(-left, 38));
            var size = _gui.m_containerGrid.GetWidgetSize();
            _gui.m_player.GetWorldCorners(_inventoryCorners);
            float width = (_inventoryCorners[3].x - _inventoryCorners[0].x) / _gui.m_container.lossyScale.x;
            _gui.m_container.pivot = new Vector2(0, 1);
            _gui.m_container.position = _inventoryCorners[0] - new Vector3(0, 20 * _gui.m_player.lossyScale.y, 0);
            _gui.m_container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _gui.m_container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y + 190);
            var grid = (RectTransform)_gui.m_containerGrid.transform;
            grid.SetParent(_gui.m_container, false);
            grid.anchorMin = grid.anchorMax = new Vector2(0, 1); grid.pivot = new Vector2(0, 1);
            grid.anchoredPosition = new Vector2((width - size.x) * .5f, -110); grid.sizeDelta = size;
            var slots = _gui.m_containerGrid.m_gridRoot;
            if (slots != grid)
            {
                if (slots.parent != grid) slots.SetParent(grid, false);
                slots.anchorMin = slots.anchorMax = new Vector2(0, 1); slots.pivot = new Vector2(0, 1);
                slots.anchoredPosition = Vector2.zero; slots.sizeDelta = size;
            }
            // Native UpdateGui caches a centering offset when it creates the cells.
            // Our grid is already centered; discard that old offset after resizing.
            foreach (var element in _gui.m_containerGrid.m_elements)
            {
                var cell = (RectTransform)element.transform;
                cell.anchorMin = cell.anchorMax = new Vector2(0, 1);
                cell.pivot = new Vector2(0, 1);
                cell.anchoredPosition = new Vector2(element.Position.x * _gui.m_containerGrid.m_elementSpace,
                    -element.Position.y * _gui.m_containerGrid.m_elementSpace);
            }
        }

        private void CreateInventoryFooter()
        {
            var original = Auga.Assets.InventoryScreen.transform.Find("root/Player");
            _gui.m_armor.transform.parent.gameObject.SetActive(false);
            _gui.m_weight.transform.parent.gameObject.SetActive(false);
            var footer = Box(_gui.m_player, "Auga Inventory Footer", Color.clear);
            Place(footer, Vector2.zero, new Vector2(1, 0), new Vector2(12, 4), new Vector2(-12, 34));
            footer.GetComponent<Image>().raycastTarget = false;
            for (int i = 0; i < 2; i++)
            {
                bool armor = i == 0;
                var source = original.Find(armor ? "Armor" : "Weight");
                var icon = Box(footer, armor ? "Armor Icon" : "Weight Icon", Muted).GetComponent<Image>();
                icon.sprite = source.Find("Icon").GetComponent<Image>().sprite;
                icon.preserveAspect = true; icon.raycastTarget = false;
                Place(icon.rectTransform, new Vector2(armor ? 0 : 1, .5f), new Vector2(armor ? 0 : 1, .5f),
                    new Vector2(armor ? 0 : -18, -9), new Vector2(armor ? 18 : 0, 9));
                var obj = new GameObject(armor ? "Armor Value" : "Weight Value", typeof(RectTransform), typeof(TextMeshProUGUI));
                obj.layer = footer.gameObject.layer; obj.transform.SetParent(footer, false);
                var label = obj.GetComponent<TextMeshProUGUI>(); Text(label, false, 16);
                label.raycastTarget = false;
                label.alignment = armor ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
                Place(label.rectTransform, new Vector2(armor ? 0 : 1, 0), new Vector2(armor ? 0 : 1, 1),
                    new Vector2(armor ? 28 : -116, 0), new Vector2(armor ? 70 : -28, 0));
                if (armor) _gui.m_armor = label; else _gui.m_weight = label;
            }
            var divider = UnityEngine.Object.Instantiate(original.Find("StandardDivider"), footer, false);
            divider.gameObject.SetActive(true);
            Place((RectTransform)divider, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(72, -6), new Vector2(-124, 6));
            foreach (var graphic in divider.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        private void Action(Button button)
        {
            _hidden.AddRange(button.GetComponentsInChildren<Image>(true).Where(i => !Hint(i.transform)));
            var bg = Box(button.transform, "Auga Inventory Action", Color.white).GetComponent<Image>();
            var source = Auga.Assets.ButtonFancy.GetComponent<Button>().targetGraphic as Image;
            bg.sprite = source.sprite; bg.type = source.type; bg.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            ((RectTransform)button.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 44);
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Hint(label.transform)) continue;
                Text(label, true, 26); label.transform.SetAsLastSibling();
                Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0));
                label.alignment = TextAlignmentOptions.Center;
            }
            var tint = button.GetComponent<ButtonTextColor>();
            if (tint != null) { tint.m_defaultColor = tint.m_defaultMeshColor = Cream; tint.m_disabledColor = Muted; }
            if (button == _gui.m_craftButton)
            {
                var obj = new GameObject("Auga Craft Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
                obj.layer = button.gameObject.layer; obj.transform.SetParent(button.transform, false);
                _craftCaption = obj.GetComponent<TextMeshProUGUI>(); Text(_craftCaption, true, 26);
                _craftCaption.raycastTarget = false; _craftCaption.alignment = TextAlignmentOptions.Center;
                Place(_craftCaption.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            _actions.Add((button, bg));
        }

        private void CreateStatusPanel()
        {
            var source = Auga.Assets.InventoryScreen.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "TabContent_PlayerPanel");
            if (source == null) { Auga.LogWarning("Original Auga player-details content was not found."); return; }
            var frame = Box(_gui.m_info.parent, "Auga Unified Inventory Panel", Panel);
            _frame = frame;
            frame.anchorMin = frame.anchorMax = Vector2.one; frame.pivot = Vector2.one;
            frame.sizeDelta = new Vector2(600, 1030);
            var canvasRect = (RectTransform)frame.parent;
            float scale = canvasRect.rect.width * .24f / 600f;
            frame.localScale = Vector3.one * scale;
            frame.anchoredPosition = new Vector2(-canvasRect.rect.width * .015f, -8f);
            frame.SetSiblingIndex(_gui.m_crafting.GetSiblingIndex());
            SettingsPresentation.AddCorners(frame);
            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.layer = frame.gameObject.layer; titleObject.transform.SetParent(frame, false);
            _panelTitle = titleObject.GetComponent<TextMeshProUGUI>(); Text(_panelTitle, true, 36);
            _panelTitle.alignment = TextAlignmentOptions.Center; _panelTitle.raycastTarget = false;
            Place(_panelTitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(16, -70), new Vector2(-16, -12));
            // Native info callbacks remain available through the shared header.
            var infoVisibility = _gui.m_info.GetComponent<CanvasGroup>() ?? _gui.m_info.gameObject.AddComponent<CanvasGroup>();
            infoVisibility.alpha = 0; infoVisibility.interactable = infoVisibility.blocksRaycasts = false;
            _gui.m_crafting.SetParent(frame, false);
            _gui.m_crafting.anchorMin = _gui.m_crafting.anchorMax = new Vector2(.5f, 0);
            _gui.m_crafting.pivot = new Vector2(.5f, 0); _gui.m_crafting.anchoredPosition = new Vector2(0, 18);
            _gui.m_crafting.sizeDelta = new Vector2(564, 870);
            foreach (var child in _gui.m_crafting.GetComponentsInChildren<Transform>(true))
                if (child.name == "Auga Inventory Panel") child.gameObject.SetActive(false);
            _craftVisibility = _gui.m_crafting.GetComponent<CanvasGroup>() ?? _gui.m_crafting.gameObject.AddComponent<CanvasGroup>();
            var content = UnityEngine.Object.Instantiate(source, frame, false);
            _statusPanel = content.gameObject;
            Place((RectTransform)content, Vector2.zero, Vector2.one, new Vector2(18, 40), new Vector2(-18, -140));
            Localization.instance.Localize(content); content.gameObject.SetActive(false);
            var skillsSource = Auga.Assets.InventoryScreen.GetComponentsInChildren<Transform>(true).First(t => t.name == "TabContent_Skills");
            var skills = UnityEngine.Object.Instantiate(skillsSource, frame, false);
            _skillsPanel = skills.gameObject;
            foreach (var scroll in skills.GetComponentsInChildren<ScrollRect>(true))
                scroll.scrollSensitivity = 240f;
            Place((RectTransform)skills, Vector2.zero, Vector2.one, new Vector2(18, 40), new Vector2(-18, -140));
            skills.gameObject.SetActive(false);
            var logSource = Auga.Assets.InventoryScreen.GetComponentsInChildren<Transform>(true).First(t => t.name == "TabContent_MessageLog");
            var log = UnityEngine.Object.Instantiate(logSource, frame, false);
            _messageLogPanel = log.gameObject;
            Place((RectTransform)log, Vector2.zero, Vector2.one, new Vector2(18, 40), new Vector2(-18, -140));
            Localization.instance.Localize(log);
            log.gameObject.SetActive(false);
            var names = new[] { "TabButton_PlayerPanel", "TabButton_Crafting", "TabButton_Skills", "TabButton_MessageLog", "TabButton_PVP" };
            for (int i = 0; i < names.Length; i++)
            {
                var tabSource = Auga.Assets.InventoryScreen.GetComponentsInChildren<Transform>(true).First(t => t.name == names[i]);
                var tab = UnityEngine.Object.Instantiate(tabSource, frame, false).GetComponent<TabButton>();
                var rect = (RectTransform)tab.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2((i - 2) * 42, -98); rect.sizeDelta = new Vector2(38, 38);
                tab.gameObject.SetActive(true); tab.Button.onClick = new Button.ButtonClickedEvent();
                int index = i;
                tab.Button.onClick.AddListener(() => {
                    if (index == 4) { _gui.m_pvp.isOn = !_gui.m_pvp.isOn; return; }
                    _selectedTab = index;
                    ApiCloseVariant();
                    _statusPanel.SetActive(index == 0); _skillsPanel.SetActive(index == 2);
                    _messageLogPanel.SetActive(index == 3);
                });
                _referenceTabs.Add(tab);
            }
            var dividerSource = Auga.Assets.InventoryScreen.transform.Find("root/Player/StandardDivider");
            for (int side = 0; side < 2; side++)
            {
                var divider = UnityEngine.Object.Instantiate(dividerSource, frame, false);
                divider.gameObject.SetActive(true);
                Place((RectTransform)divider, new Vector2(side == 0 ? 0 : .5f, 1), new Vector2(side == 0 ? .5f : 1, 1),
                    new Vector2(side == 0 ? 24 : 116, -104), new Vector2(side == 0 ? -116 : -24, -92));
                foreach (var graphic in divider.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            }
            Debug.Log("[Auga] Shared inventory frame, original diamond tabs and skills content restored.");
        }

        private void StyleSlots(Transform root)
        {
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) if (!Hint(label.transform)) Text(label);
            StyleSlotBackground(root);
        }

        internal static void StyleSlotBackground(Transform root)
        {
            var icon = root.Find("icon")?.GetComponent<Image>();
            if (icon != null)
            {
                // Keep the native icon node/bindings. This safe inset fits even a fully
                // opaque square sprite inside the octagon's 18-percent clipped corners.
                var rect = icon.rectTransform;
                rect.localScale = Vector3.one;
                Place(rect, new Vector2(.14f, .14f), new Vector2(.86f, .86f), Vector2.zero, Vector2.zero);
                icon.preserveAspect = true;
            }
            if (_slotNumberFont == null)
                _slotNumberFont = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>()
                    .First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
            var binding = root.Find("binding")?.GetComponentInChildren<TMP_Text>(true);
            if (binding != null)
            {
                binding.font = _slotNumberFont; binding.fontSharedMaterial = _slotNumberFont.material;
                binding.fontSize = 18; binding.fontStyle = FontStyles.Normal;
                binding.alignment = TextAlignmentOptions.TopLeft; binding.color = Cream;
                Place(binding.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -29), new Vector2(28, -7));
            }
            var amount = root.Find("amount")?.GetComponentInChildren<TMP_Text>(true);
            if (amount != null)
            {
                amount.font = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                amount.fontSharedMaterial = amount.font.material;
                amount.fontSize = 12; amount.fontStyle = FontStyles.Normal;
                amount.alignment = TextAlignmentOptions.Center; amount.color = Cream;
                Place(amount.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(4, 5), new Vector2(-4, 21));
            }
            var durability = root.Find("durability")?.GetComponent<GuiBar>();
            if (durability != null)
            {
                Place((RectTransform)durability.transform, new Vector2(.5f, 0), new Vector2(.5f, 0),
                    new Vector2(-21, 10), new Vector2(21, 13));
                foreach (var image in durability.GetComponentsInChildren<Image>(true))
                {
                    image.sprite = null; image.type = Image.Type.Simple;
                    image.color = image.transform == durability.m_bar ? Cream : Color.clear;
                    image.raycastTarget = false;
                }
                durability.m_originalColor = Cream;
                durability.SetWidth(42);
                // Keep a full-width template for native GuiBar.Awake when slots are cloned.
                Place(durability.m_bar, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(42, 0));
                durability.m_bar.pivot = new Vector2(0, .5f);
            }
            var originalGrid = Auga.Assets.InventoryScreen.transform.Find("root/Player/PlayerGrid").GetComponent<InventoryGrid>();
            var originalShape = originalGrid.m_elementPrefab.GetComponent<Image>().sprite;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name.Equals("equiped", StringComparison.OrdinalIgnoreCase) || image.name.Equals("equipped", StringComparison.OrdinalIgnoreCase))
                {
                    var reference = originalGrid.m_elementPrefab.transform.Find("equiped").GetComponent<Image>();
                    image.sprite = reference.sprite; image.type = Image.Type.Simple; image.color = reference.color;
                    image.rectTransform.pivot = Vector2.zero;
                    // Match the octagonal background's two-unit inset, after setting the pivot.
                    Place(image.rectTransform, Vector2.zero, Vector2.zero, new Vector2(2, 2), new Vector2(11, 11));
                    continue;
                }
                if (image.name.IndexOf("selected", StringComparison.OrdinalIgnoreCase) < 0) continue;
                image.sprite = originalShape; image.type = Image.Type.Simple;
            }
            if (root.GetComponent<InventoryElement>() != null && root.GetComponent<AugaSlotIndicators>() == null)
                root.gameObject.AddComponent<AugaSlotIndicators>();
            var background = root.Find("bkg")?.GetComponent<Image>() ?? root.GetComponent<Image>();
            if (root.Find("Auga Slot") == null)
            {
                if (background != null) { background.sprite = null; background.color = Color.clear; }
                var slot = new GameObject("Auga Slot", typeof(RectTransform), typeof(AugaInventorySlot));
                slot.layer = root.gameObject.layer; slot.transform.SetParent(root, false); slot.transform.SetAsFirstSibling();
                Place((RectTransform)slot.transform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
                var graphic = slot.GetComponent<AugaInventorySlot>(); graphic.color = Dark; graphic.raycastTarget = false;
            }
        }

        public void StyleRecipes()
        {
            if (!_ready) return;
            _gui.m_recipeListRoot.anchoredPosition = Vector2.zero;
            foreach (Transform row in _gui.m_recipeListRoot)
            {
                var rect = (RectTransform)row;
                rect.anchorMin = new Vector2(0, rect.anchorMin.y); rect.anchorMax = new Vector2(1, rect.anchorMax.y);
                rect.offsetMin = new Vector2(0, rect.offsetMin.y); rect.offsetMax = new Vector2(0, rect.offsetMax.y);
                foreach (var icon in row.GetComponentsInChildren<Image>(true).Where(image => image.name.Equals("icon", StringComparison.OrdinalIgnoreCase)))
                {
                    icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0, .5f);
                    icon.rectTransform.pivot = new Vector2(.5f, .5f); icon.rectTransform.anchoredPosition = new Vector2(17, 0);
                    icon.rectTransform.sizeDelta = new Vector2(28, 28);
                    icon.preserveAspect = true;
                }
                var name = row.Find("name")?.GetComponent<TMP_Text>();
                if (name != null)
                {
                    Place(name.rectTransform, Vector2.zero, Vector2.one, new Vector2(36, 0), new Vector2(-6, 0));
                    Text(name, false, 14);
                    name.alignment = TextAlignmentOptions.MidlineLeft;
                    name.enableAutoSizing = false;
                    name.textWrappingMode = TextWrappingModes.Normal;
                    name.overflowMode = TextOverflowModes.Ellipsis;
                    name.margin = Vector4.zero;
                    name.lineSpacing = 0;
                }
                foreach (var image in row.GetComponentsInChildren<Image>(true))
                {
                    if (image.name.IndexOf("selected", StringComparison.OrdinalIgnoreCase) >= 0)
                    { image.sprite = null; image.color = new Color(.29f, .265f, .22f, .9f); Place(image.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); }
                }
            }
        }

        private void LateUpdate()
        {
            if (!_ready) return;
            LayoutChest();
            if (_frame != null && Minimap.instance != null && Minimap.instance.m_smallRoot != null)
            {
                var map = (RectTransform)Minimap.instance.m_smallRoot.transform;
                var border = map.Find("Auga Minimap Border") as RectTransform;
                (border != null ? border : map).GetWorldCorners(_mapCorners);
                var screen = RectTransformUtility.WorldToScreenPoint(null, _mapCorners[2]);
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)_frame.parent, screen, null, out var corner)) _frame.position = corner;
            }
            _gui.m_crafting.anchorMin = _gui.m_crafting.anchorMax = new Vector2(.5f, 0);
            _gui.m_crafting.pivot = new Vector2(.5f, 0);
            _gui.m_crafting.anchoredPosition = new Vector2(0, 18);
            _gui.m_crafting.sizeDelta = new Vector2(564, 870);
            if (!ApiCustomWorkbenchActive) UpdateReferenceRequirements();
            if (_recipeInfo != null && !ApiCustomWorkbenchActive)
            {
                var recipe = _gui.m_selectedRecipe.Recipe;
                _recipeInfo.gameObject.SetActive(recipe != null);
                if (recipe != null)
                {
                    var item = _gui.m_selectedRecipe.ItemData;
                    _recipeInfo.SetItem(item ?? recipe.m_item.m_itemData, item == null ? 1 : Mathf.Min(item.m_quality + 1, item.m_shared.m_maxQuality), _gui.m_selectedVariant);
                }
                _gui.m_recipeName.gameObject.SetActive(false); _gui.m_recipeDecription.gameObject.SetActive(false);
            }
            if (_craftVisibility != null)
            {
                bool station = Player.m_localPlayer != null && Player.m_localPlayer.GetCurrentCraftingStation() != null;
                _stationHeader.SetActive(station);
                if (station)
                {
                    _selectedTab = 1; _statusPanel.SetActive(false); _skillsPanel.SetActive(false); _messageLogPanel.SetActive(false);
                    _stationIcon.sprite = _gui.m_craftingStationIcon.sprite;
                    if (_stationLevel != null) _stationLevel.text = _gui.m_craftingStationLevel.text;
                    _stationRepair.interactable = _gui.m_repairButton.interactable;
                    _stationMode.text = Localization.instance.Localize(_gui.m_tabCraft.interactable ? "$inventory_upgradebutton" : "$inventory_craftbutton").ToUpperInvariant();
                    for (int i = 0; i < _stationTabs.Count; i++) _stationTabs[i].SetSelected(i == ApiWorkbenchIndex);
                }
                bool status = _selectedTab != 1;
                _panelTitle.text = station ? _gui.m_craftingStationName.text : _selectedTab == 0 ? _gui.m_playerName.text : Localization.instance.Localize(_selectedTab == 2 ? "$skills_panel" : _selectedTab == 3 ? "$messagelog_panel" : "$crafting_panel");
                for (int i = 0; i < _referenceTabs.Count; i++) { _referenceTabs[i].gameObject.SetActive(!station); _referenceTabs[i].SetSelected(i == _selectedTab); }
                _gui.m_crafting.gameObject.SetActive(!status);
                _craftVisibility.alpha = status ? 0 : 1;
                _craftVisibility.interactable = _craftVisibility.blocksRaycasts = !status;
            }
            ApiRefreshExtensions();
            foreach (var label in new[] { _gui.m_armor, _gui.m_weight })
            {
                label.color = Cream; label.canvasRenderer.SetAlpha(1);
            }
            foreach (var image in _hidden) if (image != null && image.enabled) image.enabled = false;
            foreach (var action in _actions)
            {
                action.border.color = action.button.interactable ? Color.white : new Color(.55f, .55f, .55f);
                foreach (var label in action.button.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (Hint(label.transform)) continue;
                    if (action.button == _gui.m_craftButton && label != _craftCaption) { label.enabled = false; continue; }
                    label.color = action.button.interactable ? Cream : Muted; label.canvasRenderer.SetAlpha(1);
                }
            }
            _gui.m_craftingStationName.gameObject.SetActive(false);
            _gui.m_craftCancelButton.gameObject.SetActive(_gui.m_craftTimer >= 0);
            _gui.m_craftButton.gameObject.SetActive(_gui.m_craftTimer < 0 && !ApiCustomWorkbenchActive);
            if (_craftCaption != null) _craftCaption.text = Localization.instance.Localize(_gui.m_tabCraft.interactable ? "$inventory_upgradebutton" : "$inventory_craftbutton");
            if (_gui.m_tabCraft.targetGraphic is TMP_Text craftLabel) craftLabel.color = _gui.m_tabCraft.interactable ? Muted : Cream;
            if (_gui.m_tabUpgrade.targetGraphic is TMP_Text upgradeLabel) upgradeLabel.color = _gui.m_tabUpgrade.interactable ? Muted : Cream;
        }
    }

    public sealed class InventoryReferenceLayout : MonoBehaviour
    {
        public RectTransform Panel;
        private int _count = -1;
        public void Apply(InventoryGrid grid)
        {
            if (_count == grid.m_elements.Count || grid.m_elements.Count == 0) return;
            _count = grid.m_elements.Count;
            foreach (var slot in grid.m_elements)
            {
                if (slot.Position.y > 0) ((RectTransform)slot.transform).anchoredPosition += new Vector2(0, -20);
                InventoryPresentation.StyleSlotBackground(slot.transform);
            }
            Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, grid.GetWidgetSize().y + 64);
            if (Panel.Find("Auga Hotbar Divider") == null)
            {
                var line = Box(Panel, "Auga Hotbar Divider", new Color(.59f, .56f, .49f, .6f));
                Place(line, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -grid.m_elementSpace - 20), new Vector2(-16, -grid.m_elementSpace - 19));
                line.GetComponent<Image>().raycastTarget = false;
            }
        }
    }

    // Keep native equipped/quality state, and derive the food marker from active foods.
    public sealed class AugaSlotIndicators : MonoBehaviour
    {
        private InventoryElement _element;
        private InventoryGrid _grid;
        private Image _food;
        private Image _quality;

        private void Start()
        {
            _element = GetComponent<InventoryElement>();
            _grid = GetComponentInParent<InventoryGrid>();
            var original = Auga.Assets.InventoryScreen.transform.Find("root/Player/PlayerGrid")
                .GetComponent<InventoryGrid>().m_elementPrefab.transform;
            var food = Box(transform, "Auga Active Food", Color.white);
            _food = food.GetComponent<Image>();
            var source = original.Find("foodindicator").GetComponent<Image>();
            _food.sprite = source.sprite; _food.color = source.color; _food.raycastTarget = false;
            Place(_food.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(9, 9));
            if (_element.m_quality != null)
            {
                var label = _element.m_quality;
                Place(label.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-11, -10), new Vector2(11, 12));
                label.fontSize = 14; label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(.918f, .882f, .851f); label.raycastTarget = false;
                var diamond = Box(transform, "Auga Quality Diamond", Color.white);
                _quality = diamond.GetComponent<Image>();
                source = original.Find("quality_bkg").GetComponent<Image>();
                _quality.sprite = source.sprite; _quality.color = source.color; _quality.raycastTarget = false;
                Place(_quality.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-10, -9), new Vector2(10, 11));
                // A sibling renders behind the number while following its native visibility.
                diamond.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
            }
        }

        private void LateUpdate()
        {
            if (_food == null) return;
            var item = _grid != null && _grid.m_inventory != null
                ? _grid.m_inventory.GetItemAt(_element.Position.x, _element.Position.y) : null;
            _food.enabled = item != null && Player.m_localPlayer != null &&
                Player.m_localPlayer.m_foods.Any(food => food.m_item.m_shared.m_name == item.m_shared.m_name);
            if (_quality != null) _quality.enabled = _element.m_quality.isActiveAndEnabled;
        }
    }

    // The characteristic clipped corners without relying on an obsolete slot prefab.
    public sealed class AugaInventorySlot : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; float c = Mathf.Min(r.width, r.height) * .18f;
            var points = new[] { new Vector2(r.xMin+c,r.yMin), new Vector2(r.xMax-c,r.yMin), new Vector2(r.xMax,r.yMin+c), new Vector2(r.xMax,r.yMax-c), new Vector2(r.xMax-c,r.yMax), new Vector2(r.xMin+c,r.yMax), new Vector2(r.xMin,r.yMax-c), new Vector2(r.xMin,r.yMin+c) };
            vh.AddVert(r.center, color, Vector2.zero);
            foreach (var point in points) vh.AddVert(point, color, Vector2.zero);
            for (int i = 0; i < 8; i++) vh.AddTriangle(0, i+1, (i+1)%8+1);
        }
    }
}
