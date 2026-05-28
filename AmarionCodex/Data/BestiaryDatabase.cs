using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AmarionCodex.Data
{
    /// <summary>
    /// Loads and indexes the static bestiary_data.json file.
    /// Provides all NPC/zone/loot queries without runtime game-data access.
    /// </summary>
    internal static class BestiaryDatabase
    {
        private static BestiaryData _data;
        private static Dictionary<string, ZoneData> _zoneIndex;
        private static Dictionary<string, string> _displayNameIndex;
        private static Dictionary<string, string> _sceneToZone;
        private static List<string> _sortedZoneNames;
        private static bool _loaded;

        private const string EmbeddedResourceName = "AmarionCodex.bestiary_data.json";

        /// <summary>
        /// Load bestiary data from the JSON file in the plugin directory.
        /// Falls back to the copy embedded in the DLL if the external file is missing or corrupt.
        /// Call once during Plugin.Awake().
        /// </summary>
        public static void Load(string pluginDir)
        {
            _loaded = false;
            _data = null;
            _zoneIndex = new Dictionary<string, ZoneData>();
            _displayNameIndex = new Dictionary<string, string>();
            _sceneToZone = new Dictionary<string, string>();
            _sortedZoneNames = new List<string>();

            string json = null;

            // Try external file first (allows power users to swap data)
            string path = Path.Combine(pluginDir, "bestiary_data.json");
            if (File.Exists(path))
            {
                try
                {
                    json = File.ReadAllText(path);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AmarionCodex] Could not read external bestiary_data.json: {ex.Message}");
                }
            }

            // Fall back to embedded resource
            if (string.IsNullOrEmpty(json))
            {
                json = LoadEmbeddedJson();
                if (!string.IsNullOrEmpty(json))
                    Debug.Log("[AmarionCodex] Using embedded bestiary data (external file missing or unreadable)");
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[AmarionCodex] No bestiary data available — codex will be empty");
                return;
            }

            try
            {
                _data = ParseBestiaryJson(json);
                if (_data == null || _data.zones == null || _data.zones.Count == 0)
                {
                    Debug.LogWarning("[AmarionCodex] Failed to parse bestiary data or no zones found");
                    return;
                }

                BuildIndexes();
                _loaded = true;
                Debug.Log($"[AmarionCodex] Loaded bestiary: {_data.zones.Count} zones, {_displayNameIndex.Count} NPCs");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AmarionCodex] Error parsing bestiary data: {ex.Message}");
            }
        }

        private static string LoadEmbeddedJson()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
                {
                    if (stream == null)
                        return null;
                    using (var reader = new StreamReader(stream))
                        return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AmarionCodex] Failed to load embedded bestiary data: {ex.Message}");
                return null;
            }
        }

        // ── MiniJson-based parsing ──

        private static BestiaryData ParseBestiaryJson(string json)
        {
            var root = MiniJson.DeserializeObject(json);
            if (root == null) return null;

            var data = new BestiaryData();
            data.version = GetInt(root, "version");

            // Parse zones
            if (root.TryGetValue("zones", out object zonesObj) && zonesObj is List<object> zonesList)
            {
                foreach (var zoneObj in zonesList)
                {
                    if (!(zoneObj is Dictionary<string, object> zoneDict)) continue;
                    var zone = new ZoneData();
                    zone.name = GetString(zoneDict, "name");
                    zone.levelRange = GetString(zoneDict, "levelRange");
                    zone.isDungeon = GetBool(zoneDict, "isDungeon");

                    if (zoneDict.TryGetValue("npcs", out object npcsObj) && npcsObj is List<object> npcsList)
                    {
                        foreach (var npcObj in npcsList)
                        {
                            if (!(npcObj is Dictionary<string, object> npcDict)) continue;
                            var npc = new NpcData();
                            npc.name = GetString(npcDict, "name");
                            npc.normalizedName = GetString(npcDict, "normalizedName");
                            npc.minLevel = GetInt(npcDict, "minLevel");
                            npc.maxLevel = GetInt(npcDict, "maxLevel");
                            npc.isBoss = GetBool(npcDict, "isBoss");
                            npc.loot = GetStringList(npcDict, "loot");
                            npc.questsGiven = GetStringList(npcDict, "questsGiven");
                            npc.questsTurnIn = GetStringList(npcDict, "questsTurnIn");
                            npc.questItems = GetStringList(npcDict, "questItems");
                            zone.npcs.Add(npc);
                        }
                    }

                    data.zones.Add(zone);
                }
            }

            // Parse sceneToZone
            if (root.TryGetValue("sceneToZone", out object stObj) && stObj is List<object> stList)
            {
                foreach (var item in stList)
                {
                    if (!(item is Dictionary<string, object> mapping)) continue;
                    var m = new SceneZoneMapping();
                    m.key = GetString(mapping, "key");
                    m.value = GetString(mapping, "value");
                    data.sceneToZone.Add(m);
                }
            }

            return data;
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out object val) && val is string s)
                return s;
            return null;
        }

        private static int GetInt(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out object val))
            {
                if (val is long l) return (int)l;
                if (val is double d) return (int)d;
                if (val is int i) return i;
            }
            return 0;
        }

        private static bool GetBool(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out object val) && val is bool b)
                return b;
            return false;
        }

        private static List<string> GetStringList(Dictionary<string, object> dict, string key)
        {
            var result = new List<string>();
            if (dict.TryGetValue(key, out object val) && val is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is string s)
                        result.Add(s);
                }
            }
            return result;
        }

        // ── Public API ──

        public static bool IsLoaded => _loaded;

        private static void BuildIndexes()
        {
            foreach (var zone in _data.zones)
            {
                _zoneIndex[zone.name] = zone;
                _sortedZoneNames.Add(zone.name);

                foreach (var npc in zone.npcs)
                {
                    // Index display name by normalized name
                    if (!string.IsNullOrEmpty(npc.normalizedName) && !_displayNameIndex.ContainsKey(npc.normalizedName))
                        _displayNameIndex[npc.normalizedName] = npc.name;
                }
            }

            _sortedZoneNames.Sort(StringComparer.OrdinalIgnoreCase);

            // Build scene-to-zone mapping
            if (_data.sceneToZone != null)
            {
                foreach (var mapping in _data.sceneToZone)
                    _sceneToZone[mapping.key] = mapping.value;
            }
        }

        /// <summary>
        /// Returns all zone names, sorted alphabetically.
        /// </summary>
        public static List<string> GetAllZones()
        {
            if (!_loaded) return new List<string>();
            return new List<string>(_sortedZoneNames);
        }

        /// <summary>
        /// Returns all NPC entries for a zone, as BestiaryEntry objects.
        /// </summary>
        public static List<BestiaryEntry> GetEntriesForZone(string zoneName)
        {
            var result = new List<BestiaryEntry>();
            if (!_loaded || !_zoneIndex.TryGetValue(zoneName, out var zone))
                return result;

            foreach (var npc in zone.npcs)
            {
                result.Add(new BestiaryEntry(npc, zoneName));
            }

            result.Sort((a, b) => a.MinLevel.CompareTo(b.MinLevel));
            return result;
        }

        /// <summary>
        /// Returns total and discovered NPC counts for a zone.
        /// </summary>
        public static void GetZoneProgress(string zoneName, out int total, out int discovered)
        {
            total = 0;
            discovered = 0;

            if (!_loaded || !_zoneIndex.TryGetValue(zoneName, out var zone))
                return;

            total = zone.npcs.Count;
            foreach (var npc in zone.npcs)
            {
                if (EncounterTracker.IsDiscovered(npc.normalizedName, zoneName))
                    discovered++;
            }
        }

        /// <summary>
        /// Returns zone metadata (level range, dungeon flag).
        /// </summary>
        public static ZoneData GetZoneInfo(string zoneName)
        {
            if (!_loaded) return null;
            _zoneIndex.TryGetValue(zoneName, out var zone);
            return zone;
        }

        /// <summary>
        /// Search across all NPCs by name substring match.
        /// Returns only discovered entries.
        /// </summary>
        public static List<BestiaryEntry> Search(string query)
        {
            var results = new List<BestiaryEntry>();
            if (!_loaded || string.IsNullOrEmpty(query))
                return results;

            string normalized = query.ToLowerInvariant().Trim();
            if (string.IsNullOrEmpty(normalized))
                return results;

            foreach (var zone in _data.zones)
            {
                foreach (var npc in zone.npcs)
                {
                    if (!npc.normalizedName.Contains(normalized))
                        continue;

                    if (!EncounterTracker.IsDiscoveredAnywhere(npc.normalizedName))
                        continue;

                    results.Add(new BestiaryEntry(npc, zone.name));
                }
            }

            results.Sort((a, b) => a.MinLevel.CompareTo(b.MinLevel));
            return results;
        }

        /// <summary>
        /// Returns the proper-case display name for a normalized NPC name.
        /// </summary>
        public static string GetDisplayName(string normalizedName)
        {
            if (!_loaded || _displayNameIndex == null)
                return normalizedName;
            if (_displayNameIndex.TryGetValue(normalizedName, out string display))
                return display;

            // Fallback: title-case
            if (string.IsNullOrEmpty(normalizedName))
                return normalizedName;

            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalizedName);
        }

        /// <summary>
        /// Resolves a Unity scene name to a canonical zone name.
        /// Used by discovery patches to map GameData.SceneName to zone keys.
        /// Falls back to returning the scene name itself if no mapping exists.
        /// </summary>
        public static string ResolveSceneToZone(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return sceneName;

            if (!_loaded)
                return sceneName;

            // Try scene-to-zone mapping
            if (_sceneToZone.TryGetValue(sceneName, out string zone))
                return zone;

            // Try direct zone name match (scene name may already be a canonical zone name)
            if (_zoneIndex.ContainsKey(sceneName))
                return sceneName;

            return sceneName;
        }

        /// <summary>
        /// Returns all canonical zone names that a given NPC appears in.
        /// Used by EncounterTracker.Discover() to create compound keys.
        /// </summary>
        public static List<string> GetZonesForNpc(string normalizedNpcName)
        {
            var result = new List<string>();
            if (!_loaded || string.IsNullOrEmpty(normalizedNpcName))
                return result;

            foreach (var zone in _data.zones)
            {
                foreach (var npc in zone.npcs)
                {
                    if (npc.normalizedName == normalizedNpcName)
                    {
                        result.Add(zone.name);
                        break;
                    }
                }
            }
            return result;
        }
    }

    // ── Data types ──

    internal class BestiaryData
    {
        public int version;
        public List<ZoneData> zones = new List<ZoneData>();
        public List<SceneZoneMapping> sceneToZone = new List<SceneZoneMapping>();
    }

    internal class ZoneData
    {
        public string name;
        public string levelRange;
        public bool isDungeon;
        public List<NpcData> npcs = new List<NpcData>();
    }

    internal class NpcData
    {
        public string name;
        public string normalizedName;
        public int minLevel;
        public int maxLevel;
        public bool isBoss;
        public List<string> loot = new List<string>();
        public List<string> questsGiven = new List<string>();
        public List<string> questsTurnIn = new List<string>();
        public List<string> questItems = new List<string>();
    }

    internal class SceneZoneMapping
    {
        public string key;
        public string value;
    }
}
