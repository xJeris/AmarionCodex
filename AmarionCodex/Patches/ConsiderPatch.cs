using AmarionCodex.Data;
using HarmonyLib;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Marks an NPC as discovered when the player uses the Consider action.
    /// Uses the player's current zone for discovery tracking.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControl), "ConsiderOpponent")]
    internal static class ConsiderPatch
    {
        [HarmonyPostfix]
        static void Postfix(Character newTar)
        {
            if (newTar == null || !newTar.isNPC)
                return;

            var npc = newTar.GetComponent<NPC>();
            if (npc == null || npc.SimPlayer)
                return;

            if (GameData.KnowledgeDatabase == null)
                return;

            string normalized = GameData.KnowledgeDatabase.Normalize(npc.NPCName);
            string currentZone = GameData.SceneName ?? "";
            EncounterTracker.Discover(normalized, currentZone);
        }
    }
}
