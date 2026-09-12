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
    [HarmonyPatch(typeof(UnifiedPopup), nameof(UnifiedPopup.Show))]
    public static class PopupPresentationPatch
    {
        private static void Postfix(UnifiedPopup __instance)
        {
            var view = __instance.GetComponent<PopupPresentation>();
            if (view == null) view = __instance.gameObject.AddComponent<PopupPresentation>();
            view.Apply(__instance);
        }
    }
    public sealed class PopupPresentation : MonoBehaviour
    {
        private Image[] _native;
        public void Apply(UnifiedPopup popup)
        {
            try
            {
                if (_native != null) return;
                var body = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
                var norse = LegacyText.GetFont(Resources.FindObjectsOfTypeAll<Font>().First(f => f.name.StartsWith("Norsebold", StringComparison.OrdinalIgnoreCase)));
                var root = popup.popupUIParent.transform;
                var images = root.GetComponentsInChildren<Image>(true).Where(i => i.sprite != null && i.sprite.name.Contains("woodpanel")).ToList();
                foreach (var image in images)
                {
                    var panel = Box(image.transform, "Auga Popup Panel", new Color(.22f, .20f, .165f, .98f));
                    panel.SetAsFirstSibling();
                    SettingsPresentation.AddCorners(panel);
                }
                popup.headerText.font = norse; popup.headerText.fontSharedMaterial = norse.material;
                popup.headerText.fontSize = 36; popup.headerText.color = new Color(.918f, .882f, .851f);
                popup.bodyText.font = body; popup.bodyText.fontSharedMaterial = body.material;
                popup.bodyText.fontSize = 21; popup.bodyText.color = new Color(.918f, .882f, .851f);
                foreach (var button in new[] { popup.buttonLeft, popup.buttonCenter, popup.buttonRight, popup.buttonConfirm })
                {
                    if (button == null) continue;
                    images.AddRange(button.GetComponentsInChildren<Image>(true));
                    var rect = (RectTransform)button.transform;
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 44);
                    var bg = Box(button.transform, "Auga Popup Button", Color.white).GetComponent<Image>();
                    var source = Auga.Assets.ButtonFancy.GetComponent<Button>().targetGraphic as Image;
                    bg.sprite = source.sprite; bg.type = source.type; bg.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                    StyleButtonStates(button, bg);
                    foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (label.transform.parent.name.StartsWith("gamepad_hint")) continue;
                        label.font = norse; label.fontSharedMaterial = norse.material; label.fontSize = 26;
                        label.color = new Color(.918f, .882f, .851f);
                        label.transform.SetAsLastSibling();
                    }
                    var tint = button.GetComponent<ButtonTextColor>();
                    if (tint != null) tint.m_defaultColor = tint.m_defaultMeshColor = new Color(.918f, .882f, .851f);
                }
                _native = images.Distinct().ToArray();
                foreach (var image in _native) image.enabled = false;
                Debug.Log("[Auga] Native confirmation/warning popup styled.");
            }
            catch (Exception exception) { Auga.LogError($"Popup presentation failed: {exception}"); enabled = false; }
        }
        private void LateUpdate() { if (_native != null) foreach (var image in _native) if (image != null && image.enabled) image.enabled = false; }
    }
}
