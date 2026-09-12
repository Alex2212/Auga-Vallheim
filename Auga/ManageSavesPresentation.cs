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
    [HarmonyPatch(typeof(ManageSavesMenu), nameof(ManageSavesMenu.Awake))]
    public static class ManageSavesPresentationPatch
    {
        private static void Postfix(ManageSavesMenu __instance)
        {
            if (__instance.GetComponent<ManageSavesPresentation>() == null)
                __instance.gameObject.AddComponent<ManageSavesPresentation>();
        }
    }

    [HarmonyPatch(typeof(ManageSavesMenuElement), nameof(ManageSavesMenuElement.Start))]
    public static class ManageSaveRowPresentationPatch
    {
        private static void Postfix(ManageSavesMenuElement __instance) => ManageSavesPresentation.StyleRow(__instance.transform);
    }

    [HarmonyPatch(typeof(ManageSavesMenuElement), nameof(ManageSavesMenuElement.CreateBackupElement))]
    public static class ManageBackupRowPresentationPatch
    {
        private static void Postfix(ManageSavesMenuElement.BackupElement __result) => ManageSavesPresentation.StyleRow(__result.GuiInstance.transform);
    }

    // Keep native save operations, backup animation and row geometry intact.
    public sealed class ManageSavesPresentation : MonoBehaviour
    {
        private static readonly Color Cream = new Color(.918f, .882f, .851f);
        private static readonly Color Muted = new Color(.59f, .56f, .49f);
        private static readonly Color Panel = new Color(.22f, .20f, .165f, .98f);
        private readonly List<Image> _hidden = new List<Image>();
        private readonly List<Button> _tabs = new List<Button>();
        private readonly List<(Button button, Image border)> _actions = new List<(Button, Image)>();

        private static bool IsHint(Transform t)
        {
            for (; t != null; t = t.parent) if (t.name.StartsWith("gamepad_hint")) return true;
            return false;
        }

        private static void Text(TMP_Text label, TMP_FontAsset font, float size, bool upper = false)
        {
            label.font = font; label.fontSharedMaterial = font.material;
            label.fontSize = size; label.fontStyle = upper ? FontStyles.UpperCase : FontStyles.Normal;
            label.color = Cream;
            foreach (var shadow in label.GetComponents<Shadow>()) shadow.enabled = false;
        }

        private void Start()
        {
            try
            {
                var gui = GetComponent<ManageSavesMenu>();
                var body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                var norse = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                foreach (var image in GetComponentsInChildren<Image>(true))
                {
                    if (image.sprite != null && image.sprite.name == "panel_separator") { _hidden.Add(image); continue; }
                    if (image.sprite == null || !image.sprite.name.Contains("woodpanel")) continue;
                    _hidden.Add(image);
                    var panel = Box(image.transform, "Auga Saves Panel", Panel);
                    panel.SetAsFirstSibling(); SettingsPresentation.AddCorners(panel);
                }
                foreach (var label in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (IsHint(label.transform)) continue;
                    bool heading = label.GetComponentInParent<Selectable>(true) == null && label.fontSize >= 28;
                    Text(label, heading ? norse : body, heading ? 36 : label.fontSize);
                }
                foreach (var scroll in GetComponentsInChildren<ScrollRect>(true))
                {
                    var rect = (RectTransform)scroll.transform;
                    rect.offsetMax += new Vector2(0, -38);
                    var image = scroll.GetComponent<Image>();
                    if (image != null) { image.sprite = null; image.color = new Color(.12f, .11f, .09f, .65f); }
                }
                foreach (var scrollbar in GetComponentsInChildren<Scrollbar>(true))
                {
                    SettingsPresentation.StyleScrollbar(scrollbar);
                    ((RectTransform)scrollbar.transform).offsetMax += new Vector2(0, -38);
                }
                foreach (var button in new[] { gui.moveButton, gui.removeButton, gui.backButton, gui.actionButton })
                {
                    _hidden.AddRange(button.GetComponentsInChildren<Image>(true).Where(i => !IsHint(i.transform)));
                    var rect = (RectTransform)button.transform;
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 220);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 44);
                    var bg = Box(button.transform, "Auga Save Action", Color.white).GetComponent<Image>();
                    var source = (button == gui.backButton || button == gui.actionButton ? Auga.Assets.ButtonFancy : Auga.Assets.ButtonMedium).GetComponent<Button>().targetGraphic as Image;
                    bg.sprite = source.sprite; bg.type = source.type; bg.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                    _actions.Add((button, bg));
                    var colors = button.colors; colors.normalColor = Color.white;
                    colors.highlightedColor = colors.selectedColor = new Color(1, .83f, .5f);
                    colors.pressedColor = new Color(.75f, .6f, .3f); button.colors = colors;
                    var tint = button.GetComponent<ButtonTextColor>();
                    if (tint != null) { tint.m_defaultColor = tint.m_defaultMeshColor = Cream; tint.m_disabledColor = Muted; }
                    foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (IsHint(label.transform)) continue;
                        bool fancy = button == gui.backButton || button == gui.actionButton;
                        Text(label, fancy ? norse : body, fancy ? 26 : 18, !fancy);
                        label.transform.SetAsLastSibling(); label.alignment = TextAlignmentOptions.Center;
                        Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 0), new Vector2(-20, 0));
                    }
                }
                var actionPosition = gui.actionButton.transform.position;
                actionPosition.y = gui.backButton.transform.position.y;
                gui.actionButton.transform.position = actionPosition;
                var tabs = gui.tabHandler.m_tabs.Where(t => t.m_button != null).ToArray();
                for (int i = 0; i < tabs.Length; i++)
                {
                    var button = tabs[i].m_button; _tabs.Add(button);
                    _hidden.AddRange(button.GetComponentsInChildren<Image>(true));
                    var rect = (RectTransform)button.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
                    rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(160, 32);
                    rect.anchoredPosition = new Vector2((i - (tabs.Length - 1) * .5f) * 164, -104);
                    var hit = Box(button.transform, "Auga Save Tab Hit Area", Color.clear); hit.SetAsFirstSibling();
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    Text(label, body, 20, true); label.alignment = TextAlignmentOptions.Center;
                    button.targetGraphic = label; button.transition = Selectable.Transition.ColorTint;
                    var tint = button.GetComponent<ButtonTextColor>(); if (tint != null) tint.enabled = false;
                    var colors = button.colors; colors.normalColor = colors.disabledColor = Color.white;
                    colors.highlightedColor = colors.selectedColor = new Color(1, .83f, .5f); button.colors = colors;
                }
                if (_tabs.Count > 0)
                {
                    var parent = _tabs[0].transform.parent;
                    for (int side = 0; side < 2; side++)
                    {
                        var line = Box(parent, "Auga Saves Divider", new Color(Cream.r, Cream.g, Cream.b, .4f));
                        Place(line, new Vector2(side == 0 ? .04f : .5f, 1), new Vector2(side == 0 ? .5f : .96f, 1), new Vector2(side == 0 ? 0 : 180, -104.5f), new Vector2(side == 0 ? -180 : 0, -103.5f));
                        line.GetComponent<Image>().raycastTarget = false;
                        var diamond = Box(line, "Diamond", new Color(Cream.r, Cream.g, Cream.b, .4f));
                        diamond.anchorMin = diamond.anchorMax = new Vector2(side == 0 ? 1 : 0, .5f);
                        diamond.sizeDelta = new Vector2(6, 6); diamond.anchoredPosition = Vector2.zero; diamond.localRotation = Quaternion.Euler(0, 0, 45);
                        diamond.GetComponent<Image>().raycastTarget = false;
                        var center = Box(diamond, "Center", Panel); Place(center, Vector2.zero, Vector2.one, Vector2.one, -Vector2.one); center.GetComponent<Image>().raycastTarget = false;
                    }
                }
                Text(gui.storageUsed, body, 18); gui.storageUsed.color = Muted;
                foreach (var image in gui.storageBar.GetComponentsInChildren<Image>(true))
                { image.sprite = null; image.color = new Color(.72f, .56f, .19f); }
                foreach (var image in _hidden) image.enabled = false;
                Debug.Log("[Auga] Manage saves styled with native backup and save controls.");
            }
            catch (Exception e) { Auga.LogError($"Manage saves presentation failed: {e}"); enabled = false; }
        }

        public static void StyleRow(Transform root)
        {
            var body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) Text(label, body, label.fontSize);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.name.IndexOf("selected", StringComparison.OrdinalIgnoreCase) >= 0)
                { image.sprite = null; image.color = new Color(.18f, .34f, .43f); }
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (!(button.targetGraphic is Image bg) || bg.name != "bkg") continue;
                bg.sprite = null; bg.color = Color.white;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.clear;
                colors.highlightedColor = colors.selectedColor = new Color(.18f, .34f, .43f, .45f);
                colors.pressedColor = new Color(.18f, .34f, .43f, .7f);
                button.colors = colors;
            }
        }

        private void LateUpdate()
        {
            foreach (var image in _hidden) if (image != null && image.enabled) image.enabled = false;
            foreach (var action in _actions) action.border.color = action.button.interactable ? Color.white : new Color(.55f, .55f, .55f);
            foreach (var tab in _tabs)
            {
                var label = tab.targetGraphic as TMP_Text;
                if (label != null) label.color = tab.interactable ? Muted : Cream;
            }
        }
    }
}
