using System.Collections.Generic;

namespace AmarionCodex.Data
{
    /// <summary>
    /// Provides bestiary data to the UI layer.
    /// Delegates to BestiaryDatabase (static JSON) for all queries.
    /// </summary>
    internal static class BestiaryDataProvider
    {
        public static List<string> GetAllZones()
        {
            return BestiaryDatabase.GetAllZones();
        }

        public static List<BestiaryEntry> GetEntriesForZone(string zoneName)
        {
            return BestiaryDatabase.GetEntriesForZone(zoneName);
        }

        public static void GetZoneProgress(string zoneName, out int total, out int discovered)
        {
            BestiaryDatabase.GetZoneProgress(zoneName, out total, out discovered);
        }

        /// <summary>
        /// Returns zone metadata (level range, dungeon flag), or null if zone not found.
        /// </summary>
        public static ZoneData GetZoneInfo(string zoneName)
        {
            return BestiaryDatabase.GetZoneInfo(zoneName);
        }

        public static List<BestiaryEntry> Search(string query)
        {
            return BestiaryDatabase.Search(query);
        }
    }
}
