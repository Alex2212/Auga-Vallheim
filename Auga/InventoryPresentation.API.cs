using System;
using System.Collections.Generic;
using System.Linq;
using AugaUnity;
using UnityEngine;
using UnityEngine.UI;
using static Auga.CharacterSelectionPresentation;

namespace Auga
{
    public sealed partial class InventoryPresentation
    {
        private sealed class PlayerExtension
        {
            public PlayerPanelTabData Data;
            public Action<int> Selected;
        }
        private sealed class WorkbenchExtension
        {
            public WorkbenchTabData Data;
            public GameObject Content;
            public Action<int> Selected;
        }
        private readonly Dictionary<string, PlayerExtension> _apiPlayerTabs = new Dictionary<string, PlayerExtension>(StringComparer.Ordinal);
        private readonly Dictionary<string, WorkbenchExtension> _apiWorkbenchTabs = new Dictionary<string, WorkbenchExtension>(StringComparer.Ordinal);
        private GameObject _apiNativeDetails, _apiRecipeList, _apiRecipeDivider;
        private AugaCraftingPanel _apiCraftSource;
        private int _apiWorkbenchIndex = -1;
        private CraftingStation _apiStation;
        private Button _apiVariantButton;
        private GameObject _apiVariantDialog;
        private Text _apiVariantText;
        private Action<bool> _apiVariantCallback;
        private bool _apiVariantEnabled;

        private int ApiWorkbenchIndex => _apiWorkbenchIndex >= 2 ? _apiWorkbenchIndex : (_gui.m_tabCraft.interactable ? 1 : 0);
        private bool ApiCustomWorkbenchActive => _apiWorkbenchIndex >= 2 && Player.m_localPlayer != null && Player.m_localPlayer.GetCurrentCraftingStation() != null;

        internal bool ApiHasTab(string id, bool workbench) => !string.IsNullOrEmpty(id) &&
            (workbench ? _stationTabs : _referenceTabs).Any(tab => tab.name == id);

        private Text ApiTitle(string id, string title)
        {
            var obj = new GameObject(id + ".Title", typeof(RectTransform), typeof(Text));
            obj.layer = _frame.gameObject.layer;
            obj.transform.SetParent(_frame, false);
            var text = obj.GetComponent<Text>();
            text.font = Auga.Assets.SourceSansProBold;
            text.text = Localization.instance.Localize(title ?? "");
            text.raycastTarget = false;
            // Legacy Text contract remains writable; current TMP headers render its value.
            obj.SetActive(false);
            return text;
        }

        private TabButton ApiAddButton(string id, Sprite icon, bool workbench, Action select)
        {
            var tabs = workbench ? _stationTabs : _referenceTabs;
            var tab = Instantiate(tabs[0], workbench ? _stationHeader.transform : _frame, false);
            tab.name = id;
            tab.enabled = false; // Selection is owned here, not a cloned built-in Update method.
            tab.Button.onClick = new Button.ButtonClickedEvent();
            tab.Button.onClick.AddListener(() => select());
            tab.SetSelected(false);
            if (icon != null) tab.SetIcon(icon);
            tab.gameObject.SetActive(true);
            tabs.Add(tab);
            ApiLayoutButtons(tabs, workbench);
            return tab;
        }

        private static void ApiValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A non-empty, stable tab ID is required.", nameof(id));
        }

        internal PlayerPanelTabData ApiAddPlayerTab(string id, Sprite icon, string title, Action<int> selected)
        {
            ApiValidateId(id);
            if (_apiPlayerTabs.TryGetValue(id, out var existing)) return existing.Data;
            if (ApiHasTab(id, false)) throw new ArgumentException("The tab ID is already used by a built-in tab.", nameof(id));
            if (_referenceTabs.Count >= 12) throw new InvalidOperationException("The player tab bar supports up to 12 tabs, including built-ins.");
            var content = new GameObject(id + ".Content", typeof(RectTransform));
            content.layer = _frame.gameObject.layer;
            content.transform.SetParent(_frame, false);
            Place((RectTransform)content.transform, Vector2.zero, Vector2.one, new Vector2(18, 40), new Vector2(-18, -140));
            content.SetActive(false);
            var entry = new PlayerExtension { Data = new PlayerPanelTabData {
                Index = _referenceTabs.Count, ContentGO = content, TabTitle = ApiTitle(id, title)
            }, Selected = selected };
            var tab = ApiAddButton(id, icon, false, () => {
                if (_selectedTab == entry.Data.Index) return;
                ApiCloseVariant();
                _selectedTab = entry.Data.Index;
                ApiRefreshExtensions();
                entry.Selected?.Invoke(entry.Data.Index);
            });
            entry.Data.TabButtonGO = tab.gameObject;
            _apiPlayerTabs.Add(id, entry);
            return entry.Data;
        }

        internal WorkbenchTabData ApiAddWorkbenchTab(string id, Sprite icon, string title, Action<int> selected, bool mimic)
        {
            ApiValidateId(id);
            if (_apiWorkbenchTabs.TryGetValue(id, out var existing)) return existing.Data;
            if (ApiHasTab(id, true)) throw new ArgumentException("The tab ID is already used by a built-in tab.", nameof(id));
            if (_stationTabs.Count >= 10) throw new InvalidOperationException("The workbench tab bar supports up to 10 tabs, including built-ins.");
            var content = new GameObject(id + ".Content", typeof(RectTransform));
            content.layer = _gui.gameObject.layer;
            content.transform.SetParent(_gui.m_crafting, false);
            Place((RectTransform)content.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            content.SetActive(false);
            var requirements = Instantiate(_apiCraftSource.GenericCraftingRequirementsPanel, content.transform, false);
            requirements.enabled = false; // The integrating mod owns its values, not native recipe refresh.
            requirements.gameObject.SetActive(true);
            var info = Instantiate(_recipeInfo, content.transform, false);
            info.Icon = requirements.Icon;
            info.ClearTextBoxes();
            info.gameObject.SetActive(true);
            if (mimic)
            {
                var mirror = content.AddComponent<MimicVanillaCraftingTab>();
                mirror.OutputTooltip = info;
            }
            var entry = new WorkbenchExtension { Content = content, Selected = selected,
                Data = new WorkbenchTabData { Index = _stationTabs.Count, TabTitle = ApiTitle(id, title),
                    RequirementsPanelGO = requirements.gameObject, ItemInfoGO = info.gameObject } };
            var tab = ApiAddButton(id, icon, true, () => {
                if (_apiWorkbenchIndex == entry.Data.Index) return;
                ApiCloseVariant();
                _apiWorkbenchIndex = entry.Data.Index;
                ApiRefreshExtensions();
                entry.Selected?.Invoke(entry.Data.Index);
            });
            entry.Data.TabButtonGO = tab.gameObject;
            _apiWorkbenchTabs.Add(id, entry);
            return entry.Data;
        }

        private static void ApiLayoutButtons(List<TabButton> tabs, bool workbench)
        {
            const float step = 42;
            for (int i = 0; i < tabs.Count; i++)
            {
                var rect = (RectTransform)tabs[i].transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
                rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(38, 38);
                rect.anchoredPosition = new Vector2((i - (tabs.Count - 1) * .5f) * step, -98);
                var navigation = tabs[i].Button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = tabs[(i + tabs.Count - 1) % tabs.Count].Button;
                navigation.selectOnRight = tabs[(i + 1) % tabs.Count].Button;
                tabs[i].Button.navigation = navigation;
            }
        }

        private void ApiRefreshExtensions()
        {
            if (!_ready) return;
            var station = Player.m_localPlayer != null ? Player.m_localPlayer.GetCurrentCraftingStation() : null;
            if (station != _apiStation)
            {
                _apiStation = station;
                _apiWorkbenchIndex = -1;
                ApiCloseVariant();
            }
            bool workbench = station != null;
            bool customWorkbench = ApiCustomWorkbenchActive;
            bool variantOpen = _apiVariantEnabled && _apiVariantDialog != null && _apiVariantDialog.activeSelf;
            var playerTab = !workbench ? _apiPlayerTabs.Values.FirstOrDefault(entry => entry.Data.Index == _selectedTab) : null;
            foreach (var entry in _apiPlayerTabs.Values) entry.Data.ContentGO.SetActive(entry == playerTab);
            if (playerTab != null)
            {
                _statusPanel.SetActive(false); _skillsPanel.SetActive(false); _messageLogPanel.SetActive(false);
                _panelTitle.text = playerTab.Data.TabTitle.text;
            }
            foreach (var entry in _apiWorkbenchTabs.Values)
            {
                bool active = customWorkbench && entry.Data.Index == _apiWorkbenchIndex;
                entry.Content.SetActive(active && !variantOpen);
                if (active) _stationMode.text = entry.Data.TabTitle.text;
            }
            _apiNativeDetails.SetActive(!customWorkbench);
            if (_apiRecipeList != null) _apiRecipeList.SetActive(!customWorkbench);
            if (_apiRecipeDivider != null) _apiRecipeDivider.SetActive(!customWorkbench);
            foreach (var tab in _referenceTabs) tab.SetSelected(!workbench && _referenceTabs.IndexOf(tab) == _selectedTab);
            foreach (var tab in _stationTabs) tab.SetSelected(workbench && _stationTabs.IndexOf(tab) == ApiWorkbenchIndex);
            if (playerTab != null)
            {
                _gui.m_crafting.gameObject.SetActive(false);
                _craftVisibility.alpha = 0;
                _craftVisibility.interactable = _craftVisibility.blocksRaycasts = false;
            }
            if (_apiVariantButton != null)
            {
                bool visible = _apiVariantEnabled && _selectedTab == 1;
                _apiVariantButton.gameObject.SetActive(visible);
                if (!visible) ApiCloseVariant();
                if (_apiVariantEnabled) _referenceVariant.gameObject.SetActive(false);
                if (_apiVariantDialog.activeSelf)
                {
                    _apiVariantDialog.transform.SetAsLastSibling();
                    _recipeInfo.gameObject.SetActive(false);
                    foreach (var panel in new[] { _craftRequirements, _upgradeRequirements, _maxRequirements }) panel.gameObject.SetActive(false);
                }
            }
        }

        internal GameObject ApiCreateResultsPanel()
        {
            var tab = _apiWorkbenchTabs.Values.FirstOrDefault(entry => entry.Data.Index == _apiWorkbenchIndex);
            return Instantiate(_apiCraftSource.ResultsPanelPrefab, tab != null ? tab.Content.transform : _gui.m_crafting, false);
        }

        internal Text ApiEnableVariant(string label, Action<bool> onShow)
        {
            if (_apiVariantButton == null)
            {
                _apiVariantButton = Instantiate(_apiCraftSource.CustomVariantButton, _gui.m_crafting, false);
                _apiVariantButton.name = "Auga API Variant Button";
                Place((RectTransform)_apiVariantButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-250, -76), new Vector2(-24, -36));
                _apiVariantButton.onClick = new Button.ButtonClickedEvent();
                _apiVariantDialog = Instantiate(_apiCraftSource.CustomVariantDialog, _gui.m_crafting, false);
                _apiVariantDialog.name = "Auga API Variant Dialog";
                Place((RectTransform)_apiVariantDialog.transform, Vector2.zero, Vector2.one, new Vector2(194, 85), new Vector2(-12, -100));
                _apiVariantText = _apiVariantDialog.GetComponentsInChildren<Text>(true).First(text => text.name == _apiCraftSource.CustomVariantText.name);
                _apiVariantDialog.SetActive(false);
                _apiVariantButton.onClick.AddListener(() => {
                    bool show = !_apiVariantDialog.activeSelf;
                    _apiVariantDialog.SetActive(show);
                    _apiVariantCallback?.Invoke(show);
                });
            }
            _apiVariantEnabled = true;
            _apiVariantCallback = onShow;
            ApiSetVariantLabel(label);
            ApiRefreshExtensions();
            return _apiVariantText;
        }

        internal void ApiSetVariantLabel(string label)
        {
            if (_apiVariantButton != null) _apiVariantButton.GetComponentInChildren<Text>(true).text = Localization.instance.Localize(label ?? "");
        }

        private void ApiCloseVariant()
        {
            if (_apiVariantDialog != null) _apiVariantDialog.SetActive(false);
        }

        internal void ApiDisableVariant()
        {
            ApiCloseVariant();
            _apiVariantEnabled = false;
            _apiVariantCallback = null;
            if (_apiVariantButton != null) _apiVariantButton.gameObject.SetActive(false);
            if (_referenceVariant != null) _referenceVariant.gameObject.SetActive(_gui.m_variantButton.gameObject.activeSelf);
        }
    }
}
