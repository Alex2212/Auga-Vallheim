using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class Chat_Setup
    {
        [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
        public static class Chat_Awake_Patch
        {
            public static bool Prefix(Chat __instance)
            {
                if (!Auga.AugaChatShow.Value || Auga.HasChatter)
                    return true;
                
                // The bundled input component is missing in current Valheim. Replacing Chat
                // leaves m_input null and breaks every caller of Chat.HasFocus, including movement.
                Debug.Log("[Auga] Retaining native chat input for Valheim 1.0.");
                return true;
            }

            public static void Postfix(Chat __instance)
            {
                if (!Auga.AugaChatShow.Value || Auga.HasChatter)
                    return;
                
                // Native chat owns its layout and focus lifecycle until its prefab is migrated.
            }
        }

        [HarmonyPatch(typeof(Chat), nameof(Chat.SetNpcText))]
        public static class Chat_SetNpcText_Patch
        {
            public static void Postfix(Chat __instance)
            {
                if (!Auga.AugaChatShow.Value || Auga.HasChatter)
                    return;
                
                var latestChatMessage = __instance.m_npcTexts.LastOrDefault();
                if (latestChatMessage != null)
                {
                    var text = latestChatMessage.m_textField.text;
                    text = text.Replace("<color=orange>", $"<color={Auga.Colors.Topic}>");
                    text = text.Replace("<color=yellow>", $"<color={Auga.Colors.Emphasis}>");
                    latestChatMessage.m_textField.text = text;
                }
            }
        }
    }
}
