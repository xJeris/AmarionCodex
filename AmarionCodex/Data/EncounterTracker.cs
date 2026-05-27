using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace AmarionCodex.Data
{
    internal static class EncounterTracker
    {
        // Discovered entries keyed by "npcName|canonicalZone"
        private static HashSet<string> _discovered = new HashSet<string>();
        // Kill counts keyed by normalized NPC name (global, not per-zone)
        private static Dictionary<string, int> _killCounts = new Dictionary<string, int>();
        private static int _loadedSlot = -1;

        public const int MaxKillCount = 9999;
        private const char KeySeparator = '|';

        /// <summary>
        /// Fired when a new NPC is discovered in a zone. Passes the compound key.
        /// </summary>
        public static event Action<string> OnNewDiscovery;

        /// <summary>
        /// Builds the compound discovery key from NPC name and canonical zone.
        /// </summary>
        private static string MakeKey(string normalizedNpcName, string canonicalZone)
        {
            return normalizedNpcName + KeySeparator + canonicalZone;
        }

        /// <summary>
        /// Check if an NPC is discovered in a specific canonical zone.
        /// </summary>
        public static bool IsDiscovered(string normalizedNpcName, string canonicalZone)
        {
            return _discovered.Contains(MakeKey(normalizedNpcName, canonicalZone));
        }

        /// <summary>
        /// Check if an NPC is discovered in any zone. Used for search results.
        /// </summary>
        public static bool IsDiscoveredAnywhere(string normalizedNpcName)
        {
            string prefix = normalizedNpcName + KeySeparator;
            foreach (var key in _discovered)
            {
                if (key.StartsWith(prefix))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Mark an NPC as discovered using a raw zone name (scene name or loot zone).
        /// Resolves the raw zone to canonical zones and discovers for all matching ones.
        /// Also records the NPC in the shared ZoneSpawnRegistry so that future
        /// characters see it in the zone list even before discovering it themselves.
        /// Returns true if any new discovery was made.
        /// </summary>
        public static bool Discover(string normalizedNpcName, string rawZone)
        {
            if (string.IsNullOrEmpty(normalizedNpcName))
                return false;

            var canonicalZones = BestiaryDataProvider.GetCanonicalZones(rawZone);
            if (canonicalZones.Count == 0)
                return false;

            bool anyNew = false;
            foreach (var zone in canonicalZones)
            {
                // Record in the shared registry so this NPC always appears
                // in this zone's list for all characters
                ZoneSpawnRegistry.Record(normalizedNpcName, zone);

                string key = MakeKey(normalizedNpcName, zone);
                if (_discovered.Add(key))
                {
                    Plugin.Log.LogDebug($"Discovered: {normalizedNpcName} in {zone}");
                    anyNew = true;
                    OnNewDiscovery?.Invoke(key);
                }
            }
            return anyNew;
        }

        /// <summary>
        /// Increment the kill count for an NPC (capped at 9999).
        /// Kill counts are global, not per-zone.
        /// </summary>
        public static void RecordKill(string normalizedNpcName)
        {
            if (string.IsNullOrEmpty(normalizedNpcName))
                return;

            _killCounts.TryGetValue(normalizedNpcName, out int current);
            if (current < MaxKillCount)
                _killCounts[normalizedNpcName] = current + 1;
        }

        /// <summary>
        /// Returns the number of times the player has killed this NPC type.
        /// </summary>
        public static int GetKillCount(string normalizedNpcName)
        {
            if (string.IsNullOrEmpty(normalizedNpcName))
                return 0;

            _killCounts.TryGetValue(normalizedNpcName, out int count);
            return count;
        }

        public static int DiscoveredCount => _discovered.Count;

        public static void Load(int slotIndex)
        {
            _discovered.Clear();
            _killCounts.Clear();
            _loadedSlot = slotIndex;

            string path = GetSavePath(slotIndex);
            if (!File.Exists(path))
            {
                Plugin.Log.LogInfo($"No save file found at {path}, starting fresh.");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<EncounterSaveData>(json);
                if (data?.DiscoveredNPCs != null)
                {
                    foreach (var key in data.DiscoveredNPCs)
                        _discovered.Add(key);

                    // Migrate old name-only keys to compound keys
                    MigrateOldKeys();
                }
                if (data?.KillNames != null && data?.KillValues != null &&
                    data.KillNames.Count == data.KillValues.Count)
                {
                    for (int i = 0; i < data.KillNames.Count; i++)
                        _killCounts[data.KillNames[i]] = data.KillValues[i];
                }
                Plugin.Log.LogInfo($"Loaded {_discovered.Count} discoveries and {_killCounts.Count} kill records for slot {slotIndex}.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to load encounter data: {ex.Message}");
            }
        }

        /// <summary>
        /// Migrates old-format discovery keys (name only, no '|') to compound keys
        /// (name|zone) by looking up all zones each NPC appears in.
        /// Old keys are removed after migration.
        /// </summary>
        private static void MigrateOldKeys()
        {
            if (GameData.KnowledgeDatabase == null || GameData.KnowledgeDatabase.GameKnowledge == null)
                return;

            var oldKeys = new List<string>();
            foreach (var key in _discovered)
            {
                if (key.IndexOf(KeySeparator) < 0)
                    oldKeys.Add(key);
            }

            if (oldKeys.Count == 0)
                return;

            Plugin.Log.LogInfo($"Migrating {oldKeys.Count} old discovery keys to per-zone format...");

            // Build a lookup: normalized NPC name -> set of canonical zones
            var npcZones = new Dictionary<string, HashSet<string>>();
            foreach (var entry in GameData.KnowledgeDatabase.GameKnowledge)
            {
                if (string.IsNullOrEmpty(entry.NPCName) || string.IsNullOrEmpty(entry.NPCZoneName))
                    continue;

                if (!npcZones.TryGetValue(entry.NPCName, out var zones))
                {
                    zones = new HashSet<string>();
                    npcZones[entry.NPCName] = zones;
                }

                foreach (var z in BestiaryDataProvider.GetCanonicalZones(entry.NPCZoneName))
                    zones.Add(z);
            }

            foreach (var oldKey in oldKeys)
            {
                _discovered.Remove(oldKey);
                if (npcZones.TryGetValue(oldKey, out var zones))
                {
                    foreach (var zone in zones)
                        _discovered.Add(MakeKey(oldKey, zone));
                }
            }

            Plugin.Log.LogInfo($"Migration complete. Now {_discovered.Count} discovery keys.");
        }

        public static void Save()
        {
            if (_loadedSlot < 0)
                return;

            Save(_loadedSlot);
        }

        public static void Save(int slotIndex)
        {
            string path = GetSavePath(slotIndex);

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var data = new EncounterSaveData();
                data.DiscoveredNPCs = new List<string>(_discovered);
                data.KillNames = new List<string>(_killCounts.Count);
                data.KillValues = new List<int>(_killCounts.Count);
                foreach (var kvp in _killCounts)
                {
                    data.KillNames.Add(kvp.Key);
                    data.KillValues.Add(kvp.Value);
                }
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to save encounter data: {ex.Message}");
            }
        }

        public static void Clear()
        {
            _discovered.Clear();
            _killCounts.Clear();
            _loadedSlot = -1;
        }

        private static string GetSavePath(int slotIndex)
        {
            // Game saves to: Application.persistentDataPath + "/ESSaveData/"
            string saveDir = Path.Combine(Application.persistentDataPath, "ESSaveData");
            return Path.Combine(saveDir, $"AmarionCodex_{slotIndex}.json");
        }
    }

    [Serializable]
    internal class EncounterSaveData
    {
        /// <summary>
        /// Save format version. Increment when the save format changes.
        /// v1 = name-only discovery keys (pre-zone tracking)
        /// v2 = compound "name|zone" discovery keys
        /// </summary>
        public int SaveVersion = 2;
        public List<string> DiscoveredNPCs = new List<string>();
        // Kill counts stored as parallel lists (JsonUtility doesn't support Dictionary)
        public List<string> KillNames = new List<string>();
        public List<int> KillValues = new List<int>();
    }
}
