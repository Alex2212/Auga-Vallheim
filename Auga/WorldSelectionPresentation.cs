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
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdateWorldList))]
    public static class WorldSelectionRowsPatch
    {
        private static void Postfix(FejdStartup __instance)
        {
            var presentation = __instance.GetComponent<WorldSelectionPresentation>();
            if (presentation != null) presentation.StyleRows();
        }
    }

    // Native list, selection, tab navigation, server options and dialogs retain ownership of behavior.
    public sealed class WorldSelectionPresentation : MonoBehaviour
    {
        private FejdStartup _startup;
        private TMP_FontAsset _body, _norse;
        private TMP_Text _hostLabel, _joinLabel;
        private TMP_Text _nativePasswordPlaceholder;
        private TMP_Text _passwordHint;
        private bool _ready;
        private static readonly Color PanelColor = new Color(.22f, .20f, .165f, .98f);
        private static readonly Color Cream = new Color(.918f, .882f, .851f);
        private static readonly Color Muted = new Color(.59f, .56f, .49f);
        private static readonly Color Gold = new Color(.72f, .56f, .19f);
        private static readonly Color Dark = new Color(.12f, .11f, .09f, .65f);

        private void Start()
        {
            try
            {
                _startup = GetComponent<FejdStartup>();
                var root = _startup.m_startGamePanel.transform;
                var panel = (RectTransform)root.Find("Panel");
                var world = (RectTransform)_startup.m_worldListPanel.transform;
                var list = world.Find("WorldList").GetComponent<ScrollRect>();
                var host = panel.Find("Host").GetComponent<Button>();
                var join = panel.Find("Join").GetComponent<Button>();
                var back = world.Find("Back").GetComponent<Button>();
                var create = world.Find("New world").GetComponent<Button>();
                var manage = world.Find("ManageSaves").GetComponent<Button>();
                _body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                _norse = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));

                // The same placement and panel proportions as the approved character screen.
                Place(panel, new Vector2(.04f, .08f), new Vector2(.38f, .92f), Vector2.zero, Vector2.zero);
                panel.localScale = Vector3.one;
                foreach (Transform child in panel)
                    if (child.GetComponent<Image>() is Image image) image.enabled = false;
                var background = Box(panel, "Auga World Panel", PanelColor);
                background.SetAsFirstSibling();
                SettingsPresentation.AddCorners(background);
                Place(world, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var oldBackground = world.Find("bkg").GetComponent<Image>();
                oldBackground.enabled = false;
                var title = world.Find("topic").GetComponent<TMP_Text>();
                Text(title, true, 40f);
                Place(title.rectTransform, new Vector2(.06f, 1f), new Vector2(.94f, 1f), new Vector2(0f, -82f), new Vector2(0f, -20f));

                _hostLabel = Tab(host, -74f);
                _joinLabel = Tab(join, 74f);
                Divider(panel, true);
                Divider(panel, false);

                Place((RectTransform)list.transform, new Vector2(.07f, 0f), new Vector2(.93f, 1f), new Vector2(0f, 270f), new Vector2(-12f, -140f));
                var listImage = list.GetComponent<Image>();
                listImage.sprite = null;
                listImage.color = Dark;
                listImage.enabled = true;
                Place(_startup.m_worldListRoot, new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -300f), new Vector2(-10f, -10f));
                _startup.m_worldListRoot.pivot = new Vector2(.5f, 1f);
                var scroll = world.Find("worldScroll").GetComponent<Scrollbar>();
                Place((RectTransform)scroll.transform, new Vector2(.93f, 0f), new Vector2(.93f, 1f), new Vector2(-8f, 270f), new Vector2(4f, -140f));
                SettingsPresentation.StyleScrollbar(scroll);
                var lower = Box(world, "Auga World List Lower Background", Dark);
                Place(lower, new Vector2(.07f, 0f), new Vector2(.93f, 0f), new Vector2(0f, 246f), new Vector2(-12f, 270f));
                var insetObject = new GameObject("Auga World Actions Inset", typeof(RectTransform), typeof(CharacterActionsInset));
                insetObject.layer = world.gameObject.layer;
                insetObject.transform.SetParent(world, false);
                At((RectTransform)insetObject.transform, 0f, 258f, 332f, 24f);
                insetObject.GetComponent<CharacterActionsInset>().color = new Color(PanelColor.r, PanelColor.g, PanelColor.b, 1f);
                Action(_startup.m_worldRemove, world, -80f, 246f, 148f);
                Action(create, world, 80f, 246f, 148f);
                Action(manage, world, -112f, 204f, 212f);
                Action(_startup.m_serverOptionsButton, world, 112f, 204f, 212f);
                Action(back, world, -110f, 42f, 200f, true);
                Action(_startup.m_worldStart, world, 110f, 42f, 200f, true);

                ToggleStyle(_startup.m_openServerToggle, -174f, 160f, 150f);
                ToggleStyle(_startup.m_publicServerToggle, -10f, 160f, 180f);
                ToggleStyle(_startup.m_crossplayServerToggle, 170f, 160f, 130f);
                // Platform-specific alternative keeps its native visibility rule.
                if (_startup.m_samePlatformOnlyToggleWorldPanel != null)
                    ToggleStyle(_startup.m_samePlatformOnlyToggleWorldPanel, 155f, 130f, 200f);
                var password = (RectTransform)_startup.m_serverPassword.transform;
                At(password, 0f, 108f, 270f, 32f);
                var passwordImage = password.GetComponent<Image>();
                var passwordSource = Auga.Assets.MainMenuPrefab.transform.Find("StartGame/Panel/WorldPanel/ServerPassword").GetComponent<Image>();
                passwordImage.enabled = false;
                var ownedPassword = Box(password, "Auga Password Background", Color.white).GetComponent<Image>();
                ownedPassword.transform.SetAsFirstSibling();
                ownedPassword.sprite = passwordSource.sprite;
                ownedPassword.type = passwordSource.type;
                ownedPassword.pixelsPerUnitMultiplier = passwordSource.pixelsPerUnitMultiplier;
                var textArea = (RectTransform)password.Find("Text Area");
                Place(textArea, Vector2.zero, Vector2.one, new Vector2(28f, 0f), new Vector2(-28f, 0f));
                foreach (var label in password.GetComponentsInChildren<TMP_Text>(true))
                    if (!IsHint(label.transform)) Text(label, false, 18f);
                var placeholder = password.Find("Text Area/Placeholder").GetComponent<TMP_Text>();
                _nativePasswordPlaceholder = placeholder;
                // Native localization updates its placeholder on activation; keep our visual beneath its visibility owner.
                placeholder.enabled = false;
                var hintObject = new GameObject("Auga Password Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
                hintObject.layer = password.gameObject.layer;
                hintObject.transform.SetParent(placeholder.transform, false);
                var hint = hintObject.GetComponent<TextMeshProUGUI>();
                _passwordHint = hint;
                Place(hint.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Text(hint, false, 18f);
                hint.text = Localization.instance.Localize("$menu_serverpassword");
                hint.fontStyle = FontStyles.Italic;
                hint.color = new Color(.439f, .392f, .341f);
                hint.alignment = TextAlignmentOptions.MidlineLeft;
                hint.raycastTarget = false;
                var passwordLabel = world.Find("Text").GetComponent<TMP_Text>();
                passwordLabel.enabled = false;
                var error = world.Find("PasswordError").GetComponent<TMP_Text>();
                Text(error, false, 14f);
                error.color = new Color(1f, .5f, .4f);
                At(error.rectTransform, 0f, 78f, 420f, 20f);
                _ready = true;
                StyleRows();
                Debug.Log("[Auga] World selection presentation applied; native world and server actions retained.");
            }
            catch (Exception exception)
            {
                Auga.LogError($"World selection presentation failed: {exception}");
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (!_ready || !_startup.m_startGamePanel.activeInHierarchy) return;
            if (_nativePasswordPlaceholder != null && _nativePasswordPlaceholder.enabled)
                _nativePasswordPlaceholder.enabled = false;
            bool showPasswordHint = string.IsNullOrEmpty(_startup.m_serverPassword.text);
            if (_passwordHint != null && _passwordHint.enabled != showPasswordHint)
                _passwordHint.enabled = showPasswordHint;
            bool host = _startup.m_worldListPanel.activeSelf;
            _hostLabel.color = host ? Cream : Muted;
            _joinLabel.color = host ? Muted : Cream;
        }

        public void StyleRows()
        {
            if (!_ready) return;
            for (int i = 0; i < _startup.m_worldListElements.Count; i++)
            {
                var row = _startup.m_worldListElements[i];
                // Native rows retain a fixed prefab width unless explicitly stretched.
                var rect = (RectTransform)row.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(0f, -i * _startup.m_worldListElementStep - 10f);
                var selected = row.transform.Find("selected").GetComponent<Image>();
                Place(selected.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                selected.sprite = null;
                selected.color = new Color(.18f, .34f, .43f);
                var background = row.transform.Find("bkg").GetComponent<Image>();
                Place(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                background.sprite = null;
                background.color = Color.clear;
                foreach (var label in row.GetComponentsInChildren<TMP_Text>(true)) Text(label, false, 17f);
                var seed = row.transform.Find("seed").GetComponent<TMP_Text>();
                seed.alignment = TextAlignmentOptions.MidlineRight;
                Place(seed.rectTransform, new Vector2(.65f, 0f), Vector2.one, new Vector2(4f, 1f), new Vector2(-36f, -1f));
                var name = row.transform.Find("name").GetComponent<TMP_Text>();
                name.alignment = TextAlignmentOptions.MidlineLeft;
                Place(name.rectTransform, Vector2.zero, new Vector2(.43f, 1f), new Vector2(12f, 1f), new Vector2(-8f, -1f));
                var modifiers = row.transform.Find("modifiers").GetComponent<TMP_Text>();
                Place(modifiers.rectTransform, new Vector2(.43f, 0f), new Vector2(.65f, 1f), new Vector2(4f, 1f), new Vector2(-4f, -1f));
                foreach (var sourceName in new[] { "source_cloud", "source_local", "source_legacy" })
                {
                    var icon = row.transform.Find(sourceName) as RectTransform;
                    if (icon == null) continue;
                    Place(icon, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-30f, -10f), new Vector2(-10f, 10f));
                }
                // Preserve native modifier summaries and cloud/local indicators alongside the seed.
            }
            _startup.m_worldListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(_startup.m_worldListBaseSize, _startup.m_worldListElements.Count * _startup.m_worldListElementStep + 20f));
        }

        private void Action(Button button, Transform parent, float x, float y, float width, bool fancy = false)
        {
            // Native controller hint text keeps its original geometry and font.
            var hints = button.GetComponentsInChildren<TMP_Text>(true).Where(t => IsHint(t.transform)).ToArray();
            var parents = hints.Select(t => t.transform.parent).ToArray();
            for (int i = 0; i < hints.Length; i++) hints[i].transform.SetParent(transform, true);
            StyleButton(button, parent, x, y, width, fancy, _body, _norse);
            // SetParent to the same parent does not move an existing native button above the inset.
            button.transform.SetAsLastSibling();
            var textColor = button.GetComponent<ButtonTextColor>();
            if (textColor != null)
            {
                textColor.m_defaultColor = textColor.m_defaultMeshColor = Cream;
                textColor.m_disabledColor = Muted;
            }
            for (int i = 0; i < hints.Length; i++) hints[i].transform.SetParent(parents[i], true);
        }

        private TMP_Text Tab(Button button, float x)
        {
            At((RectTransform)button.transform, x, -110f, 148f, 32f, true);
            foreach (var image in button.GetComponentsInChildren<Image>(true)) image.enabled = false;
            // The original Image was also the tab's pointer hit target. Keep an invisible
            // full-size target when removing the native button chrome.
            var hitArea = Box(button.transform, "Auga World Tab Hit Area", Color.clear);
            hitArea.SetAsFirstSibling();
            hitArea.GetComponent<Image>().raycastTarget = true;
            button.transform.SetAsLastSibling();
            var label = button.transform.Find("Text").GetComponent<TMP_Text>();
            var textColor = button.GetComponent<ButtonTextColor>();
            if (textColor != null) textColor.enabled = false;
            Text(label, false, 20f);
            label.fontStyle = FontStyles.UpperCase;
            label.alignment = TextAlignmentOptions.Center;
            button.targetGraphic = label;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            // TabHandler disables the active tab; its caption must remain bright.
            colors.disabledColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color(1f, .83f, .5f);
            colors.pressedColor = Gold;
            button.colors = colors;
            return label;
        }

        private void ToggleStyle(Toggle toggle, float x, float y, float width)
        {
            At((RectTransform)toggle.transform, x, y, width, 26f);
            var background = toggle.transform.Find("Background").GetComponent<Image>();
            background.sprite = null;
            background.color = new Color(.10f, .11f, .08f);
            At(background.rectTransform, -width / 2 + 9f, 13f, 14f, 14f);
            background.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var mark = toggle.graphic as Image;
            mark.sprite = null;
            mark.color = Gold;
            Place(mark.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-3.5f, -3.5f), new Vector2(3.5f, 3.5f));
            var label = toggle.transform.Find("Label").GetComponent<TMP_Text>();
            Text(label, false, 16f);
            label.fontStyle = FontStyles.UpperCase;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 0f), Vector2.zero);
        }

        private void Divider(Transform parent, bool left)
        {
            var line = Box(parent, "Auga World Tab Divider", new Color(Cream.r, Cream.g, Cream.b, .4f));
            Place(line, new Vector2(left ? .07f : .5f, 1f), new Vector2(left ? .5f : .93f, 1f), new Vector2(left ? 0f : 160f, -110.5f), new Vector2(left ? -160f : 0f, -109.5f));
            var diamond = Box(line, "Auga Divider Diamond", Muted);
            Place(diamond, new Vector2(left ? 1f : 0f, .5f), new Vector2(left ? 1f : 0f, .5f), new Vector2(-3f, -3f), new Vector2(3f, 3f));
            diamond.localRotation = Quaternion.Euler(0, 0, 45);
            var fill = Box(diamond, "Fill", PanelColor);
            Place(fill, Vector2.zero, Vector2.one, Vector2.one, -Vector2.one);
        }

        private void Text(TMP_Text label, bool norse, float size)
        {
            label.font = norse ? _norse : _body;
            label.fontSharedMaterial = label.font.material;
            label.fontSize = size;
            label.enableAutoSizing = false;
            label.fontStyle = FontStyles.Normal;
            label.color = Cream;
        }

        private static bool IsHint(Transform t)
        {
            for (; t != null; t = t.parent) if (t.name.StartsWith("gamepad_hint")) return true;
            return false;
        }

        private static void At(RectTransform rect, float x, float y, float width, float height, bool top = false)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, top ? 1f : 0f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }
    }
}
