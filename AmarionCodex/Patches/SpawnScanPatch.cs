using AmarionCodex.Data;
using HarmonyLib;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// After each zone loads, scans every SpawnPoint in the scene to learn which
    /// NPCs can appear in this zone.  SpawnPoint.CommonSpawns and RareSpawns are
    /// editor-assigned prefab references that are always populated regardless of
    /// whether the NPC has actually spawned, so this captures every possible mob
    /// for the zone — including rare spawns, quest-gated spawns, and night-only
    /// spawns.
    ///
    /// The NPC names are recorded in the shared ZoneSpawnRegistry so that the
    /// bestiary can list NPCs whose KnowledgeEntry has an empty zone.
    /// </summary>
    [HarmonyPatch(typeof(ZoneAnnounce), "Start")]
    internal static class SpawnScanPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            if (GameData.KnowledgeDatabase == null)
                return;

            string zoneName = GameData.SceneName;
            if (string.IsNullOrEmpty(zoneName))
                return;

            // Resolve scene/display name to canonical zone via the same map
            // the rest of the mod uses.
            var canonicalZones = BestiaryDataProvider.GetCanonicalZones(zoneName);
            if (canonicalZones.Count == 0)
                return;

            string zone = canonicalZones[0];

            foreach (var sp in SpawnPointManager.SpawnPointsInScene)
            {
                if (sp == null)
                    continue;

                ScanSpawnList(sp.CommonSpawns, zone);
                ScanSpawnList(sp.RareSpawns, zone);
            }

            ZoneSpawnRegistry.Save();
        }

        private static void ScanSpawnList(
            System.Collections.Generic.List<UnityEngine.GameObject> prefabs,
            string canonicalZone)
        {
            if (prefabs == null)
                return;

            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                    continue;

                var npc = prefab.GetComponent<NPC>();
                if (npc == null || string.IsNullOrEmpty(npc.NPCName))
                    continue;

                string normalized = GameData.KnowledgeDatabase.Normalize(npc.NPCName);
                ZoneSpawnRegistry.Record(normalized, canonicalZone);
            }
        }
    }
}
