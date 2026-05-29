using System.Collections.Generic;

namespace AmarionCodex.Data
{
    /// <summary>
    /// A bestiary entry representing a unique NPC within a zone.
    /// Wraps the static JSON data from bestiary_data.json.
    /// </summary>
    internal class BestiaryEntry
    {
        /// <summary>NPC display name (proper case).</summary>
        public string Name;

        /// <summary>Normalized NPC name (lowercase) used as discovery key.</summary>
        public string NormalizedName;

        /// <summary>Lowest level this NPC appears at.</summary>
        public int MinLevel;

        /// <summary>Highest level this NPC appears at.</summary>
        public int MaxLevel;

        /// <summary>Whether this NPC is a boss/unique.</summary>
        public bool IsBoss;

        /// <summary>Zone this entry belongs to.</summary>
        public string ZoneName;

        /// <summary>Loot drop names.</summary>
        public List<string> Loot;

        /// <summary>All quest names this NPC is associated with (deduplicated).</summary>
        public List<string> Quests;

        /// <summary>
        /// Returns a display string for the level: "Level 5" or "Level 5-10".
        /// </summary>
        public string LevelString
        {
            get
            {
                if (MinLevel == MaxLevel)
                    return $"Level {MinLevel}";
                return $"Level {MinLevel}-{MaxLevel}";
            }
        }

        public BestiaryEntry(NpcData npc, string zoneName)
        {
            Name = npc.name;
            NormalizedName = npc.normalizedName;
            MinLevel = npc.minLevel;
            MaxLevel = npc.maxLevel;
            IsBoss = npc.isBoss;
            ZoneName = zoneName;
            Loot = npc.loot ?? new List<string>();
            var questSet = new HashSet<string>();
            if (npc.questsGiven != null)
                foreach (var q in npc.questsGiven) if (!string.IsNullOrEmpty(q)) questSet.Add(q);
            if (npc.questsTurnIn != null)
                foreach (var q in npc.questsTurnIn) if (!string.IsNullOrEmpty(q)) questSet.Add(q);
            if (npc.questItems != null)
                foreach (var q in npc.questItems) if (!string.IsNullOrEmpty(q)) questSet.Add(q);
            Quests = new List<string>(questSet);
        }
    }
}
