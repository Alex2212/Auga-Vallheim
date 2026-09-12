using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class DamageText_Setup
    {
        // Retain the native TMP prefab and Awake initialization. The legacy bundle
        // uses an older text component and cannot replace the current object safely.

        [HarmonyPatch(typeof(DamageText), nameof(DamageText.AddInworldText))]
        [HarmonyPrefix]
        public static void AddInworldText_Prefix(DamageText __instance, out int __state)
        {
            __state = __instance.m_worldTexts.Count;
        }

        [HarmonyPatch(typeof(DamageText), nameof(DamageText.AddInworldText))]
        [HarmonyPostfix]
        public static void AddInworldText_Postfix(DamageText __instance, DamageText.TextType type, string text, bool mySelf, int __state)
        {
            // Native code can skip zero-damage entries when its text limit is reached.
            if (__instance.m_worldTexts.Count <= __state) return;
            var worldTextInstance = __instance.m_worldTexts.LastOrDefault();
            if (worldTextInstance == null)
            {
                return;
            }

            Color color;
            if (type == DamageText.TextType.Heal)
            {
                color = Auga.Colors.Healing;
            }
            else if (mySelf && (type == DamageText.TextType.Normal || type == DamageText.TextType.Resistant ||
                                type == DamageText.TextType.Weak || type == DamageText.TextType.Immune))
            {
                color = text != "0" ? Auga.Colors.PlayerDamage : Auga.Colors.PlayerNoDamage;
            }
            else
            {
                switch (type)
                {
                    case DamageText.TextType.Normal:
                        color = Auga.Colors.NormalDamage;
                        break;
                    case DamageText.TextType.Resistant:
                        color = Auga.Colors.ResistDamage;
                        break;
                    case DamageText.TextType.Weak:
                        color = Auga.Colors.WeakDamage;
                        break;
                    case DamageText.TextType.Immune:
                        color = Auga.Colors.ImmuneDamage;
                        break;
                    case DamageText.TextType.TooHard:
                        color = Auga.Colors.TooHard;
                        break;
                    default:
                        // Preserve native colors for new text types such as blocking messages.
                        return;
                }
            }
            worldTextInstance.m_textField.color = color;
        }
    }
}
