using AmarionCodex.Data;
using HarmonyLib;
using System.Reflection;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Marks an NPC as discovered when it aggros on the player
    /// or any member of the player's group/raid.
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.AggroOn))]
    internal static class AggroPatch
    {
        // InRaid only exists in playtest builds, not retail — resolve via reflection once
        private static readonly FieldInfo InRaidField =
            typeof(SimPlayer).GetField("InRaid", BindingFlags.Public | BindingFlags.Instance);

        [HarmonyPostfix]
        static void Postfix(NPC __instance, Character tar)
        {
            if (__instance == null || tar == null)
                return;

            // Don't discover sim players as NPCs
            if (__instance.SimPlayer)
                return;

            // Check if the aggro target is the player or a grouped/raided sim player
            if (!IsPlayerOrAlly(tar))
                return;

            if (GameData.KnowledgeDatabase == null)
                return;

            if (string.IsNullOrEmpty(__instance.NPCName))
            {
                UnityEngine.Debug.LogWarning("[AmarionCodex] AggroPatch: NPCName was null/empty");
                return;
            }
            string normalized = GameData.KnowledgeDatabase.Normalize(__instance.NPCName);
            string currentZone = GameData.SceneName ?? "";
            EncounterTracker.Discover(normalized, currentZone);
        }

        private static bool IsPlayerOrAlly(Character tar)
        {
            // Actual player
            if (tar.transform != null && tar.transform.name == "Player")
                return true;

            // Grouped or raided sim player
            if (tar.isNPC && tar.MyNPC != null && tar.MyNPC.SimPlayer && tar.MyNPC.ThisSim != null)
            {
                if (tar.MyNPC.ThisSim.InGroup)
                    return true;

                // InRaid only exists in playtest builds — skip on retail
                if (InRaidField != null)
                    return (bool)InRaidField.GetValue(tar.MyNPC.ThisSim);
            }

            return false;
        }
    }
}
