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
    [HarmonyPatch(typeof(ServerOptionsGUI), nameof(ServerOptionsGUI.Awake))]
    public static class WorldModifiersPresentationPatch
    {
        private static void Postfix(ServerOptionsGUI __instance)
        {
            if (__instance.GetComponent<WorldModifiersPresentation>() == null)
                __instance.gameObject.AddComponent<WorldModifiersPresentation>();
        }
    }

    // Only presentation changes: preset keys, modifier values and Done/Cancel remain native.
    public sealed class WorldModifiersPresentation : MonoBehaviour
    {
        private static readonly Color Cream = new Color(.918f, .882f, .851f);
        private static readonly Color Muted = new Color(.59f, .56f, .49f);
        private static readonly Color Gold = new Color(.72f, .56f, .19f);
        private static readonly Color Panel = new Color(.22f, .20f, .165f, .98f);

        private void Start()
        {
            try
            {
                var gui = GetComponent<ServerOptionsGUI>();
                var body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                var norse = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                foreach (var label in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (IsHint(label.transform)) continue;
                    bool tooltip = label == gui.m_toolTipText;
                    bool heading = !tooltip && label.GetComponentInParent<Selectable>(true) == null && label.fontSize >= 24f;
                    label.font = heading ? norse : body;
                    label.fontSharedMaterial = label.font.material;
                    label.color = Cream;
                    label.fontStyle = heading || tooltip ? FontStyles.Normal : FontStyles.UpperCase;
                    if (label.name.IndexOf("value", StringComparison.OrdinalIgnoreCase) >= 0) label.fontStyle = FontStyles.Normal;
                    if (tooltip) { label.fontSize = 20f; label.fontStyle = FontStyles.Normal; label.margin = new Vector4(12f, 12f, 12f, 12f); }
                    if (label.text.IndexOf("presets", StringComparison.OrdinalIgnoreCase) >= 0)
                        AddPresetDividers(label);
                }
                foreach (var native in GetComponentsInChildren<Image>(true))
                {
                    if (native.GetComponentInParent<Selectable>(true) != null) continue;
                    if (native.sprite == null || !(native.sprite.name.Contains("woodpanel") || native.sprite.name == "panel_interior_bkg_128")) continue;
                    native.enabled = false;
                    var panel = Box(native.transform, "Auga Modifiers Panel", Panel);
                    panel.SetAsFirstSibling();
                    SettingsPresentation.AddCorners(panel);
                }
                if (gui.m_toolTipPanel != null)
                {
                    foreach (var image in gui.m_toolTipPanel.GetComponents<Image>()) image.enabled = false;
                    var tooltip = Box(gui.m_toolTipPanel, "Auga Modifier Description", new Color(.12f, .11f, .09f, .95f));
                    tooltip.SetAsFirstSibling();
                    SettingsPresentation.AddCorners(tooltip);
                }
                foreach (var button in GetComponentsInChildren<Button>(true))
                {
                    bool footer = button.gameObject == gui.m_doneButton || button.name.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0;
                    foreach (var image in button.GetComponentsInChildren<Image>(true)) if (!IsHint(image.transform)) image.enabled = false;
                    var bg = Box(button.transform, "Auga Modifier Button", Color.white);
                    bg.SetAsFirstSibling();
                    var imageTarget = bg.GetComponent<Image>();
                    var source = (footer ? Auga.Assets.ButtonFancy : Auga.Assets.ButtonMedium).GetComponent<Button>().targetGraphic as Image;
                    imageTarget.sprite = source.sprite;
                    imageTarget.type = source.type;
                    imageTarget.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                    // Native animation can re-enable its original background (notably Done).
                    foreach (var nativeImage in button.GetComponentsInChildren<Image>(true))
                    {
                        if (nativeImage == imageTarget || IsHint(nativeImage.transform)) continue;
                        nativeImage.sprite = source.sprite;
                        nativeImage.type = source.type;
                        nativeImage.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                    }
                    button.targetGraphic = imageTarget;
                    button.transition = Selectable.Transition.ColorTint;
                    var colors = button.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = colors.selectedColor = new Color(1f, .83f, .5f);
                    colors.pressedColor = new Color(.75f, .6f, .3f);
                    button.colors = colors;
                    // Native SpriteSwap must not restore the old Done texture on hover/selection.
                    var sprites = button.spriteState;
                    sprites.highlightedSprite = sprites.pressedSprite = sprites.selectedSprite = sprites.disabledSprite = source.sprite;
                    button.spriteState = sprites;
                    var tint = button.GetComponent<ButtonTextColor>();
                    if (tint != null) { tint.m_defaultColor = tint.m_defaultMeshColor = Cream; tint.m_disabledColor = Muted; tint.m_sprite = source.sprite; }
                    foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (IsHint(label.transform)) continue;
                        label.font = footer ? norse : body;
                        label.fontSharedMaterial = label.font.material;
                        label.fontSize = footer ? 28f : 18f;
                        label.fontStyle = footer ? FontStyles.Normal : FontStyles.UpperCase;
                        label.enableAutoSizing = false;
                        label.alignment = TextAlignmentOptions.Center;
                        Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 0), new Vector2(-18f, 0));
                    }
                }
                // Done uses the game's GUI button implementation, which may not derive from UI.Button.
                var doneRoot = gui.m_doneButton.transform;
                foreach (var nativeImage in doneRoot.GetComponentsInChildren<Image>(true)) nativeImage.enabled = false;
                var doneBackground = Box(doneRoot, "Auga Done Background", Color.white).GetComponent<Image>();
                var doneSource = Auga.Assets.ButtonFancy.GetComponent<Button>().targetGraphic as Image;
                doneBackground.sprite = doneSource.sprite;
                doneBackground.type = doneSource.type;
                doneBackground.pixelsPerUnitMultiplier = doneSource.pixelsPerUnitMultiplier;
                foreach (var label in doneRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (IsHint(label.transform)) continue;
                    label.transform.SetAsLastSibling();
                    label.font = norse;
                    label.fontSharedMaterial = norse.material;
                    label.fontSize = 28f;
                    label.color = Cream;
                }
                foreach (var toggle in GetComponentsInChildren<Toggle>(true))
                {
                    var background = toggle.targetGraphic as Image;
                    var check = toggle.graphic as Image;
                    if (background == null || check == null || background == check) continue;
                    background.enabled = check.enabled = false;
                    var diamond = Center(background.transform, "Auga Modifier Toggle", new Color(.10f, .105f, .075f), 14f);
                    diamond.localRotation = Quaternion.Euler(0, 0, 45);
                    var mark = Center(diamond, "Auga Modifier Check", Gold, 7f).GetComponent<Image>();
                    toggle.targetGraphic = diamond.GetComponent<Image>();
                    toggle.graphic = mark;
                    mark.canvasRenderer.SetAlpha(toggle.isOn ? 1f : 0f);
                }
                foreach (var slider in GetComponentsInChildren<Slider>(true))
                {
                    foreach (var image in slider.GetComponentsInChildren<Image>(true))
                    {
                        image.enabled = false;
                        if (image.transform == slider.handleRect) continue;
                        var line = Box(image.transform, "Auga Modifier Track", Cream);
                        Place(line, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, -.5f), new Vector2(0, .5f));
                    }
                    if (slider.handleRect == null) continue;
                    var handle = Center(slider.handleRect, "Auga Modifier Handle", Gold, 8f);
                    handle.localRotation = Quaternion.Euler(0, 0, 45);
                    slider.targetGraphic = handle.GetComponent<Image>();
                    var colors = slider.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = colors.selectedColor = new Color(1f, .9f, .6f);
                    colors.pressedColor = Color.white;
                    slider.colors = colors;
                    // Value captions retain the native casing, such as Normal and Hard.
                    foreach (var label in slider.GetComponentsInChildren<TMP_Text>(true))
                        if (label.name.IndexOf("value", StringComparison.OrdinalIgnoreCase) >= 0) label.fontStyle = FontStyles.Normal;
                }
                Debug.Log("[Auga] World modifiers presentation applied; native presets, values and save/cancel retained.");
            }
            catch (Exception exception)
            {
                Auga.LogError($"World modifiers presentation failed: {exception}");
                enabled = false;
            }
        }

        private static RectTransform Center(Transform parent, string name, Color color, float size)
        {
            var rect = Box(parent, name, color);
            Place(rect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.one * (-size / 2), Vector2.one * (size / 2));
            return rect;
        }

        private static void AddPresetDividers(TMP_Text label)
        {
            float width = Mathf.Max(480f, ((RectTransform)label.transform.parent).rect.width - 48f);
            var row = Box(label.transform.parent, "Auga Presets Dividers", Color.clear);
            row.GetComponent<Image>().raycastTarget = false;
            row.anchorMin = row.anchorMax = new Vector2(.5f, .5f);
            row.sizeDelta = new Vector2(width, 1f);
            row.position = label.rectTransform.TransformPoint(label.rectTransform.rect.center);
            for (int side = 0; side < 2; side++)
            {
                var line = Box(row, "Auga Presets Line", new Color(Cream.r, Cream.g, Cream.b, .4f));
                Place(line, new Vector2(side == 0 ? 0f : .5f, 0f), new Vector2(side == 0 ? .5f : 1f, 1f),
                    new Vector2(side == 0 ? 0f : 65f, 0f), new Vector2(side == 0 ? -65f : 0f, 0f));
                line.GetComponent<Image>().raycastTarget = false;
                var diamond = Center(line, "Auga Presets Diamond", Muted, 6f);
                diamond.anchorMin = diamond.anchorMax = new Vector2(side == 0 ? 1f : 0f, .5f);
                diamond.anchoredPosition = Vector2.zero;
                diamond.localRotation = Quaternion.Euler(0, 0, 45);
                Center(diamond, "Auga Presets Diamond Fill", Panel, 4f);
            }
        }

        private static bool IsHint(Transform t)
        {
            for (; t != null; t = t.parent) if (t.name.StartsWith("gamepad_hint")) return true;
            return false;
        }
    }
}
