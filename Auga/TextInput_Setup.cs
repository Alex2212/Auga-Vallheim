using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using AugaUnity;
using static Auga.CharacterSelectionPresentation;

namespace Auga
{
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.Awake))]
    public static class TextInputPresentationPatch
    {
        private static void Postfix(TextInput __instance)
        {
            var panel = (RectTransform)__instance.m_panel.transform;
            panel.sizeDelta = new Vector2(450, 145);
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
                if (image.GetComponentInParent<Selectable>() == null && image.rectTransform.rect.width > 100)
                    image.enabled = false;
            var background = Box(panel, "Auga Tag Background", new Color(.22f, .20f, .165f, .98f));
            background.SetAsFirstSibling();
            var title = __instance.m_topic;
            title.font = LegacyText.GetFont(Auga.Assets.SourceSansProBold);
            title.fontSharedMaterial = title.font.material; title.fontSize = 26;
            title.color = new Color(.918f, .882f, .851f);
            title.alignment = TMPro.TextAlignmentOptions.Center;
            Place(title.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(24, -62), new Vector2(-24, -12));
            var input = __instance.m_inputField;
            Place((RectTransform)input.transform, Vector2.zero, Vector2.one, new Vector2(28, 22), new Vector2(-28, -76));
            foreach (var image in input.GetComponentsInChildren<Image>(true)) image.enabled = false;
            var field = Box(input.transform, "Auga Tag Input", Color.white).GetComponent<Image>();
            field.transform.SetAsFirstSibling();
            var source = Auga.Assets.MainMenuPrefab.transform.Find("StartGame/Panel/WorldPanel/ServerPassword").GetComponent<Image>();
            field.sprite = source.sprite; field.type = source.type; field.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            input.targetGraphic = field;
            if (input.textViewport != null) Place(input.textViewport, Vector2.zero, Vector2.one, new Vector2(24, 3), new Vector2(-24, -3));
            input.textComponent.font = LegacyText.GetFont(Auga.Assets.SourceSansProSemiBold);
            input.textComponent.fontSharedMaterial = input.textComponent.font.material;
            input.textComponent.fontSize = 20; input.textComponent.color = title.color;
            var divider = Auga.Assets.InventoryScreen.transform.Find("root/Player/StandardDivider");
            for (int side = 0; side < 2; side++)
            {
                var line = UnityEngine.Object.Instantiate(divider, panel, false);
                Place((RectTransform)line, new Vector2(side == 0 ? 0 : .5f, 1), new Vector2(side == 0 ? .5f : 1, 1),
                    new Vector2(side == 0 ? 26 : 52, -43), new Vector2(side == 0 ? -52 : -26, -31));
                foreach (var graphic in line.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            }
        }
    }

    [HarmonyPatch]
    public static class TextInput_Setup
    {
        [HarmonyPatch(typeof(TextInput), nameof(TextInput.Awake))]
        public static class TextInput_Awake_Patch
        {
            public static bool Prefix(TextInput __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.TextInput, "TextInput");
            }
        }
    }
}
