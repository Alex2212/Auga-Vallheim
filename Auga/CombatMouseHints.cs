using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.Start))]
    public static class CombatMouseHintsPatch
    {
        private static void Postfix(KeyHints __instance)
        {
            foreach (var root in new[] { __instance.m_combatHints, __instance.m_inventoryHints, __instance.m_inventoryWithContainerHints, __instance.m_buildHints }.Distinct())
                if (root != null && root.GetComponent<CombatMouseHints>() == null)
                    root.AddComponent<CombatMouseHints>();
            foreach (var root in new[] { __instance.m_combatHints, __instance.m_inventoryHints, __instance.m_inventoryWithContainerHints, __instance.m_buildHints }.Distinct())
                if (root != null && root.GetComponent<CombatHintColumn>() == null)
                    root.AddComponent<CombatHintColumn>();
        }
    }

    public sealed class CombatHintColumn : MonoBehaviour
    {
        private readonly List<TMP_Text> _captions = new List<TMP_Text>();
        private readonly Dictionary<TMP_Text, LayoutElement> _keySpacers = new Dictionary<TMP_Text, LayoutElement>();
        private RectTransform _column;

        private void Start()
        {
            var hints = GetComponent<UIInputHint>();
            var column = hints != null && hints.m_mouseKeyboardHint != null
                ? (RectTransform)hints.m_mouseKeyboardHint.transform : (RectTransform)transform;
            _column = column;
            var oldLayout = column.GetComponent<LayoutGroup>();
            if (oldLayout != null && !(oldLayout is VerticalLayoutGroup))
                DestroyImmediate(oldLayout);
            var layout = column.GetComponent<VerticalLayoutGroup>() ?? column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.spacing = 6;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            column.anchorMin = column.anchorMax = new Vector2(1, 0);
            column.pivot = new Vector2(1, 0);
            column.anchoredPosition = Vector2.zero;
            // Reserve enough height for inventory/container actions as well as combat.
            column.sizeDelta = new Vector2(280, Mathf.Max(150, column.childCount * 36 - 6));
            var actionFont = AugaUnity.LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>()
                .First(font => font.name.StartsWith("Norsebold", System.StringComparison.OrdinalIgnoreCase)));
            foreach (Transform row in column)
            {
                var size = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
                size.minHeight = size.preferredHeight = 30;
                // Combo rows also contain a direct TMP '+' separator before the action.
                // It belongs with the keys, never in the action column.
                var caption = row.Cast<Transform>().Select(child => child.GetComponent<TMP_Text>())
                    .LastOrDefault(text => text != null && !string.IsNullOrWhiteSpace(text.text) && text.text.Trim() != "+");
                if (caption == null) continue;

                // Keep native binding/combo objects intact, followed by a shared-width action column.
                caption.transform.SetAsLastSibling();
                caption.alignment = TextAlignmentOptions.MidlineLeft;
                caption.font = actionFont;
                caption.fontSharedMaterial = actionFont.material;
                caption.fontStyle = FontStyles.UpperCase;
                _captions.Add(caption);
                var oldRowLayout = row.GetComponent<LayoutGroup>();
                if (oldRowLayout != null && !(oldRowLayout is HorizontalLayoutGroup))
                    DestroyImmediate(oldRowLayout);
                var horizontal = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
                horizontal.childAlignment = TextAnchor.MiddleLeft;
                horizontal.padding = new RectOffset();
                horizontal.spacing = 8;
                horizontal.childControlWidth = horizontal.childControlHeight = true;
                horizontal.childForceExpandWidth = horizontal.childForceExpandHeight = false;
                var spacer = new GameObject("Auga Key Column Spacer", typeof(RectTransform), typeof(LayoutElement));
                spacer.layer = row.gameObject.layer;
                spacer.transform.SetParent(row, false);
                spacer.transform.SetSiblingIndex(caption.transform.GetSiblingIndex());
                _keySpacers.Add(caption, spacer.GetComponent<LayoutElement>());
            }
        }

        private void LateUpdate()
        {
            if (_column == null || _captions.Count == 0) return;
            // Pad shorter combinations so keys and captions each share a left edge.
            float width = Mathf.Ceil(_captions.Max(caption => caption.GetPreferredValues(caption.text).x));
            var keyWidths = _captions.ToDictionary(caption => caption, caption =>
            {
                var keys = caption.transform.parent.Cast<Transform>()
                    .Where(child => child != caption.transform && child != _keySpacers[caption].transform && child.gameObject.activeSelf)
                    .Select(child => (RectTransform)child).ToArray();
                return keys.Sum(key => Mathf.Max(LayoutUtility.GetMinWidth(key), LayoutUtility.GetPreferredWidth(key)))
                    + Mathf.Max(0, keys.Length - 1) * 8;
            });
            float keyWidth = keyWidths.Values.Max();
            foreach (var caption in _captions)
            {
                var spacer = _keySpacers[caption];
                spacer.minWidth = spacer.preferredWidth = keyWidth - keyWidths[caption];
                spacer.flexibleWidth = 0;
                var size = caption.GetComponent<LayoutElement>() ?? caption.gameObject.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = width;
                size.flexibleWidth = 0;
            }
            _column.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(280, width + keyWidth + 16));
        }
    }

    public sealed class CombatMouseHints : MonoBehaviour
    {
        private sealed class Hint
        {
            public TMP_Text Text;
            public Image Icon;
            public Image[] Backgrounds;
            public bool[] BackgroundEnabled;
            public Color Color;
            public bool Replaced;
            public RectTransform Keycap;
            public Vector2 OriginalSize;
            public LayoutElement TextLayout, KeyLayout;
            public Vector3 TextWidths, KeyWidths;
        }

        private readonly List<Hint> _hints = new List<Hint>();
        private readonly Sprite[] _mouse = new Sprite[3];

        private void Start()
        {
            var images = Auga.Assets.Hud.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < _mouse.Length; i++)
                _mouse[i] = images.First(image => image.name == "Mouse" + (i + 1) && image.sprite != null).sprite;
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                // Limit replacement to a single keycap, never a whole action/combo row.
                Transform keycap = text.transform;
                var parent = text.transform.parent;
                if (parent != null && parent != transform && parent.GetComponentsInChildren<TMP_Text>(true).Length == 1)
                    keycap = parent;
                var backgrounds = keycap.GetComponentsInChildren<Image>(true);
                var iconObject = new GameObject("Auga Mouse Button", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.layer = text.gameObject.layer;
                iconObject.transform.SetParent(text.transform, false);
                iconObject.GetComponent<LayoutElement>().ignoreLayout = true;
                var icon = iconObject.GetComponent<Image>();
                icon.raycastTarget = false; icon.preserveAspect = true; icon.enabled = false;
                var outline = iconObject.AddComponent<Outline>();
                outline.effectColor = new Color(.918f, .882f, .851f, .9f);
                outline.effectDistance = new Vector2(1, -1);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(.5f, .5f);
                icon.rectTransform.sizeDelta = new Vector2(22.5f, 27.5f);
                _hints.Add(new Hint { Text = text, Icon = icon, Backgrounds = backgrounds,
                    BackgroundEnabled = backgrounds.Select(image => image.enabled).ToArray(), Color = text.color,
                    Keycap = (RectTransform)keycap, OriginalSize = ((RectTransform)keycap).sizeDelta });
            }
        }

        private void LateUpdate()
        {
            foreach (var hint in _hints)
            {
                string value = hint.Text.text.Trim();
                int button = value == "Mouse-1" ? 0 : value == "Mouse-2" ? 1 : value == "Mouse-3" ? 2 : -1;
                if (button >= 0)
                {
                    if (!hint.Replaced)
                    {
                        hint.TextLayout = hint.Text.GetComponent<LayoutElement>() ?? hint.Text.gameObject.AddComponent<LayoutElement>();
                        hint.TextWidths = Widths(hint.TextLayout);
                        if (hint.Keycap != hint.Text.rectTransform)
                        {
                            hint.KeyLayout = hint.Keycap.GetComponent<LayoutElement>() ?? hint.Keycap.gameObject.AddComponent<LayoutElement>();
                            hint.KeyWidths = Widths(hint.KeyLayout);
                        }
                    }
                    SetWidths(hint.TextLayout, new Vector3(26, 26, 0));
                    if (hint.KeyLayout != null) SetWidths(hint.KeyLayout, new Vector3(30, 30, 0));
                    hint.Keycap.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 30);
                    hint.Icon.sprite = _mouse[button]; hint.Icon.enabled = true;
                    hint.Text.color = new Color(hint.Color.r, hint.Color.g, hint.Color.b, 0);
                    foreach (var background in hint.Backgrounds) background.enabled = false;
                    hint.Replaced = true;
                }
                else if (hint.Replaced)
                {
                    SetWidths(hint.TextLayout, hint.TextWidths);
                    if (hint.KeyLayout != null) SetWidths(hint.KeyLayout, hint.KeyWidths);
                    hint.Keycap.sizeDelta = hint.OriginalSize;
                    hint.Icon.enabled = false; hint.Text.color = hint.Color;
                    for (int i = 0; i < hint.Backgrounds.Length; i++) hint.Backgrounds[i].enabled = hint.BackgroundEnabled[i];
                    hint.Replaced = false;
                }
            }
        }

        private static Vector3 Widths(LayoutElement layout) => new Vector3(layout.minWidth, layout.preferredWidth, layout.flexibleWidth);
        private static void SetWidths(LayoutElement layout, Vector3 widths)
        {
            layout.minWidth = widths.x; layout.preferredWidth = widths.y; layout.flexibleWidth = widths.z;
        }
    }
}
