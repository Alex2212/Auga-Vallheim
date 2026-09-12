using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    // Keep the bundle's serialized Text fields intact while supplying current game APIs with TMP.
    public static class LegacyText
    {
        private static readonly Dictionary<Font, TMP_FontAsset> Fonts = new Dictionary<Font, TMP_FontAsset>();
        public static Font FallbackFont;

        public static TMP_FontAsset GetFont(Font source)
        {
            source = source != null ? source : FallbackFont;
            if (source == null) throw new System.InvalidOperationException("No font source is available for Auga text.");
            if (!Fonts.TryGetValue(source, out var font) || font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(source);
                if (font == null || font.material == null)
                    throw new System.InvalidOperationException("Cannot convert Auga font: " + source.name);
                Fonts[source] = font;
            }
            return font;
        }

        public static TMP_Text AsTmp(this Text source)
        {
            if (source == null) return null;
            var existing = source.transform.Find("AugaTMP");
            if (existing != null) return existing.GetComponent<TMP_Text>();
            var child = new GameObject("AugaTMP", typeof(RectTransform));
            child.layer = source.gameObject.layer;
            child.transform.SetParent(source.transform, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = child.AddComponent<TextMeshProUGUI>();
            text.font = GetFont(source.font);
            text.fontSharedMaterial = text.font.material;
            text.text = source.text;
            text.fontSize = source.fontSize;
            text.color = source.color;
            text.raycastTarget = source.raycastTarget;
            text.richText = source.supportRichText;
            text.enableWordWrapping = source.horizontalOverflow == HorizontalWrapMode.Wrap;
            text.overflowMode = source.verticalOverflow == VerticalWrapMode.Overflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
            var alignments = new[] { TextAlignmentOptions.TopLeft, TextAlignmentOptions.Top, TextAlignmentOptions.TopRight,
                TextAlignmentOptions.Left, TextAlignmentOptions.Center, TextAlignmentOptions.Right,
                TextAlignmentOptions.BottomLeft, TextAlignmentOptions.Bottom, TextAlignmentOptions.BottomRight };
            text.alignment = alignments[(int)source.alignment];
            text.fontStyle = source.fontStyle == FontStyle.Bold ? FontStyles.Bold : source.fontStyle == FontStyle.Italic ? FontStyles.Italic : source.fontStyle == FontStyle.BoldAndItalic ? FontStyles.Bold | FontStyles.Italic : FontStyles.Normal;
            text.enabled = source.enabled;
            source.enabled = false;
            return text;
        }
    }
}
