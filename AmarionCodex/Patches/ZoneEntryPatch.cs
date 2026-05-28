using AmarionCodex.Data;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Handles special discovery logic when the player enters a zone.
    /// ZoneAnnounce.Start() fires on every zone transition.
    /// </summary>
    [HarmonyPatch(typeof(ZoneAnnounce), "Start")]
    internal static class ZoneEntryPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            // Auto-discover Reliquary Hall NPCs on zone entry.
            // The Reliquary Ward and Fiend are one-time-per-account kills that
            // despawn permanently, so grant discovery credit just for zoning in.
            string scene = SceneManager.GetActiveScene().name;
            string zone = BestiaryDatabase.ResolveSceneToZone(scene);
            if (zone == "Reliquary Hall")
            {
                EncounterTracker.Discover("reliquary ward", scene);
                EncounterTracker.Discover("reliquary fiend", scene);
            }
        }
    }
}
