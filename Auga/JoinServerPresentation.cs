using System;
using System.Linq;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Auga.CharacterSelectionPresentation;

namespace Auga
{
    [HarmonyPatch(typeof(ServerListGui), nameof(ServerListGui.UpdateServerListGuiInternal))]
    public static class JoinServerRowsPatch
    {
        private static void Postfix(ServerListGui __instance)
        {
            var view = __instance.GetComponent<JoinServerPresentation>();
            if (view != null) view.StyleRows();
        }
    }
    [HarmonyPatch(typeof(ServerListGui), nameof(ServerListGui.RecreateTabs))]
    public static class JoinServerTabsPatch
    {
        private static void Postfix(ServerListGui __instance)
        {
            var view = __instance.GetComponent<JoinServerPresentation>();
            if (view != null) view.StyleTabs();
        }
    }
    [HarmonyPatch(typeof(ServerListGui), nameof(ServerListGui.Awake))]
    public static class JoinServerPresentationPatch
    {
        private static void Postfix(ServerListGui __instance)
        {
            if (__instance.GetComponent<JoinServerPresentation>() == null)
                __instance.gameObject.AddComponent<JoinServerPresentation>();
        }
    }

    // Presentation only; native discovery, favorites, filtering and connection callbacks remain.
    public sealed class JoinServerPresentation : MonoBehaviour
    {
        private ServerListGui _gui;
        private TMP_FontAsset _body, _norse;
        private bool _ready;
        private Image _connectBorder;
        private Image[] _connectNativeImages;
        private Image[] _addDialogNativeImages;
        private Image _addConfirmBorder, _addCancelBorder;
        private static readonly Color Cream = new Color(.918f, .882f, .851f);
        private static readonly Color Muted = new Color(.59f, .56f, .49f);
        private static readonly Color Dark = new Color(.12f, .11f, .09f, .85f);

        private void Start()
        {
            try
            {
                _gui = GetComponent<ServerListGui>();
                _body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                _norse = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                var root = (RectTransform)transform;
                Place(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                foreach (Transform child in root)
                    if (child.name == "bkg" && child.GetComponent<Image>() is Image bg) bg.enabled = false;
                var title = root.Find("topic").GetComponent<TMP_Text>();
                Text(title, 40f, true);
                Place(title.rectTransform, new Vector2(.06f, 1f), new Vector2(.94f, 1f), new Vector2(0, -82), new Vector2(0, -20));
                title.alignment = TextAlignmentOptions.Center;
                var list = root.Find("ServerList").GetComponent<ScrollRect>();
                Place((RectTransform)list.transform, new Vector2(.07f, 0), new Vector2(.93f, 1), new Vector2(0, 204), new Vector2(-12, -268));
                list.GetComponent<Image>().sprite = null;
                list.GetComponent<Image>().color = Dark;
                Place(_gui.m_serverListRoot, new Vector2(0, 1), Vector2.one, new Vector2(10, -300), new Vector2(-10, 0));
                _gui.m_serverListRoot.pivot = new Vector2(.5f, 1);
                var scroll = root.Find("ServerListScroll").GetComponent<Scrollbar>();
                Place((RectTransform)scroll.transform, new Vector2(.93f, 0), new Vector2(.93f, 1), new Vector2(-8, 204), new Vector2(4, -268));
                SettingsPresentation.StyleScrollbar(scroll);
                Text(_gui.m_serverCount, 16);
                Place(_gui.m_serverCount.rectTransform, new Vector2(.07f, 1), new Vector2(.93f, 1), new Vector2(0, -265), new Vector2(-14, -243));
                _gui.m_serverCount.alignment = TextAlignmentOptions.MidlineRight;
                Action(root.Find("Back").GetComponent<Button>(), -110, 42, 200, true);
                Action(_gui.m_joinGameButton, 110, 42, 200, true);
                // Keep an owned border outside the native Connect animation's target graphic.
                var connect = _gui.m_joinGameButton;
                _connectBorder = Box(connect.transform, "Auga Connect Border", Color.white).GetComponent<Image>();
                var connectSource = Auga.Assets.ButtonFancy.GetComponent<Button>().targetGraphic as Image;
                _connectBorder.sprite = connectSource.sprite;
                _connectBorder.type = connectSource.type;
                _connectBorder.pixelsPerUnitMultiplier = connectSource.pixelsPerUnitMultiplier;
                _connectNativeImages = connect.GetComponentsInChildren<Image>(true).Where(image => image != _connectBorder && !Hint(image.transform)).ToArray();
                foreach (var label in connect.GetComponentsInChildren<TMP_Text>(true))
                    if (!Hint(label.transform)) label.transform.SetAsLastSibling();
                var lower = Box(root, "Auga Server List Lower Background", Dark);
                Place(lower, new Vector2(.07f, 0), new Vector2(.93f, 0), new Vector2(0, 180), new Vector2(-12, 204));
                var insetObject = new GameObject("Auga Add Server Inset", typeof(RectTransform), typeof(CharacterActionsInset));
                insetObject.layer = root.gameObject.layer;
                insetObject.transform.SetParent(root, false);
                At((RectTransform)insetObject.transform, 0, 192, 184, 24);
                insetObject.GetComponent<CharacterActionsInset>().color = new Color(.22f, .20f, .165f, 1f);
                Action(_gui.m_addServerButton, 0, 180, 152);
                Icon(_gui.m_serverRefreshButton, -215, 138);
                At((RectTransform)_gui.m_serverRefreshButton.transform, -215, -222, 34, 34, true);
                Icon(_gui.m_removeButton, 215, 138);
                Icon(_gui.m_favoriteButton, 44, 138);
                Icon(_gui.m_upButton, -28, 94);
                Icon(_gui.m_downButton, 28, 94);
                var filter = (RectTransform)_gui.m_filterInputField.transform;
                At(filter, 0, -222, 360, 32, true);
                var native = filter.GetComponent<Image>();
                native.enabled = false;
                var field = Box(filter, "Auga Server Filter", Color.white).GetComponent<Image>();
                field.transform.SetAsFirstSibling();
                var source = Auga.Assets.MainMenuPrefab.transform.Find("StartGame/Panel/WorldPanel/ServerPassword").GetComponent<Image>();
                field.sprite = source.sprite;
                field.type = source.type;
                field.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                Place((RectTransform)filter.Find("Text Area"), Vector2.zero, Vector2.one, new Vector2(22, 0), new Vector2(-22, 0));
                foreach (var label in filter.GetComponentsInChildren<TMP_Text>(true))
                    if (!Hint(label.transform)) { Text(label, 17); if (label.name == "Placeholder") { label.fontStyle = FontStyles.Italic; label.color = Muted; } }
                var help = (RectTransform)root.Find("Server help");
                help.anchorMin = help.anchorMax = new Vector2(1, 1);
                help.pivot = new Vector2(0, 1);
                help.anchoredPosition = new Vector2(24, -140);
                help.sizeDelta = new Vector2(340, 370);
                var helpImage = help.GetComponent<Image>();
                helpImage.sprite = null;
                helpImage.color = Dark;
                SettingsPresentation.AddCorners(help);
                var helpTitle = help.Find("topic").GetComponent<TMP_Text>();
                Text(helpTitle, 28, true);
                Place(helpTitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(20, -54), new Vector2(-20, -14));
                var helpText = help.Find("Text").GetComponent<TMP_Text>();
                Text(helpText, 19);
                Place(helpText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -64));
                _ready = true;
                StyleAddDialog();
                StyleTabs();
                StyleRows();
                Debug.Log("[Auga] Join server presentation applied; native discovery and connection controls retained.");
            }
            catch (Exception exception) { Auga.LogError($"Join server presentation failed: {exception}"); enabled = false; }
        }

        public void StyleTabs()
        {
            if (!_ready) return;
            var tabs = _gui.m_serverListTabs;
            var totalWidth = tabs.Count * 134f;
            for (int side = 0; side < 2; side++)
            {
                string dividerName = "Auga Server Category Divider " + side;
                var line = transform.Find(dividerName) as RectTransform;
                if (line == null)
                {
                    line = Box(transform, dividerName, new Color(Cream.r, Cream.g, Cream.b, .4f));
                    line.GetComponent<Image>().raycastTarget = false;
                    var diamond = Box(line, "Diamond", Muted);
                    var anchor = new Vector2(side == 0 ? 1f : 0f, .5f);
                    Place(diamond, anchor, anchor, new Vector2(-3, -3), new Vector2(3, 3));
                    diamond.localRotation = Quaternion.Euler(0, 0, 45);
                    diamond.GetComponent<Image>().raycastTarget = false;
                    var fill = Box(diamond, "Fill", new Color(.22f, .20f, .165f, 1));
                    Place(fill, Vector2.zero, Vector2.one, Vector2.one, -Vector2.one);
                    fill.GetComponent<Image>().raycastTarget = false;
                }
                Place(line, new Vector2(side == 0 ? .07f : .5f, 1), new Vector2(side == 0 ? .5f : .93f, 1),
                    new Vector2(side == 0 ? 0 : totalWidth / 2 + 10, -162.5f), new Vector2(side == 0 ? -totalWidth / 2 - 10 : 0, -161.5f));
            }
            for (int i = 0; i < tabs.Count; i++)
            {
                var button = tabs[i].GetComponent<Button>();
                At((RectTransform)button.transform, (i - (tabs.Count - 1) * .5f) * 134, -162, 132, 30, true);
                if (button.transform.Find("Auga Server Tab Hit") == null)
                {
                    foreach (var image in button.GetComponentsInChildren<Image>(true)) if (!Hint(image.transform)) image.enabled = false;
                    Box(button.transform, "Auga Server Tab Hit", Color.clear).SetAsFirstSibling();
                }
                var label = button.transform.Find("Text").GetComponent<TMP_Text>();
                Text(label, 18);
                label.fontStyle = FontStyles.UpperCase;
                label.alignment = TextAlignmentOptions.Center;
                var tint = button.GetComponent<ButtonTextColor>();
                if (tint != null) tint.enabled = false;
                button.targetGraphic = label;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = colors.disabledColor = Color.white;
                colors.highlightedColor = colors.selectedColor = new Color(1, .83f, .5f);
                button.colors = colors;
                button.transform.SetAsLastSibling();
            }
        }

        private void LateUpdate()
        {
            if (!_ready) return;
            _connectBorder.color = _gui.m_joinGameButton.IsInteractable() ? Color.white : new Color(.65f, .65f, .65f, 1f);
            // Row stars replace the standalone favorite control; native tab changes may reactivate it.
            if (_gui.m_favoriteButton.gameObject.activeSelf) _gui.m_favoriteButton.gameObject.SetActive(false);
            if (_gui.m_removeButton.gameObject.activeSelf) _gui.m_removeButton.gameObject.SetActive(false);
            foreach (var image in _connectNativeImages) if (image.enabled) image.enabled = false;
            if (_gui.m_addServerPanel.activeInHierarchy && _addDialogNativeImages != null)
            {
                foreach (var image in _addDialogNativeImages) if (image.enabled) image.enabled = false;
                _addConfirmBorder.color = _gui.m_addServerConfirmButton.IsInteractable() ? Color.white : new Color(.65f, .65f, .65f, 1);
                _addCancelBorder.color = Color.white;
            }
            foreach (var tab in _gui.m_serverListTabs)
            {
                var button = tab.GetComponent<Button>();
                var label = tab.transform.Find("Text").GetComponent<TMP_Text>();
                label.color = button.interactable ? Muted : Cream;
            }
        }

        public void StyleRows()
        {
            if (!_ready) return;
            for (int i = 0; i < _gui.m_serverListElements.Count; i++)
            {
                var row = _gui.m_serverListElements[i];
                var rect = row.m_rectTransform;
                rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(0, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(0, -i * _gui.m_serverListElementStep - 10);
                var selected = row.m_selected.GetComponent<Image>();
                selected.sprite = null; selected.color = new Color(.18f, .34f, .43f);
                Place(row.m_selected, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var bg = row.m_element.transform.Find("bkg").GetComponent<Image>(); bg.sprite = null; bg.color = Color.clear;
                foreach (var label in row.m_element.GetComponentsInChildren<TMP_Text>(true)) Text(label, 16);
                Place(row.m_serverName.rectTransform, Vector2.zero, new Vector2(.43f, 1), new Vector2(42, 0), new Vector2(-4, 0));
                Place(row.m_modifiers.rectTransform, new Vector2(.43f, 0), new Vector2(.65f, 1), Vector2.zero, new Vector2(-4, 0));
                Place(row.m_version.rectTransform, new Vector2(.65f, 0), new Vector2(.78f, 1), Vector2.zero, new Vector2(-4, 0));
                Place(row.m_players.rectTransform, new Vector2(.78f, 0), new Vector2(.87f, 1), Vector2.zero, new Vector2(-4, 0));
                Place(row.m_status.rectTransform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-66, -10), new Vector2(-46, 10));
                Place((RectTransform)row.m_crossplay, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-90, -10), new Vector2(-70, 10));
                Place((RectTransform)row.m_private, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-108, -10), new Vector2(-94, 10));
                var starRoot = row.m_element.transform.Find("Auga Favorite Toggle") as RectTransform;
                if (starRoot == null)
                {
                    starRoot = Box(row.m_element.transform, "Auga Favorite Toggle", Color.clear);
                    Place(starRoot, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(36, 0));
                    starRoot.gameObject.AddComponent<Button>();
                    var star = Box(starRoot, "Star", Color.white).GetComponent<Image>();
                    Place(star.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-9, -9), new Vector2(9, 9));
                    star.sprite = _gui.m_favoriteButton.transform.Find("Image").GetComponent<Image>().sprite;
                    star.raycastTarget = false;
                }
                var starImage = starRoot.Find("Star").GetComponent<Image>();
                var entry = row.m_serverListEntry;
                starImage.color = _gui.m_favoriteServersList.Contains(entry.m_joinData) ? new Color(.72f, .56f, .19f) : Muted;
                var starButton = starRoot.GetComponent<Button>();
                starButton.targetGraphic = starImage;
                // Rows are pooled: replace the binding whenever native list data is refreshed.
                starButton.onClick.RemoveAllListeners();
                starButton.onClick.AddListener(() => ToggleFavorite(entry));
                var deleteRoot = row.m_element.transform.Find("Auga Delete Server") as RectTransform;
                if (deleteRoot == null)
                {
                    deleteRoot = Box(row.m_element.transform, "Auga Delete Server", Color.clear);
                    Place(deleteRoot, new Vector2(1, 0), Vector2.one, new Vector2(-36, 0), Vector2.zero);
                    deleteRoot.gameObject.AddComponent<Button>();
                    var icon = Box(deleteRoot, "Delete", Cream).GetComponent<Image>();
                    Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-14, -14), new Vector2(14, 14));
                    icon.sprite = _gui.m_removeButton.transform.Find("Image").GetComponent<Image>().sprite;
                    icon.raycastTarget = false;
                }
                var delete = deleteRoot.GetComponent<Button>();
                delete.targetGraphic = deleteRoot.Find("Delete").GetComponent<Image>();
                delete.interactable = _gui.m_serverLists[_gui.m_currentServerList] == _gui.m_favoriteServersList || _gui.m_serverLists[_gui.m_currentServerList] == _gui.m_recentServersList;
                delete.onClick.RemoveAllListeners();
                delete.onClick.AddListener(() => ConfirmDeleteServer(entry));
            }
            _gui.m_serverListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(_gui.m_serverListBaseSize, _gui.m_serverListElements.Count * _gui.m_serverListElementStep + 20));
        }

        private void ToggleFavorite(ServerListEntryData entry)
        {
            var favorites = _gui.m_favoriteServersList;
            if (favorites.Contains(entry.m_joinData))
            {
                favorites.Remove(entry.m_joinData);
                if (_gui.m_serverLists[_gui.m_currentServerList] == favorites) _gui.ClearSelectedServer();
            }
            else
            {
                MultiBackendMatchmaking.SetServerName(entry.m_joinData, new ServerNameAtTimePoint(entry.m_serverName, entry.m_timeStampUtc));
                favorites.Add(entry.m_joinData);
            }
            _gui.m_filteredListOutdated = true;
            _gui.m_updateServerListGui = true;
            _gui.SetButtonsOutdated();
            StyleRows();
        }

        private void ConfirmDeleteServer(ServerListEntryData entry)
        {
            int index = _gui.CurrentServerListFiltered.FindIndex(candidate => candidate.m_joinData.Equals(entry.m_joinData));
            if (index < 0) return;
            _gui.SetSelectedServer(index, true);
            _gui.OnRemoveServerButton();
        }
        private void StyleAddDialog()
        {
            var dialog = _gui.m_addServerPanel.transform;
            var input = _gui.m_addServerTextInput;
            var nativeImages = dialog.GetComponentsInChildren<Image>(true).Where(image => !Hint(image.transform)).ToArray();
            foreach (var image in nativeImages)
            {
                if (image.GetComponentInParent<Selectable>(true) != null) continue;
                if (image.sprite == null || !image.sprite.name.Contains("woodpanel")) continue;
                var panel = Box(image.transform, "Auga Add Server Panel", new Color(.22f, .20f, .165f, .98f));
                panel.SetAsFirstSibling();
                SettingsPresentation.AddCorners(panel);
            }
            foreach (var label in dialog.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Hint(label.transform)) continue;
                bool heading = !label.transform.IsChildOf(input.transform) && label.GetComponentInParent<Selectable>(true) == null;
                Text(label, heading ? 32 : 18, heading);
            }
            var field = Box(input.transform, "Auga Add Server Input", Color.white).GetComponent<Image>();
            field.transform.SetAsFirstSibling();
            var source = Auga.Assets.MainMenuPrefab.transform.Find("StartGame/Panel/WorldPanel/ServerPassword").GetComponent<Image>();
            field.sprite = source.sprite; field.type = source.type; field.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            var textArea = input.transform.Find("Text Area") as RectTransform;
            if (textArea != null) Place(textArea, Vector2.zero, Vector2.one, new Vector2(28, 0), new Vector2(-28, 0));
            if (input.placeholder is TMP_Text placeholder) { placeholder.fontStyle = FontStyles.Italic; placeholder.color = Muted; }
            _addCancelBorder = DialogButton(_gui.m_addServerCancelButton);
            _addConfirmBorder = DialogButton(_gui.m_addServerConfirmButton);
            _addDialogNativeImages = nativeImages;
            foreach (var image in nativeImages) image.enabled = false;
            Debug.Log("[Auga] Add server dialog styled; native address validation and add/cancel retained.");
        }

        private Image DialogButton(Button button)
        {
            var rect = (RectTransform)button.transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48);
            var border = Box(rect, "Auga Add Server Action", Color.white).GetComponent<Image>();
            var source = Auga.Assets.ButtonFancy.GetComponent<Button>().targetGraphic as Image;
            border.sprite = source.sprite; border.type = source.type; border.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            StyleButtonStates(button, border);
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Hint(label.transform)) continue;
                Text(label, 28, true);
                Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0));
                label.alignment = TextAlignmentOptions.Center;
                label.transform.SetAsLastSibling();
            }
            var tint = button.GetComponent<ButtonTextColor>();
            if (tint != null) { tint.m_defaultColor = tint.m_defaultMeshColor = Cream; tint.m_disabledColor = Muted; }
            return border;
        }

        private void Action(Button button, float x, float y, float width, bool fancy = false)
        {
            StyleButton(button, transform, x, y, width, fancy, _body, _norse);
            button.transform.SetAsLastSibling();
            var source = (button.targetGraphic as Image);
            var sprites = button.spriteState;
            sprites.highlightedSprite = sprites.selectedSprite = sprites.pressedSprite = sprites.disabledSprite = source.sprite;
            button.spriteState = sprites;
            foreach (var image in button.GetComponentsInChildren<Image>(true))
            {
                if (Hint(image.transform)) continue;
                image.sprite = source.sprite; image.type = source.type; image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            }
            var colors = button.colors; colors.disabledColor = new Color(.65f, .65f, .65f, 1f); button.colors = colors;
            var tint = button.GetComponent<ButtonTextColor>();
            if (tint != null) { tint.m_defaultColor = tint.m_defaultMeshColor = Cream; tint.m_disabledColor = Muted; tint.m_sprite = (button.targetGraphic as Image).sprite; }
        }
        private void Icon(Button button, float x, float y)
        {
            At((RectTransform)button.transform, x, y, 34, 34);
            var original = button.GetComponent<Image>(); if (original != null) original.enabled = false;
            var bg = Box(button.transform, "Auga Server Icon Background", new Color(.16f, .15f, .12f));
            bg.SetAsFirstSibling();
            var icon = button.transform.Find("Image")?.GetComponent<Image>();
            var nativeTint = button.GetComponent<ButtonImageColor>();
            if (nativeTint != null) nativeTint.enabled = false;
            if (icon != null) { icon.color = Cream; Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-10, -10), new Vector2(10, 10)); }
            button.targetGraphic = bg.GetComponent<Image>();
            button.transition = Selectable.Transition.ColorTint;
        }
        private void Text(TMP_Text label, float size, bool norse = false)
        {
            label.font = norse ? _norse : _body; label.fontSharedMaterial = label.font.material;
            label.fontSize = size; label.color = Cream; label.fontStyle = FontStyles.Normal; label.enableAutoSizing = false;
        }
        private static void At(RectTransform rect, float x, float y, float width, float height, bool top = false)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, top ? 1 : 0); rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
        private static bool Hint(Transform t) { for (; t != null; t = t.parent) if (t.name.StartsWith("gamepad_hint")) return true; return false; }
    }
}
