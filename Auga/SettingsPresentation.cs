using System;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    public static class SettingsPresentationPatch
    {
        private static void Postfix(Settings __instance)
        {
            if (__instance.GetComponent<SettingsPresentation>() == null)
                __instance.gameObject.AddComponent<SettingsPresentation>();
        }
    }

    [HarmonyPatch(typeof(Settings), nameof(Settings.ApplyAndClose))]
    public static class SettingsClockSavePatch
    {
        private static void Prefix(Settings __instance) => __instance.GetComponent<SettingsPresentation>()?.SaveClock();
    }

    [HarmonyPatch(typeof(ScrollRect), nameof(ScrollRect.OnScroll))]
    public static class GlobalScrollSpeedPatch
    {
        private static void Prefix(ScrollRect __instance)
        {
            if (Auga.ScrollSpeed != null) __instance.scrollSensitivity = 40f * Auga.ScrollSpeed.Value;
        }
    }

    // Retain the current settings tabs, values, callbacks and save/cancel behavior.
    public sealed class SettingsPresentation : MonoBehaviour
    {
        private static TMP_FontAsset _bodyFont;
        private static TMP_FontAsset _labelFont;
        private static TMP_FontAsset _headingFont;
        private static readonly Color Cream = new Color(0.918f, 0.882f, 0.851f);
        private static readonly Color Gold = new Color(0.72f, 0.56f, 0.19f);
        private static readonly Color Panel = new Color(0.22f, 0.20f, 0.165f, 0.98f);
        private Toggle _showClock;
        private Slider _scrollSpeed;

        public void SaveClock()
        {
            if (_showClock != null) Auga.ShowClock.Value = _showClock.isOn;
            if (_scrollSpeed != null) Auga.ScrollSpeed.Value = Mathf.RoundToInt(_scrollSpeed.value);
        }

        private void AddClockSetting()
        {
            var gameplay = GetComponentInChildren<Valheim.SettingsGui.GameplaySettings>(true);
            if (gameplay == null || gameplay.m_showKeyHints == null) return;
            var source = gameplay.m_showKeyHints;
            var sourceRect = (RectTransform)source.transform;
            var parent = source.transform.parent;
            float step = Mathf.Max(32f, sourceRect.rect.height + 6f);
            bool automatic = parent.GetComponent<LayoutGroup>() != null;
            if (!automatic)
                foreach (RectTransform sibling in parent)
                    if (sibling != sourceRect && sibling.anchoredPosition.y < sourceRect.anchoredPosition.y)
                        sibling.anchoredPosition -= new Vector2(0, step);
            _showClock = Instantiate(source, parent, false);
            _showClock.name = "Auga Show Clock";
            _showClock.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            if (!automatic) ((RectTransform)_showClock.transform).anchoredPosition = sourceRect.anchoredPosition - new Vector2(0, step);
            _showClock.onValueChanged = new Toggle.ToggleEvent();
            _showClock.group = null;
            _showClock.SetIsOnWithoutNotify(Auga.ShowClock.Value);
            foreach (var label in _showClock.GetComponentsInChildren<TMP_Text>(true)) label.text = "SHOW CLOCK";
            foreach (var tooltip in _showClock.GetComponentsInChildren<UITooltip>(true))
            { tooltip.m_topic = "SHOW CLOCK"; tooltip.m_text = "Show the in-game time above the minimap."; }
            var navigation = _showClock.navigation; navigation.mode = Navigation.Mode.Automatic; _showClock.navigation = navigation;
        }

        private void AddScrollSpeedSetting()
        {
            var accessibility = GetComponentInChildren<Valheim.SettingsGui.AccessibilitySettings>(true);
            if (accessibility == null) return;
            var source = accessibility.m_guiScaleSlider;
            var parent = (RectTransform)source.transform.parent;
            Canvas.ForceUpdateCanvases();
            float bottom = float.MaxValue;
            var corners = new Vector3[4];
            foreach (var control in accessibility.GetComponentsInChildren<Selectable>(true))
            {
                ((RectTransform)control.transform).GetWorldCorners(corners);
                bottom = Mathf.Min(bottom, parent.InverseTransformPoint(corners[0]).y);
            }
            var row = new GameObject("Auga Scroll Speed", typeof(RectTransform));
            row.layer = parent.gameObject.layer;
            var rect = (RectTransform)row.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(0, 36);
            rect.anchoredPosition = new Vector2(0, bottom - parent.rect.yMax - 12);
            row.AddComponent<LayoutElement>().preferredHeight = 36;
            _scrollSpeed = Instantiate(source, rect, false);
            _scrollSpeed.name = "Scroll Speed Slider";
            _scrollSpeed.onValueChanged = new Slider.SliderEvent();
            _scrollSpeed.minValue = 1; _scrollSpeed.maxValue = 20; _scrollSpeed.wholeNumbers = true;
            _scrollSpeed.SetValueWithoutNotify(Auga.ScrollSpeed.Value);
            void AlignRow(RectTransform target, RectTransform template)
            {
                target.anchorMin = new Vector2(template.anchorMin.x, .5f);
                target.anchorMax = new Vector2(template.anchorMax.x, .5f);
                target.pivot = new Vector2(template.pivot.x, .5f);
                target.sizeDelta = new Vector2(template.sizeDelta.x, 36);
                target.anchoredPosition = new Vector2(template.anchoredPosition.x, 0);
            }
            AlignRow((RectTransform)_scrollSpeed.transform, (RectTransform)source.transform);
            foreach (var text in _scrollSpeed.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
            var captionTemplate = parent.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => text != accessibility.m_guiScaleText && !text.transform.IsChildOf(rect) && !text.transform.IsChildOf(source.transform))
                .OrderBy(text => Mathf.Abs(text.transform.position.y - source.transform.position.y)).First();
            TMP_Text Label(string name, string caption, TMP_Text template)
            {
                var label = Instantiate(template, rect, false);
                label.name = name; label.text = caption; label.raycastTarget = false;
                AlignRow(label.rectTransform, template.rectTransform);
                return label;
            }
            Label("Scroll Speed Label", "SCROLL SPEED", captionTemplate);
            var value = Label("Scroll Speed Value", Auga.ScrollSpeed.Value + "x", accessibility.m_guiScaleText);
            _scrollSpeed.onValueChanged.AddListener(speed => value.text = Mathf.RoundToInt(speed) + "x");
            foreach (var tooltip in _scrollSpeed.GetComponentsInChildren<UITooltip>(true))
            { tooltip.m_topic = "SCROLL SPEED"; tooltip.m_text = "Mouse-wheel speed for all scroll lists. Applied when settings are saved."; }
        }

        private void Start()
        {
            try
            {
                var settings = GetComponent<Settings>();
                AddClockSetting();
                AddScrollSpeedSetting();
                var root = settings.m_settingsPanel.transform;
                if (_bodyFont == null) _bodyFont = TMP_FontAsset.CreateFontAsset(Auga.Assets.SourceSansProSemiBold);
                if (_labelFont == null) _labelFont = TMP_FontAsset.CreateFontAsset(Auga.Assets.SourceSansProBold);
                if (_headingFont == null)
                {
                    var source = Resources.FindObjectsOfTypeAll<Font>().First(font => font.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase));
                    _headingFont = TMP_FontAsset.CreateFontAsset(source);
                }
                foreach (var label in GetComponentsInChildren<TMP_Text>(true))
                {
                    bool heading = label.fontSize >= 30f && label.GetComponentInParent<Selectable>(true) == null;
                    label.font = heading ? _headingFont : _bodyFont;
                    label.fontSharedMaterial = label.font.material;
                    label.color = Cream;
                    label.fontStyle = FontStyles.Normal;
                    // Keep editable text, control values and button captions in their native case.
                    var control = label.GetComponentInParent<Selectable>(true);
                    bool optionLabel = !heading && (control == null || control is Toggle || control is Slider)
                        && label.name.IndexOf("value", StringComparison.OrdinalIgnoreCase) < 0;
                    if (optionLabel)
                    {
                        label.font = _labelFont;
                        label.fontSharedMaterial = _labelFont.material;
                        label.fontStyle = FontStyles.UpperCase;
                    }
                }

                foreach (var image in GetComponentsInChildren<Image>(true))
                {
                    if (image.GetComponentInParent<Selectable>(true) != null) continue;
                    var rect = image.rectTransform.rect;
                    if (image.sprite != null && (image.sprite.name.Contains("woodpanel") || image.sprite.name == "panel_interior_bkg_128"))
                    {
                        Debug.Log($"[Auga] Settings panel skin: {image.name}, sprite={image.sprite?.name}, rect={rect}");
                        image.enabled = false;
                    }
                }
                var panelBackground = Visual(root, "Auga Settings Background", Panel);
                panelBackground.transform.SetAsFirstSibling();
                panelBackground.raycastTarget = true;

                foreach (var button in GetComponentsInChildren<Button>(true))
                {
                    var image = button.targetGraphic as Image;
                    if (image == null) continue;
                    foreach (var oldImage in button.GetComponentsInChildren<Image>(true)) oldImage.enabled = false;
                    bool tab = button.transform.parent.name.IndexOf("tab", StringComparison.OrdinalIgnoreCase) >= 0;
                    var originalRect = image.rectTransform;
                    image = Visual(originalRect, "Auga Settings Button", tab ? Color.clear : Color.white);
                    image.transform.SetAsFirstSibling();
                    var template = (button == settings.m_backButton || button == settings.m_okButton)
                        ? Auga.Assets.ButtonFancy : Auga.Assets.ButtonMedium;
                    var source = template.GetComponent<Button>().targetGraphic as Image;
                    if (source == null) continue;
                    image.sprite = tab ? null : source.sprite;
                    image.overrideSprite = null;
                    image.type = source.type;
                    // Sliced borders need the prefab's pixel scale; the default doubles the ornamental ends.
                    image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                    image.color = tab ? Color.clear : Color.white;
                    image.raycastTarget = true;
                    image.preserveAspect = true;
                    button.targetGraphic = tab ? (Graphic)button.GetComponentInChildren<TMP_Text>(true) : image;
                    button.transition = Selectable.Transition.ColorTint;
                    var colors = button.colors;
                    colors.normalColor = tab ? Cream : Color.white;
                    colors.highlightedColor = new Color(1f, 0.83f, 0.5f);
                    colors.selectedColor = colors.highlightedColor;
                    colors.pressedColor = new Color(0.75f, 0.6f, 0.3f);
                    if (tab) colors.disabledColor = new Color(1f, 0.83f, 0.5f);
                    button.colors = colors;
                    if (tab)
                    {
                        var selected = button.transform.Find("Selected");
                        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                        {
                            label.font = _labelFont;
                            label.fontSharedMaterial = _labelFont.material;
                            label.fontStyle = FontStyles.UpperCase;
                            label.color = selected != null && label.transform.IsChildOf(selected)
                                ? new Color(1f, 0.83f, 0.5f) : Cream;
                        }
                    }
                    if (button == settings.m_backButton || button == settings.m_okButton)
                        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                        {
                            label.font = _headingFont;
                            label.fontSharedMaterial = _headingFont.material;
                            label.fontSize = 28f;
                        }
                    StyleArrowButton(button, image);
                }

                foreach (var dropdown in GetComponentsInChildren<Selectable>(true).Where(control => control.GetType().Name.Contains("Dropdown")))
                {
                    var native = dropdown.targetGraphic as Image;
                    if (native == null) continue;
                    native.enabled = false;
                    var background = Visual(native.transform, "Auga Dropdown Background", new Color(0.12f, 0.115f, 0.09f));
                    background.transform.SetAsFirstSibling();
                    background.raycastTarget = true;
                    dropdown.targetGraphic = background;
                    dropdown.transition = Selectable.Transition.ColorTint;
                    background.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    background.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    background.rectTransform.sizeDelta = new Vector2(0f, 24f);
                    if (dropdown is TMP_Dropdown list)
                    {
                        list.captionText.fontSize = 16f;
                        list.captionText.enableAutoSizing = false;
                        list.captionText.alignment = TextAlignmentOptions.MidlineLeft;
                        list.captionText.margin = new Vector4(10f, 0f, 20f, 0f);
                        if (list.itemText != null)
                        {
                            list.itemText.fontSize = 16f;
                            list.itemText.alignment = TextAlignmentOptions.MidlineLeft;
                            list.itemText.margin = new Vector4(8f, 0f, 12f, 0f);
                        }
                        if (list.template != null)
                            list.template.anchoredPosition += new Vector2(0f, Mathf.Max(0f, native.rectTransform.rect.height - 24f) * 0.5f);
                        for (int side = 0; side < 2; side++)
                        {
                            var arrow = Visual(native.transform, "Auga Dropdown Arrow", Cream, 4f);
                            arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                            arrow.rectTransform.sizeDelta = new Vector2(4f, 1f);
                            arrow.rectTransform.anchoredPosition = new Vector2(side == 0 ? -11.4f : -8.6f, 0f);
                            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, side == 0 ? -45f : 45f);
                        }
                    }
                    foreach (var popupImage in dropdown.GetComponentsInChildren<Image>(true))
                    {
                        if (popupImage.sprite == null || !popupImage.sprite.name.Contains("woodpanel")) continue;
                        popupImage.enabled = false;
                        var popup = Visual(popupImage.transform, "Auga Dropdown List", new Color(0.12f, 0.115f, 0.09f));
                        popup.transform.SetAsFirstSibling();
                        popup.raycastTarget = true;
                    }
                }

                foreach (var toggle in GetComponentsInChildren<Toggle>(true))
                {
                    var background = toggle.targetGraphic as Image;
                    var check = toggle.graphic as Image;
                    if (background == null || check == null || background == check) continue;
                    background.enabled = check.enabled = false;
                    var diamond = Visual(background.transform, "Auga Toggle Background", new Color(0.10f, 0.105f, 0.075f), 14f);
                    if (background.rectTransform.rect.width > 30f)
                    {
                        diamond.rectTransform.anchorMin = diamond.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                        diamond.rectTransform.anchoredPosition = new Vector2(10f, 0f);
                    }
                    diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                    diamond.raycastTarget = true;
                    var mark = Visual(diamond.transform, "Auga Toggle Check", Gold, 7f);
                    toggle.targetGraphic = diamond;
                    toggle.graphic = mark;
                    mark.canvasRenderer.SetAlpha(toggle.isOn ? 1f : 0f);
                }
                foreach (var slider in GetComponentsInChildren<Slider>(true))
                {
                    if (slider.handleRect != null)
                    {
                        var handle = slider.handleRect.GetComponent<Image>();
                        if (handle != null) handle.enabled = false;
                        var diamond = Visual(slider.handleRect, "Auga Slider Handle", Gold, 8f);
                        diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                        diamond.raycastTarget = true;
                        slider.targetGraphic = diamond;
                        var colors = slider.colors;
                        colors.normalColor = Color.white;
                        colors.highlightedColor = new Color(1f, 0.9f, 0.6f);
                        colors.selectedColor = colors.highlightedColor;
                        slider.colors = colors;
                    }
                    foreach (var image in slider.GetComponentsInChildren<Image>(true))
                    {
                        if (image.transform == slider.handleRect || image.name.StartsWith("Auga ")) continue;
                        image.enabled = false;
                        var line = Visual(image.transform, "Auga Slider Track", Cream);
                        line.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                        line.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                        line.rectTransform.sizeDelta = new Vector2(0f, 1f);
                        line.raycastTarget = true;
                    }
                }
                foreach (var scrollbar in GetComponentsInChildren<Scrollbar>(true))
                    StyleScrollbar(scrollbar);
                StyleReferenceLayout(settings);
                StyleGameplayActions();
                AddCorners(root);
                Debug.Log($"[Auga] Settings presentation applied: {GetComponentsInChildren<Toggle>(true).Length} toggles, {GetComponentsInChildren<Slider>(true).Length} sliders; native settings behavior retained.");
            }
            catch (Exception exception)
            {
                Auga.LogError($"Settings presentation failed: {exception}");
                enabled = false;
            }
        }

        private static void StyleArrowButton(Button button, Image background)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            string caption = label.text.Trim();
            bool previous = caption == "<" || caption == "◀" || caption == "←" || caption == "&lt;";
            bool next = caption == ">" || caption == "▶" || caption == "→" || caption == "&gt;";
            if (!previous && !next) return;
            background.sprite = null;
            background.color = Color.clear;
            var border = Visual(background.transform, "Auga Arrow Diamond", Gold, 28f);
            border.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var fill = Visual(border.transform, "Auga Arrow Fill", new Color(0.16f, 0.15f, 0.12f), 26f);
            button.targetGraphic = border;
            label.text = previous ? "<" : ">";
            label.font = _bodyFont;
            label.fontSharedMaterial = _bodyFont.material;
            label.fontStyle = FontStyles.Normal;
            label.fontSize = 22f;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            // Open the angle of the ASCII chevron without widening its horizontal span.
            label.rectTransform.localScale = new Vector3(1f, 1.5f, 1f);
        }

        private void StyleGameplayActions()
        {
            var gameplay = GetComponentInChildren<Valheim.SettingsGui.GameplaySettings>(true);
            if (gameplay == null || gameplay.m_deleteAccount == null || gameplay.m_radialSettingsButton == null) return;
            var buttons = new[] { gameplay.m_deleteAccount, gameplay.m_radialSettingsButton };
            float width = 240f;
            foreach (var button in buttons)
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null) width = Mathf.Max(width, Mathf.Ceil(label.GetPreferredValues(label.text).x) + 48f);
            }
            var first = (RectTransform)buttons[0].transform;
            var second = (RectTransform)buttons[1].transform;
            // Preserve the group's center and vertical placement, with equal widths and a 20-unit gap.
            var center = (first.TransformPoint(first.rect.center) + second.TransformPoint(second.rect.center)) * 0.5f;
            for (int i = 0; i < buttons.Length; i++)
            {
                var rect = (RectTransform)buttons[i].transform;
                var layout = buttons[i].GetComponent<LayoutElement>();
                if (layout != null) layout.preferredWidth = width;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                var target = rect.parent.InverseTransformPoint(center);
                var position = rect.localPosition;
                position.x = target.x + (i == 0 ? -1f : 1f) * (width + 20f) * 0.5f - rect.rect.center.x;
                rect.localPosition = position;
            }
        }

        private static void StyleReferenceLayout(Settings settings)
        {
            var tabs = settings.m_tabHandler.m_tabs.Where(tab => tab.m_button != null && tab.m_button.gameObject.activeSelf).ToArray();
            if (tabs.Length == 0) return;
            Canvas.ForceUpdateCanvases();
            var row = tabs[0].m_button.transform.parent;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            var widths = tabs.Select(tab =>
            {
                var label = tab.m_button.GetComponentInChildren<TMP_Text>(true);
                return Mathf.Ceil(label.GetPreferredValues(label.text).x) + 24f;
            }).ToArray();
            float totalWidth = widths.Sum();
            float x = -totalWidth * 0.5f;
            for (int i = 0; i < tabs.Length; i++)
            {
                var rect = (RectTransform)tabs[i].m_button.transform;
                rect.anchorMin = new Vector2(0.5f, rect.anchorMin.y);
                rect.anchorMax = new Vector2(0.5f, rect.anchorMax.y);
                rect.pivot = new Vector2(0.5f, rect.pivot.y);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, widths[i]);
                rect.anchoredPosition = new Vector2(x + widths[i] * 0.5f, rect.anchoredPosition.y);
                x += widths[i];
            }
            foreach (var image in settings.GetComponentsInChildren<Image>(true))
                if (!image.name.StartsWith("Auga ") && image.GetComponentInParent<Selectable>(true) == null &&
                    image.rectTransform.rect.width > 500f && image.rectTransform.rect.height < 12f)
                    image.enabled = false;

            var left = Visual(row, "Auga Tab Divider Left", new Color(Cream.r, Cream.g, Cream.b, 0.4f));
            left.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            left.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            left.rectTransform.offsetMin = new Vector2(0f, -0.5f);
            left.rectTransform.offsetMax = new Vector2(-totalWidth * 0.5f - 10f, 0.5f);
            var right = Visual(row, "Auga Tab Divider Right", left.color);
            right.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            right.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            right.rectTransform.offsetMin = new Vector2(totalWidth * 0.5f + 10f, -0.5f);
            right.rectTransform.offsetMax = new Vector2(0f, 0.5f);
            Diamond(left.transform, new Vector2(1f, 0.5f));
            Diamond(right.transform, new Vector2(0f, 0.5f));

            var footer = new[] { settings.m_backButton, settings.m_okButton };
            for (int i = 0; i < footer.Length; i++)
            {
                var rect = (RectTransform)footer[i].transform;
                rect.anchorMin = new Vector2(0.5f, rect.anchorMin.y);
                rect.anchorMax = new Vector2(0.5f, rect.anchorMax.y);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160f);
                rect.anchoredPosition = new Vector2(i == 0 ? -90f : 90f, rect.anchoredPosition.y);
            }
            var back = (RectTransform)settings.m_backButton.transform;
            var divider = Visual(back.parent, "Auga Footer Divider", left.color);
            divider.rectTransform.anchorMin = new Vector2(0.04f, back.anchorMax.y);
            divider.rectTransform.anchorMax = new Vector2(0.96f, back.anchorMax.y);
            divider.rectTransform.sizeDelta = new Vector2(0f, 1f);
            divider.rectTransform.anchoredPosition = new Vector2(0f, back.anchoredPosition.y + back.rect.height * (1f - back.pivot.y) + 16f);
            Diamond(divider.transform, new Vector2(0.5f, 0.5f));
        }

        private static void Diamond(Transform parent, Vector2 anchor)
        {
            var color = new Color(Cream.r, Cream.g, Cream.b, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                var edge = Visual(parent, "Auga Divider Diamond", color);
                edge.rectTransform.anchorMin = edge.rectTransform.anchorMax = anchor;
                edge.rectTransform.sizeDelta = new Vector2(4f, 1f);
                float angle = 45f + i * 90f;
                edge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                edge.rectTransform.anchoredPosition = new Vector2(Mathf.Cos((angle + 90f) * Mathf.Deg2Rad), Mathf.Sin((angle + 90f) * Mathf.Deg2Rad)) * 2f;
            }
        }

        internal static void StyleScrollbar(Scrollbar scrollbar)
        {
            if (scrollbar.handleRect == null) return;
            bool vertical = scrollbar.direction == Scrollbar.Direction.BottomToTop ||
                            scrollbar.direction == Scrollbar.Direction.TopToBottom;
            foreach (var native in scrollbar.GetComponentsInChildren<Image>(true))
                native.enabled = false;

            // Keep native handle travel, thumb length and the full drag/click area.
            var hitArea = Visual(scrollbar.transform, "Auga Scrollbar Hit Area", Color.clear);
            hitArea.raycastTarget = true;
            hitArea.transform.SetAsFirstSibling();
            var track = Visual(scrollbar.transform, "Auga Scrollbar Track", new Color(0.10f, 0.095f, 0.075f));
            track.transform.SetSiblingIndex(1);
            var thumb = Visual(scrollbar.handleRect, "Auga Scrollbar Thumb", Color.white);
            foreach (var graphic in new[] { track, thumb })
            {
                var rect = graphic.rectTransform;
                rect.anchorMin = vertical ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
                rect.anchorMax = vertical ? new Vector2(0.5f, 1f) : new Vector2(1f, 0.5f);
                float thickness = graphic == track ? 2f : 5f;
                rect.sizeDelta = vertical ? new Vector2(thickness, 0f) : new Vector2(0f, thickness);
            }
            scrollbar.targetGraphic = thumb;
            scrollbar.transition = Selectable.Transition.ColorTint;
            var colors = scrollbar.colors;
            colors.normalColor = new Color(0.50f, 0.47f, 0.40f);
            colors.highlightedColor = new Color(0.65f, 0.61f, 0.53f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.74f, 0.70f, 0.61f);
            colors.disabledColor = new Color(0.35f, 0.33f, 0.29f);
            colors.colorMultiplier = 1f;
            scrollbar.colors = colors;
        }

        internal static void AddCorners(Transform root)
        {
            var source = Auga.Assets.PanelBase.GetComponentsInChildren<Image>(true).FirstOrDefault(image => image.sprite != null && image.sprite.name == "CornerDecoration");
            if (source == null) return;
            for (int i = 0; i < 4; i++)
            {
                var obj = new GameObject("Auga Settings Corner", typeof(RectTransform), typeof(Image));
                obj.layer = root.gameObject.layer;
                obj.transform.SetParent(root, false);
                obj.AddComponent<LayoutElement>().ignoreLayout = true;
                var rect = (RectTransform)obj.transform;
                float x = i % 2, y = i / 2;
                rect.anchorMin = rect.anchorMax = new Vector2(x, y);
                rect.pivot = new Vector2(x, y);
                rect.anchoredPosition = new Vector2(x == 0 ? 10 : -10, y == 0 ? 10 : -10);
                rect.sizeDelta = new Vector2(40, 40);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(x == 0 ? 30 : -30, y == 0 ? 30 : -30);
                rect.localScale = new Vector3(x == 0 ? 1 : -1, y == 1 ? 1 : -1, 1);
                var image = obj.GetComponent<Image>();
                image.sprite = source.sprite;
                image.color = new Color(Cream.r, Cream.g, Cream.b, 0.4f);
                image.raycastTarget = false;
            }
        }

        private static Image Visual(Transform parent, string name, Color color, float size = 0f)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            obj.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = size > 0 ? new Vector2(0.5f, 0.5f) : Vector2.zero;
            rect.anchorMax = size > 0 ? new Vector2(0.5f, 0.5f) : Vector2.one;
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
