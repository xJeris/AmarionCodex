using AmarionCodex.Data;
using HarmonyLib;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Marks an NPC as discovered when it aggros on the player
    /// or any member of the player's group/raid.
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.AggroOn))]
    internal static class AggroPatch
    {
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
                return tar.MyNPC.ThisSim.InGroup || tar.MyNPC.ThisSim.InRaid;

            return false;
        }
    }
}
