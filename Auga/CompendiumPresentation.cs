using System;
using System.Linq;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    public static class CompendiumPresentation
    {
        public static AugaCompendiumController Create(Transform parent)
        {
            var staging = new GameObject("Auga Compendium Host", typeof(RectTransform));
            staging.layer = parent.gameObject.layer;
            staging.transform.SetParent(parent, false);
            var host = (RectTransform)staging.transform;
            host.anchorMin = Vector2.zero; host.anchorMax = Vector2.one;
            host.offsetMin = host.offsetMax = Vector2.zero;
            staging.SetActive(false);
            try
            {
                var source = Auga.Assets.MenuPrefab.GetComponentInChildren<AugaCompendiumController>(true);
                var controller = UnityEngine.Object.Instantiate(source, host, false);
                controller.gameObject.SetActive(false);
                var group = controller.GetComponent<UIGroupHandler>();
                var groups = controller.GetComponentsInChildren<UIGroupHandler>(true);
                int priority = Menu.instance.GetComponentsInChildren<UIGroupHandler>(true)
                    .Select(g => g.m_groupPriority).DefaultIfEmpty(10).Max() + 1;
                // The original panel has separate groups for the header and each scroll list.
                // Equal priorities allow all visible sections to receive input together.
                foreach (var section in groups) section.m_groupPriority = priority;
                group.m_defaultElement = controller.TabController.m_tabs[0].m_button.gameObject;
                controller.GetComponent<CanvasGroup>().ignoreParentGroups = true;
                foreach (var scroll in controller.GetComponentsInChildren<ScrollRect>(true))
                    scroll.scrollSensitivity = 120f;
                foreach (var dialog in controller.GetComponentsInChildren<TextsDialog>(true))
                {
                    var labels = dialog.GetComponentsInChildren<Text>(true);
                    dialog.m_textAreaTopic = labels.First(t => t.name == "Name").AsTmp();
                    dialog.m_textArea = labels.First(t => t.name == "Description").AsTmp();
                    // Setup runs under an inactive host, so search inactive scrolls explicitly.
                    var scrolls = dialog.GetComponentsInChildren<ScrollRect>(true);
                    dialog.m_leftScrollRect = scrolls.First(s => s.content == dialog.m_listRoot);
                    dialog.m_leftScrollbar = dialog.m_leftScrollRect.verticalScrollbar;
                    dialog.m_rightScrollbar = scrolls
                        .First(s => s != dialog.m_leftScrollRect).verticalScrollbar;
                    // Adapt a private copy: native FillTextList expects TMP on the named node.
                    var element = UnityEngine.Object.Instantiate(dialog.m_elementPrefab, host, false);
                    element.SetActive(false);
                    var oldName = element.GetComponentsInChildren<Text>(true).First(t => t.name == "name");
                    var name = oldName.AsTmp();
                    name.enabled = true;
                    name.enableWordWrapping = false;
                    name.overflowMode = TextOverflowModes.Overflow;
                    oldName.name = "Legacy Name Layout"; name.name = "name";
                    dialog.m_elementPrefab = element;
                    dialog.gameObject.AddComponent<CompendiumTextLayout>().Dialog = dialog;
                }
                Localization.instance.Localize(controller.transform);
                staging.SetActive(true);
                foreach (var section in groups) section.SetActive(true);
                return controller;
            }
            catch
            {
                UnityEngine.Object.Destroy(staging);
                throw;
            }
        }
    }

    // Preserve the original Hugin/Lore filtering, scoped only to Auga's filtered dialogs.
    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
    public static class CompendiumTextsPatch
    {
        private static bool Prefix(TextsDialog __instance)
        {
            var filter = __instance.GetComponent<AugaTextsDialogFilter>();
            if (filter == null) return true;
            __instance.m_texts.Clear();
            if (Player.m_localPlayer == null) return false;
            foreach (var known in Player.m_localPlayer.GetKnownTexts())
            {
                if (!known.Key.Contains(filter.Filter)) continue;
                string topic = Localization.instance.Localize(known.Key);
                int separator = topic.IndexOf(": ", StringComparison.Ordinal);
                if (separator >= 0) topic = topic.Substring(separator + 2);
                __instance.m_texts.Add(new TextsDialog.TextInfo(topic, Localization.instance.Localize(known.Value)));
            }
            __instance.m_texts.Sort((a, b) => string.Compare(a.m_topic, b.m_topic, StringComparison.CurrentCulture));
            return false;
        }
    }

    public sealed class CompendiumTextLayout : MonoBehaviour
    {
        public TextsDialog Dialog;
        private string _topic, _body;

        private void LateUpdate()
        {
            if (_topic == Dialog.m_textAreaTopic.text && _body == Dialog.m_textArea.text) return;
            _topic = Dialog.m_textAreaTopic.text; _body = Dialog.m_textArea.text;
            foreach (var text in new[] { Dialog.m_textAreaTopic, Dialog.m_textArea })
            {
                var parent = (RectTransform)text.transform.parent;
                var layout = parent.GetComponent<LayoutElement>() ?? parent.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = text.GetPreferredValues(text.text, parent.rect.width, Mathf.Infinity).y;
            }
        }
    }

    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.SnapTo))]
    public static class CompendiumSelectionScrollPatch
    {
        private static bool Prefix(TextsDialog __instance, ScrollRect __0, RectTransform __1, RectTransform __2)
        {
            if (__instance.GetComponent<AugaTextsDialogFilter>() == null) return true;
            var viewport = __0.viewport != null ? __0.viewport : (RectTransform)__0.transform;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, __2);
            var visible = viewport.rect;
            float y = __1.anchoredPosition.y;
            if (bounds.max.y > visible.yMax) y -= bounds.max.y - visible.yMax;
            else if (bounds.min.y < visible.yMin) y += visible.yMin - bounds.min.y;
            __0.StopMovement();
            __1.anchoredPosition = new Vector2(0, Mathf.Clamp(y, 0, Mathf.Max(0, __1.rect.height - visible.height)));
            return false;
        }
    }

    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.FillTextList))]
    public static class CompendiumRowsPatch
    {
        private static void Postfix(TextsDialog __instance)
        {
            if (__instance.GetComponent<AugaTextsDialogFilter>() == null) return;
            foreach (var entry in __instance.m_texts)
            {
                var row = (RectTransform)entry.m_listElement.transform;
                // Native spawning uses world-space instantiation; these are local UI rows.
                row.localScale = Vector3.one;
                row.localRotation = Quaternion.identity;
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, __instance.m_listRoot.rect.width);
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, __instance.m_spacing - 4);
                var label = row.GetComponentInChildren<TMP_Text>(true);
                label.enabled = true;
                label.color = new Color(.918f, .882f, .851f, 1);
                label.canvasRenderer.SetAlpha(1);
                label.text = entry.m_topic;
            }
        }
    }
}
