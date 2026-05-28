using AmarionCodex.Data;
using HarmonyLib;
using UnityEngine;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Marks an NPC as discovered when the player hails (talks to) it.
    /// </summary>
    [HarmonyPatch(typeof(NPCDialogManager), nameof(NPCDialogManager.GenericHail))]
    internal static class HailPatch
    {
        [HarmonyPostfix]
        static void Postfix(NPCDialogManager __instance)
        {
            if (__instance == null)
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
