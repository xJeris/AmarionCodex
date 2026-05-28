using AmarionCodex.Data;
using HarmonyLib;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Counts kills and discovers NPCs by patching Character.DoDeath().
    /// Uses the same credit check the game uses for XP:
    /// player/group did >50% of the NPC's max HP in damage AND
    /// player was in the NPC's aggro table.
    /// Uses the player's current zone for discovery tracking.
    /// </summary>
    [HarmonyPatch(typeof(Character), "DoDeath")]
    internal static class KillPatch
    {
        private static readonly AccessTools.FieldRef<Character, int> DmgFromPlayerSource =
            AccessTools.FieldRefAccess<Character, int>("DmgFromPlayerSource");

        [HarmonyPostfix]
        static void Postfix(Character __instance)
        {
            if (__instance == null || !__instance.isNPC)
                return;

            var npc = __instance.GetComponent<NPC>();
            if (npc == null || npc.SimPlayer)
                return;

            // Same credit check the game uses for XP
            if (__instance.MyStats == null)
            {
                UnityEngine.Debug.LogWarning("[AmarionCodex] KillPatch: MyStats was null on " + (npc.NPCName ?? "unknown"));
                return;
            }
            int dmgFromPlayer = DmgFromPlayerSource(__instance);
            if (dmgFromPlayer <= __instance.MyStats.CurrentMaxHP / 2)
                return;

            // Check player was in aggro table
            if (npc.AggroTable == null)
            {
                UnityEngine.Debug.LogWarning("[AmarionCodex] KillPatch: AggroTable was null on " + (npc.NPCName ?? "unknown"));
                return;
            }
            bool playerInAggro = false;
            foreach (var slot in npc.AggroTable)
            {
                if (slot != null && slot.Player != null &&
                    slot.Player.transform != null &&
                    slot.Player.transform.name == "Player")
                {
                    playerInAggro = true;
                    break;
                }
            }

            if (!playerInAggro)
                return;

            if (GameData.KnowledgeDatabase == null)
                return;

            if (string.IsNullOrEmpty(npc.NPCName))
            {
                UnityEngine.Debug.LogWarning("[AmarionCodex] KillPatch: NPCName was null/empty");
                return;
            }
            string normalized = GameData.KnowledgeDatabase.Normalize(npc.NPCName);
            string currentZone = GameData.SceneName ?? "";

            EncounterTracker.Discover(normalized, currentZone);
            EncounterTracker.RecordKill(normalized);
        }
    }
}
