using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class MessageHud_Setup
    {
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        [HarmonyPostfix]
        public static void MessageHud_Awake_Postfix(MessageHud __instance)
        {
            if (__instance == null)
                return;

            var centerText = __instance.m_messageCenterText;
            var font = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
            centerText.font = font;
            centerText.fontSharedMaterial = font.material;
            centerText.fontStyle = TMPro.FontStyles.UpperCase;

            // Retain the current notification queue, TMP fields, and object lifetime.
            // The old replacement destroys MessageHud during Awake and substitutes
            // an older serialized MessageHud plus its own pickup-message controller.
            if (__instance.GetComponent<AugaMessageLog>() == null)
                __instance.gameObject.AddComponent<AugaMessageLog>();
            var canvas = __instance.m_messageText.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Auga] Notification canvas unavailable; retaining native notification visuals.");
                return;
            }
            var source = Auga.Assets.MessageHud.GetComponentInChildren<AugaTopLeftMessageController>(true);
            var container = new GameObject("Auga Pickup Notifications", typeof(RectTransform), typeof(CanvasGroup));
            container.layer = __instance.gameObject.layer;
            var rect = (RectTransform)container.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(55, -115);
            rect.sizeDelta = new Vector2(1000, 40);
            var group = container.GetComponent<CanvasGroup>();
            group.interactable = false; group.blocksRaycasts = false;
            var controller = __instance.gameObject.AddComponent<AugaTopLeftMessageController>();
            controller.LogContainer = rect; controller.LogPrefab = source.LogPrefab;
            var presentation = __instance.gameObject.AddComponent<AugaNotificationPresentation>();
            presentation.Hud = __instance; presentation.Group = group;
            Debug.Log($"[Auga] Auga stacked notifications attached to canvas {canvas.name}; native MessageHud retained.");
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowMessage))]
        [HarmonyPostfix]
        public static void ShowMessage(MessageHud __instance, MessageHud.MessageType type, string text, int amount, Sprite icon, bool showDespiteHiddenHUD)
        {
            if (type != MessageHud.MessageType.TopLeft || (Hud.IsUserHidden() && !showDespiteHiddenHUD)) return;
            var controller = __instance.GetComponent<AugaTopLeftMessageController>();
            if (controller != null) controller.AddMessage(Localization.instance.Localize(text), icon, amount);
        }
    }

    public sealed class AugaNotificationPresentation : MonoBehaviour
    {
        public MessageHud Hud;
        public CanvasGroup Group;
        private void LateUpdate()
        {
            // Keep the native queue/log processing, but render only the Auga stack.
            Hud.m_messageText.enabled = false;
            Hud.m_messageIcon.enabled = false;
            Group.alpha = global::Hud.IsUserHidden() && !Hud.m_showDespiteHiddenHUD ? 0 : 1;
        }
        private void OnDestroy()
        {
            if (Group != null) Destroy(Group.gameObject);
        }
    }
}
