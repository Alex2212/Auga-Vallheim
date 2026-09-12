using System;
using System.Linq;
using AugaUnity;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static Auga.CharacterSelectionPresentation;

namespace Auga
{
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
    public static class MinimapPresentationPatch
    {
        private static void Postfix(Minimap __instance)
        {
            try
            {
                var root = (RectTransform)__instance.m_smallRoot.transform;
                root.anchorMin = root.anchorMax = Vector2.one; root.pivot = Vector2.one;
                const float mapSize = 264; // 10% larger than the previous 240-unit map.
                float centerX = -40 - mapSize * .5f;
                root.anchoredPosition = new Vector2(-40, -40); root.sizeDelta = new Vector2(mapSize, mapSize);
                var reference = Auga.Assets.Hud.transform.Find("hudroot/MiniMap/small");
                var border = Box(root, "Auga Minimap Border", new Color(.22f, .20f, .165f, .95f));
                Place(border, Vector2.zero, Vector2.one, new Vector2(-6, -6), new Vector2(6, 6));
                var background = reference.Find("MapBG").GetComponent<Image>();
                border.GetComponent<Image>().sprite = background.sprite;
                border.GetComponent<Image>().type = background.type;
                border.GetComponent<Image>().color = background.color;
                border.SetAsFirstSibling(); border.GetComponent<Image>().raycastTarget = false;
                var trim = UnityEngine.Object.Instantiate(reference.Find("MapBorder").gameObject, root, false);
                trim.name = "Auga Map Trim";
                Place((RectTransform)trim.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                foreach (var graphic in trim.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                var label = __instance.m_biomeNameSmall;
                label.transform.SetParent(root.parent, false);
                var font = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                label.font = font; label.fontSharedMaterial = font.material; label.fontSize = 24;
                label.color = new Color(.918f, .882f, .851f); label.alignment = TMPro.TextAlignmentOptions.Center;
                var clockObject = new GameObject("Auga Minimap Clock", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI), typeof(MinimapClock));
                clockObject.layer = root.gameObject.layer;
                clockObject.transform.SetParent(root.parent, false);
                var clockText = clockObject.GetComponent<TMPro.TextMeshProUGUI>();
                clockText.font = font; clockText.fontSharedMaterial = font.material;
                clockText.fontSize = 22; clockText.color = label.color;
                clockText.alignment = TMPro.TextAlignmentOptions.Center; clockText.raycastTarget = false;
                clockText.rectTransform.sizeDelta = new Vector2(mapSize, 26);
                var clock = clockObject.GetComponent<MinimapClock>();
                clock.SmallMap = root;
                var clockCaption = UnityEngine.Object.Instantiate(reference.Find("biome").gameObject, clockObject.transform, false);
                clockCaption.name = "Auga Clock Ornaments";
                var clockCaptionRect = (RectTransform)clockCaption.transform;
                clockCaptionRect.anchorMin = clockCaptionRect.anchorMax = new Vector2(.5f, .5f);
                clockCaptionRect.pivot = new Vector2(.5f, .5f);
                clockCaptionRect.anchoredPosition = Vector2.zero;
                clock.Decoration = clockCaption;
                foreach (var graphic in clockCaption.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(centerX, -40 - mapSize - 4); rect.sizeDelta = new Vector2(mapSize, 32);
                // Keep the relocated label's visibility tied to the small map, including inventory/map transitions.
                var visibility = label.gameObject.AddComponent<MinimapLabelVisibility>(); visibility.SmallMap = root.gameObject;
                var caption = UnityEngine.Object.Instantiate(reference.Find("biome").gameObject, root.parent, false);
                caption.name = "Auga Biome Caption";
                var captionRect = (RectTransform)caption.transform;
                captionRect.anchorMin = captionRect.anchorMax = Vector2.one;
                captionRect.pivot = new Vector2(.5f, 1);
                captionRect.anchoredPosition = new Vector2(centerX, -40 - mapSize - 8);
                visibility.Caption = caption;
                foreach (var graphic in caption.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                __instance.m_windMarker.gameObject.SetActive(false);
                var wind = (RectTransform)UnityEngine.Object.Instantiate(reference.Find("WindIndicator").gameObject, root, false).transform;
                wind.name = "Auga Wind Indicator";
                var windSlot = (RectTransform)trim.transform.Find("WindCorner");
                wind.SetParent(windSlot, false);
                wind.anchorMin = wind.anchorMax = new Vector2(.5f, .5f);
                wind.pivot = new Vector2(.5f, .5f);
                wind.anchoredPosition = Vector2.zero;
                __instance.m_windMarker = wind;
                foreach (var graphic in wind.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                var effects = Hud.instance.transform.Find("hudroot/StatusEffects");
                var controller = effects != null ? effects.GetComponentInChildren<AugaStatusEffects>(true) : null;
                if (controller != null)
                {
                    var container = (RectTransform)controller.EffectsContainer.transform;
                    var effectsRect = (RectTransform)effects;
                    effectsRect.anchorMin = effectsRect.anchorMax = Vector2.one;
                    effectsRect.pivot = Vector2.one;
                    effectsRect.anchoredPosition = new Vector2(-40, -40 - mapSize - 80);
                    // MovableHudElement reapplies the saved position every frame.
                    // Add the minimap clearance to its baseline, preserving user adjustments.
                    var movable = effects.GetComponent<MovableHudElement>();
                    if (movable != null)
                        movable.LayoutOffset = effectsRect.anchoredPosition - new Vector2(-40, -330);
                    var layout = container.GetComponent<VerticalLayoutGroup>() ?? container.gameObject.AddComponent<VerticalLayoutGroup>();
                    layout.childAlignment = TextAnchor.UpperRight; layout.spacing = 8;
                    layout.childControlWidth = layout.childControlHeight = false;
                    layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                    var fitter = container.GetComponent<ContentSizeFitter>() ?? container.gameObject.AddComponent<ContentSizeFitter>();
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                Debug.Log("[Auga] Native minimap styled with biome caption below; map textures and pins retained.");
            }
            catch (Exception e) { Auga.LogError($"Minimap presentation failed: {e}"); }
        }
    }
    public sealed class MinimapClock : MonoBehaviour
    {
        public RectTransform SmallMap;
        public GameObject Decoration;
        private TMPro.TMP_Text _text;
        private int _minute = -1;
        private void Awake() => _text = GetComponent<TMPro.TMP_Text>();
        private void LateUpdate()
        {
            bool visible = Auga.ShowClock.Value && SmallMap != null && SmallMap.gameObject.activeInHierarchy && EnvMan.instance != null;
            _text.enabled = visible && Decoration == null;
            if (Decoration != null) Decoration.SetActive(visible);
            if (!visible) return;
            // Center in the gap above the map; follow its actual transform and visibility.
            _text.rectTransform.position = SmallMap.TransformPoint(new Vector3(SmallMap.rect.center.x, SmallMap.rect.yMax + 23, 0));
            int minute = Mathf.FloorToInt(Mathf.Repeat(EnvMan.instance.GetDayFraction(), 1f) * 1440f);
            if (minute == _minute) return;
            _minute = minute;
            _text.text = (minute / 60).ToString("00") + ":" + (minute % 60).ToString("00");
            if (Decoration != null)
            {
                foreach (var text in Decoration.GetComponentsInChildren<Text>(true)) text.text = _text.text;
                foreach (var text in Decoration.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.text = _text.text;
            }
        }
    }

    public sealed class MinimapLabelVisibility : MonoBehaviour
    {
        public GameObject SmallMap;
        public GameObject Caption;
        private TMPro.TMP_Text _label;
        private void Awake() => _label = GetComponent<TMPro.TMP_Text>();
        private void LateUpdate()
        {
            if (SmallMap == null) return;
            if (Caption == null) { _label.enabled = SmallMap.activeInHierarchy; return; }
            _label.enabled = false;
            Caption.SetActive(SmallMap.activeInHierarchy);
            foreach (var text in Caption.GetComponentsInChildren<Text>(true)) text.text = _label.text;
            foreach (var text in Caption.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.text = _label.text;
        }
    }
}
