namespace AmarionCodex.Data
{
    /// <summary>
    /// A deduplicated bestiary entry representing a unique NPC within a zone.
    /// Combines multiple KnowledgeEntry records (same NPC at different levels)
    /// into a single entry with a level range.
    /// </summary>
    internal class BestiaryEntry
    {
        /// <summary>
        /// A representative KnowledgeEntry (the lowest-level one) used for
        /// name, loot, quests, boss flag, etc.
        /// </summary>
        public KnowledgeEntry Entry;

        /// <summary>Lowest level this NPC appears at in this zone.</summary>
        public int MinLevel;

        /// <summary>Highest level this NPC appears at in this zone.</summary>
        public int MaxLevel;

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

        public BestiaryEntry(KnowledgeEntry entry)
        {
            Entry = entry;
            MinLevel = entry.NPCLevel;
            MaxLevel = entry.NPCLevel;
        }

        /// <summary>
        /// Incorporate another level variant of the same NPC.
        /// Keeps the lowest-level entry as the representative and expands the range.
        /// </summary>
        public void AddVariant(KnowledgeEntry entry)
        {
            if (entry.NPCLevel < MinLevel)
            {
                MinLevel = entry.NPCLevel;
                Entry = entry;
            }
            if (entry.NPCLevel > MaxLevel)
                MaxLevel = entry.NPCLevel;
        }
    }
}
