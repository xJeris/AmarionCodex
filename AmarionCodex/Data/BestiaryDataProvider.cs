using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace AmarionCodex.Data
{
    internal static class BestiaryDataProvider
    {
        // Collapses runs of whitespace into a single space and trims, for dedup.
        private static string CollapseWhitespace(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        // Maps normalized NPC name -> original display name from PrebuiltDB
        private static Dictionary<string, string> _displayNames;

        // Maps raw ZoneThisLootIsFrom -> canonical atlas zone name
        private static Dictionary<string, string> _zoneNameCache;

        // Entries whose raw zone name lists multiple zones.
        // These entries should appear in ALL listed zones.
        private static readonly Dictionary<string, string[]> MultiZoneMap =
            new Dictionary<string, string[]>
            {
                { "Hidden Hills or Stowaway's Step",            new[] { "Hidden Hills", "Stowaway's Step" } },
                { "Rottenfoot or Silkengrass or Faerie's Brake", new[] { "Rottenfoot", "Silkengrass Meadowlands", "Faerie's Brake" } },
                { "Hidden Hills or Stowaway's Step, at night time", new[] { "Hidden Hills", "Stowaway's Step" } },
            };

        // Exact mapping from raw ZoneThisLootIsFrom values to display names.
        // Built from the complete list of 63 raw zone names in the knowledge database.
        // Only needs updating when the game adds new zones in a patch.
        private static readonly Dictionary<string, string> ZoneMap =
            new Dictionary<string, string>
            {
                { "Abyssal Lake",                                       "Abyssal Lake" },
                { "any zone except Port Azure",                         "Watchers Lens" },
                { "Azynthi's Garden",                                   "Azynthi's Garden" },
                { "Azythi's Garden",                                    "Azynthi's Garden" },
                { "Blacksalt Strand",                                   "Blacksalt Strand" },
                { "Blacksalt Strand at night time",                     "Blacksalt Strand" },
                { "Blight",                                             "The Blight" },
                { "Blooming Sepulcher",                                 "Blooming Sepulcher" },
                { "Bonepits",                                           "The Bonepits" },
                { "Braxonian Desert",                                   "Braxonian Desert" },
                { "Brax's Plane",                                       "Brax's Plane" },
                { "Dark Azynthi's Garden",                              "Azynthi's Garden" },
                { "Dusken Portal",                                      "Mysterious Portals" },
                { "Duskenlight",                                        "The Duskenlight Coast" },
                { "Duskenlight Coast",                                  "The Duskenlight Coast" },
                { "Elderstone Mines",                                   "The Elderstone Mines" },
                { "Faeire's Brake",                                     "Faerie's Brake" },
                { "Faerie's Brake",                                     "Faerie's Brake" },
                { "Fallen Braxonia",                                    "Fallen Braxonia" },
                { "Fernalla's Revival Plains",                          "Fernalla's Revival Plains" },
                { "Fernalla's Revival Plains from Gruhglor's event",    "Fernalla's Revival Plains" },
                { "Hidden Hills",                                       "Hidden Hills" },
                { "Hidden Hills or Stowaway's Step",                    "Hidden Hills" },
                { "Island Tomb",                                        "Island Tomb" },
                { "Jaws",                                               "Jaws of Sivakaya" },
                { "Jaws of Sivakaya",                                   "Jaws of Sivakaya" },
                { "Krakengard",                                         "Old Krakengard" },
                { "Loomingwood",                                        "Loomingwood Forest" },
                { "Lost Cellar",                                        "Lost Cellar" },
                { "Malaroth Nesting Grounds",                           "Malaroth's Nesting Grounds" },
                { "Old Krakengard Prison",                              "Old Krakengard" },
                { "on a Treasure event",                                "Treasure Maps" },
                { "Plane of Brax",                                      "Plane of Brax" },
                { "Plane of Fernalla",                                  "Plane of Fernalla" },
                { "Plane of Soluna",                                    "Plane of Soluna" },
                { "Port Azure",                                         "Port Azure" },
                { "Prielian Cascade",                                   "Prielian Cascade" },
                { "Reliquary",                                          "Reliquary Hall" },
                { "Ripper Portal",                                      "Mysterious Portals" },
                { "Ripper's Keep",                                      "Ripper's Keep" },
                { "Rockshade Hold",                                     "Rockshade Hold" },
                { "Rockshade Hold inside the volcano",                  "Rockshade Hold" },
                { "Rottenfoot",                                         "Rottenfoot" },
                { "Rottenfoot or Silkengrass or Faerie's Brake",        "Rottenfoot" },
                { "Shivering Step",                                     "Shivering Step" },
                { "Silkengrass",                                        "Silkengrass Meadowlands" },
                { "Silkengrass Meadow",                                 "Silkengrass Meadowlands" },
                { "Silkengrass Meadowlands",                            "Silkengrass Meadowlands" },
                { "Soluna's Landing",                                   "Soluna's Landing" },
                { "Stowaway Portal",                                    "Secluded Sanctuary" },
                { "Stowaway's Step",                                    "Stowaway's Step" },
                { "The Blight",                                         "The Blight" },
                { "The Bonepits",                                       "The Bonepits" },
                { "The Braxonian Desert",                               "Braxonian Desert" },
                { "The Fernallan Portal",                               "Mysterious Portals" },
                { "The Island Tomb",                                    "Island Tomb" },
                { "The Reliquary Hall",                                 "Reliquary Hall" },
                { "Underspine Hollow",                                  "Underspine Hollow" },
                { "Vitheo's Rest",                                      "Vitheo's Rest" },
                { "Vitheo's Watch",                                     "Vitheo's Watch" },
                { "Vitheo's Watch, at night time",                      "Vitheo's Watch" },
                { "Willowwatch",                                        "Willowwatch Ridge" },
                { "Windwashed Pass",                                    "Windwashed Pass" },
                // Unity scene names (GameData.SceneName) — from GetCommonTerms.GetZoneTerm()
                // Only entries whose scene name differs from any existing key above.
                { "Azure",                                              "Port Azure" },
                { "SaltedStrand",                                       "Blacksalt Strand" },
                { "Brake",                                              "Faerie's Brake" },
                { "Braxonia",                                           "Fallen Braxonia" },
                { "Braxonian",                                          "Braxonian Desert" },
                { "Hidden",                                             "Hidden Hills" },
                { "Stowaway",                                           "Stowaway's Step" },
                { "Vitheo",                                             "Vitheo's Watch" },
                { "Fernalla",                                           "Fernalla's Revival Plains" },
                { "FernallaField",                                      "Fernalla's Revival Plains" },
                // "Bonepits" already mapped above as a loot zone name
                { "Elderstone",                                         "The Elderstone Mines" },
                { "VitheosEnd",                                         "Vitheo's Rest" },
                { "Ripper",                                             "Ripper's Keep" },
                { "PrielPlateau",                                       "Prielian Cascade" },
                { "Abyssal",                                            "Abyssal Lake" },
                { "Tutorial",                                           "Island Tomb" },
                { "ShiveringStep",                                      "Shivering Step" },
                { "AzynthiClear",                                       "Azynthi's Garden" },
                { "Soluna",                                             "Soluna's Landing" },
                { "Malaroth",                                           "Malaroth's Nesting Grounds" },
            };

        /// <summary>
        /// Maps a raw zone name (from LootTable.ZoneThisLootIsFrom) to a
        /// canonical display name. Uses exact dictionary lookup.
        /// Falls back to the raw name if not mapped (and logs a warning).
        /// </summary>
        private static string NormalizeZoneName(string rawZone)
        {
            if (string.IsNullOrEmpty(rawZone))
                return rawZone;

            if (_zoneNameCache == null)
                _zoneNameCache = new Dictionary<string, string>();

            if (_zoneNameCache.TryGetValue(rawZone, out string cached))
                return cached;

            if (ZoneMap.TryGetValue(rawZone, out string mapped))
            {
                _zoneNameCache[rawZone] = mapped;
                return mapped;
            }

            // Unknown zone — log so we can add it to the map
            Plugin.Log.LogWarning($"Unmapped zone name: \"{rawZone}\"");
            _zoneNameCache[rawZone] = rawZone;
            return rawZone;
        }

        /// <summary>
        /// Returns all canonical zone names for a given raw zone string.
        /// Multi-zone entries return multiple zones; single-zone entries return one.
        /// </summary>
        public static List<string> GetCanonicalZones(string rawZone)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(rawZone))
                return result;

            if (MultiZoneMap.TryGetValue(rawZone, out string[] zones))
            {
                result.AddRange(zones);
            }
            else
            {
                result.Add(NormalizeZoneName(rawZone));
            }
            return result;
        }

        /// <summary>
        /// Returns true if the given entry belongs to the specified zone.
        /// Handles multi-zone entries that appear in multiple zones.
        /// </summary>
        private static bool EntryBelongsToZone(string rawZoneName, string zoneName)
        {
            if (string.IsNullOrEmpty(rawZoneName))
                return false;

            // Check multi-zone map first
            if (MultiZoneMap.TryGetValue(rawZoneName, out string[] zones))
            {
                foreach (var z in zones)
                {
                    if (z == zoneName)
                        return true;
                }
                return false;
            }

            // Standard single-zone check
            return NormalizeZoneName(rawZoneName) == zoneName;
        }

        /// <summary>
        /// Returns all distinct zone names (with day/night merged) that have
        /// at least one NPC entry, sorted alphabetically. Also includes zones
        /// where the player has discovered NPCs even if no knowledge DB entries
        /// list those zones.
        /// </summary>
        public static List<string> GetAllZones()
        {
            var zones = new HashSet<string>();
            foreach (var entry in GameData.KnowledgeDatabase.GameKnowledge)
            {
                if (string.IsNullOrEmpty(entry.NPCZoneName))
                    continue;

                // Add all zones for multi-zone entries
                if (MultiZoneMap.TryGetValue(entry.NPCZoneName, out string[] multiZones))
                {
                    foreach (var z in multiZones)
                        zones.Add(z);
                }
                else
                {
                    zones.Add(NormalizeZoneName(entry.NPCZoneName));
                }
            }

            // Include zones learned at runtime via the shared spawn registry
            foreach (var zone in ZoneSpawnRegistry.GetAllZones())
                zones.Add(zone);

            var list = new List<string>(zones);
            list.Sort();
            return list;
        }

        /// <summary>
        /// Returns deduplicated bestiary entries for a given zone, sorted by
        /// minimum level. Multiple KnowledgeEntry records for the same NPC name
        /// are merged into a single BestiaryEntry with a level range.
        /// Also includes NPCs that the player discovered in this zone even if the
        /// knowledge database lists them under a different zone (e.g., a mob that
        /// spawns in Blacksalt Strand but is catalogued under Rottenfoot).
        /// </summary>
        public static List<BestiaryEntry> GetEntriesForZone(string zoneName)
        {
            // Use whitespace-collapsed key to prevent duplicates caused by
            // entries whose normalized names differ only in spacing.
            var byKey = new Dictionary<string, BestiaryEntry>();
            var order = new List<string>();
            foreach (var entry in GameData.KnowledgeDatabase.GameKnowledge)
            {
                if (!EntryBelongsToZone(entry.NPCZoneName, zoneName))
                    continue;

                string dedupKey = CollapseWhitespace(entry.NPCName);
                if (byKey.TryGetValue(dedupKey, out var existing))
                {
                    existing.AddVariant(entry);
                }
                else
                {
                    byKey[dedupKey] = new BestiaryEntry(entry);
                    order.Add(dedupKey);
                }
            }

            // Add NPCs from the shared spawn registry that aren't in the
            // knowledge DB for this zone. Use the first knowledge entry we find
            // (from any zone) so we have loot/quest/level data to show.
            var registryNpcs = ZoneSpawnRegistry.GetNpcsForZone(zoneName);
            if (registryNpcs != null)
            {
                foreach (var npcName in registryNpcs)
                {
                    string dedupKey = CollapseWhitespace(npcName);
                    if (byKey.ContainsKey(dedupKey))
                        continue;

                    // Find this NPC's knowledge entry from any zone
                    KnowledgeEntry found = null;
                    foreach (var entry in GameData.KnowledgeDatabase.GameKnowledge)
                    {
                        if (CollapseWhitespace(entry.NPCName) == dedupKey)
                        {
                            found = entry;
                            break;
                        }
                    }

                    if (found != null)
                    {
                        byKey[dedupKey] = new BestiaryEntry(found);
                        order.Add(dedupKey);
                    }
                }
            }

            var result = new List<BestiaryEntry>(order.Count);
            foreach (var key in order)
                result.Add(byKey[key]);
            result.Sort((a, b) => a.MinLevel.CompareTo(b.MinLevel));
            return result;
        }

        /// <summary>
        /// Returns the total number of unique NPCs and the number discovered for a zone.
        /// Multiple level variants of the same NPC count as one.
        /// Includes NPCs discovered in this zone that aren't in the knowledge DB listing.
        /// </summary>
        public static void GetZoneProgress(string zoneName, out int total, out int discovered)
        {
            var seen = new HashSet<string>();
            total = 0;
            discovered = 0;
            foreach (var entry in GameData.KnowledgeDatabase.GameKnowledge)
            {
                if (!EntryBelongsToZone(entry.NPCZoneName, zoneName))
                    continue;

                string dedupKey = CollapseWhitespace(entry.NPCName);
                if (!seen.Add(dedupKey))
                    continue;

                total++;
                if (EncounterTracker.IsDiscovered(entry.NPCName, zoneName))
                    discovered++;
            }

            // Count NPCs from the shared spawn registry not in the knowledge DB
            var registryNpcs = ZoneSpawnRegistry.GetNpcsForZone(zoneName);
            if (registryNpcs != null)
            {
                foreach (var npcName in registryNpcs)
                {
                    string dedupKey = CollapseWhitespace(npcName);
                    if (seen.Add(dedupKey))
                    {
                        total++;
                        if (EncounterTracker.IsDiscovered(npcName, zoneName))
                            discovered++;
                    }
                }
            }
        }

        /// <summary>
        /// Search across all entries by name substring match on normalized names.
        /// Returns only discovered entries, deduplicated by NPC name.
        /// </summary>
        public static List<BestiaryEntry> Search(string query)
        {
            if (string.IsNullOrEmpty(query))
                return new List<BestiaryEntry>();

            string normalized = GameData.KnowledgeDatabase.Normalize(query);
            if (string.IsNullOrEmpty(normalized))
                return new List<BestiaryEntry>();

            var byKey = new Dictionary<string, BestiaryEntry>();
            var order = new List<string>();
            foreach (var entry in GameData.KnowledgeDatabase.GameKnowledge)
            {
                if (string.IsNullOrEmpty(entry.NPCName))
                    continue;

                // Only return discovered entries in search results
                if (!EncounterTracker.IsDiscoveredAnywhere(entry.NPCName))
                    continue;

                if (!entry.NPCName.Contains(normalized))
                    continue;

                string dedupKey = CollapseWhitespace(entry.NPCName);
                if (byKey.TryGetValue(dedupKey, out var existing))
                {
                    existing.AddVariant(entry);
                }
                else
                {
                    byKey[dedupKey] = new BestiaryEntry(entry);
                    order.Add(dedupKey);
                }
            }

            var results = new List<BestiaryEntry>(order.Count);
            foreach (var key in order)
                results.Add(byKey[key]);
            results.Sort((a, b) => a.MinLevel.CompareTo(b.MinLevel));
            return results;
        }

        /// <summary>
        /// Get zone info from the game's ZoneAtlas for level range / dungeon status.
        /// Tries the display name directly, then searches atlas entries for any
        /// whose ZoneName appears in the display name (reverse keyword match).
        /// </summary>
        public static ZoneAtlasEntry GetZoneInfo(string zoneName)
        {
            // Try direct lookup first
            var info = ZoneAtlas.FindZoneInfo(zoneName);
            if (info != null)
                return info;

            // Try finding an atlas entry whose short name is contained in the display name
            if (ZoneAtlas.Atlas != null)
            {
                ZoneAtlasEntry bestEntry = null;
                foreach (var entry in ZoneAtlas.Atlas)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.ZoneName))
                        continue;

                    if (zoneName.IndexOf(entry.ZoneName, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                        (bestEntry == null || entry.ZoneName.Length > bestEntry.ZoneName.Length))
                    {
                        bestEntry = entry;
                    }
                }
                if (bestEntry != null)
                    return bestEntry;
            }

            return null;
        }

        /// <summary>
        /// Returns the original display name for an NPC (proper case as set in the
        /// game data) given the normalized name stored in KnowledgeEntry.NPCName.
        /// Falls back to title-casing the normalized name if no original is found.
        /// </summary>
        public static string GetDisplayName(KnowledgeEntry entry)
        {
            EnsureDisplayNames();

            if (_displayNames != null && _displayNames.TryGetValue(entry.NPCName, out string original))
                return original;

            // Fallback: title-case the normalized name
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(entry.NPCName);
        }

        /// <summary>
        /// Builds the display name lookup from the PrebuiltDB asset via reflection.
        /// Only runs once.
        /// </summary>
        private static void EnsureDisplayNames()
        {
            if (_displayNames != null)
                return;

            _displayNames = new Dictionary<string, string>();

            try
            {
                var kdb = GameData.KnowledgeDatabase;
                if (kdb == null) return;

                var field = AccessTools.Field(typeof(KnowledgeDatabase), "PrebuiltDB");
                if (field == null) return;

                var prebuilt = field.GetValue(kdb) as KnowledgeDatabaseAsset;
                if (prebuilt?.Entries == null) return;

                foreach (var entry in prebuilt.Entries)
                {
                    if (string.IsNullOrEmpty(entry.NPCName))
                        continue;

                    string normalized = kdb.Normalize(entry.NPCName);
                    // Keep the first occurrence (shouldn't have duplicates, but be safe)
                    if (!_displayNames.ContainsKey(normalized))
                        _displayNames[normalized] = entry.NPCName;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"Could not build display name lookup: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the cached display name lookup (call on game reload if needed).
        /// </summary>
        public static void ClearCache()
        {
            _displayNames = null;
            _zoneNameCache = null;
        }
    }
}
