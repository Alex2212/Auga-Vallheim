using AugaUnity;
using HarmonyLib;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.UpdateTextElements))]
    public static class UITooltip_UpdateTextElements_Patch
    {
        public static bool Prefix(UITooltip __instance)
        {
            if (UITooltip.m_tooltip != null)
            {
                var customTooltip = UITooltip.m_tooltip.GetComponent<ComplexTooltip>();
                if (customTooltip != null)
                {
                    var itemTooltip = __instance.GetComponent<ItemTooltip>();
                    if (itemTooltip != null && itemTooltip.Item != null)
                    {
                        customTooltip.SetItem(itemTooltip.Item);
                        return false;
                    }

                    var foodTooltip = __instance.GetComponent<FoodTooltip>();
                    if (foodTooltip != null && foodTooltip.Food != null)
                    {
                        customTooltip.SetFood(foodTooltip.Food);
                        return false;
                    }

                    var statusTooltip = __instance.GetComponent<StatusTooltip>();
                    if (statusTooltip != null && statusTooltip.StatusEffect != null)
                    {
                        customTooltip.SetStatusEffect(statusTooltip.StatusEffect);
                        return false;
                    }

                    var skillTooltip = __instance.GetComponent<SkillTooltip>();
                    if (skillTooltip != null && skillTooltip.Skill != null)
                    {
                        customTooltip.SetSkill(skillTooltip.Skill);
                        return false;
                    }
                }
            }

            // Old Auga tooltip prefabs use UI.Text; the current native method
            // dereferences TMP_Text on these same named objects.
            if (UITooltip.m_tooltip != null)
            {
                bool legacy = false;
                foreach (var text in UITooltip.m_tooltip.GetComponentsInChildren<Text>(true))
                {
                    if (text.name == "Topic") { text.text = Localization.instance.Localize(__instance.m_topic); legacy = true; }
                    if (text.name == "Text") { text.text = Localization.instance.Localize(__instance.m_text); legacy = true; }
                }
                if (legacy) return false;
            }
            return true;
        }
    }

}
