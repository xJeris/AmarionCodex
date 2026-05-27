using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AmarionCodex.Data
{
    /// <summary>
    /// Shared (cross-character) registry of which NPCs can spawn in which zones.
    /// Learned at runtime when the player encounters an NPC in a zone that the
    /// knowledge database doesn't list it under. Persisted to a single JSON file
    /// so that once any character discovers an NPC in a zone, all characters see
    /// it in that zone's list (as ??? until they discover it themselves).
    /// </summary>
    internal static class ZoneSpawnRegistry
    {
        // Maps canonical zone name -> set of normalized NPC names seen there
        private static Dictionary<string, HashSet<string>> _zoneNpcs
            = new Dictionary<string, HashSet<string>>();

        private static bool _dirty;

        /// <summary>
        /// Records that an NPC can spawn in a canonical zone. Only stores entries
        /// not already covered by the knowledge database so the file stays small.
        /// Returns true if this is a new mapping.
        /// </summary>
        public static bool Record(string normalizedNpcName, string canonicalZone)
        {
            if (string.IsNullOrEmpty(normalizedNpcName) || string.IsNullOrEmpty(canonicalZone))
                return false;

            if (!_zoneNpcs.TryGetValue(canonicalZone, out var names))
            {
                names = new HashSet<string>();
                _zoneNpcs[canonicalZone] = names;
            }

            if (names.Add(normalizedNpcName))
            {
                _dirty = true;
                Plugin.Log.LogDebug($"ZoneSpawnRegistry: learned {normalizedNpcName} spawns in {canonicalZone}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns all NPC names registered for a zone (only extras not in the
        /// knowledge DB). Returns null if none.
        /// </summary>
        public static HashSet<string> GetNpcsForZone(string canonicalZone)
        {
            if (_zoneNpcs.TryGetValue(canonicalZone, out var names))
                return names;
            return null;
        }

        /// <summary>
        /// Returns all zone names that have at least one registered NPC.
        /// </summary>
        public static IEnumerable<string> GetAllZones()
        {
            return _zoneNpcs.Keys;
        }

        public static void Load()
        {
            _zoneNpcs.Clear();
            _dirty = false;

            string path = GetSavePath();
            if (!File.Exists(path))
                return;

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<ZoneSpawnSaveData>(json);
                if (data?.ZoneNames != null && data?.NpcLists != null &&
                    data.ZoneNames.Count == data.NpcLists.Count)
                {
                    for (int i = 0; i < data.ZoneNames.Count; i++)
                    {
                        var names = new HashSet<string>();
                        if (data.NpcLists[i]?.Names != null)
                        {
                            foreach (var n in data.NpcLists[i].Names)
                                names.Add(n);
                        }
                        if (names.Count > 0)
                            _zoneNpcs[data.ZoneNames[i]] = names;
                    }
                    Plugin.Log.LogInfo($"ZoneSpawnRegistry: loaded {_zoneNpcs.Count} zone mappings.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to load zone spawn registry: {ex.Message}");
            }
        }

        public static void Save()
        {
            if (!_dirty)
                return;

            string path = GetSavePath();

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var data = new ZoneSpawnSaveData();
                data.ZoneNames = new List<string>(_zoneNpcs.Count);
                data.NpcLists = new List<NpcNameList>(_zoneNpcs.Count);
                foreach (var kvp in _zoneNpcs)
                {
                    data.ZoneNames.Add(kvp.Key);
                    var list = new NpcNameList();
                    list.Names = new List<string>(kvp.Value);
                    data.NpcLists.Add(list);
                }
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                _dirty = false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to save zone spawn registry: {ex.Message}");
            }
        }

        public static void Clear()
        {
            _zoneNpcs.Clear();
            _dirty = true;
        }

        private static string GetSavePath()
        {
            string saveDir = Path.Combine(Application.persistentDataPath, "ESSaveData");
            return Path.Combine(saveDir, "AmarionCodex_ZoneSpawns.json");
        }
    }

    [Serializable]
    internal class NpcNameList
    {
        public List<string> Names = new List<string>();
    }

    [Serializable]
    internal class ZoneSpawnSaveData
    {
        public List<string> ZoneNames = new List<string>();
        public List<NpcNameList> NpcLists = new List<NpcNameList>();
    }
}
