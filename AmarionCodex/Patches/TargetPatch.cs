using AmarionCodex.Data;
using HarmonyLib;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Marks an NPC as discovered when the player targets it (click or Tab).
    /// Uses the player's current zone for discovery tracking.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.TargetMe))]
    internal static class TargetPatch
    {
        [HarmonyPostfix]
        static void Postfix(Character __instance)
        {
            if (__instance == null || !__instance.isNPC)
                return;

            var npc = __instance.GetComponent<NPC>();
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
