using System.Collections.Generic;
using UnityEngine;

namespace AmarionCodex.Data
{
    /// <summary>
    /// Builds a name-to-Item lookup dictionary from the game's ItemDatabase
    /// for use by the codex UI when rendering clickable loot items.
    /// </summary>
    internal static class ItemLookup
    {
        private static Dictionary<string, Item> _itemsByName;
        private static bool _initialized;

        /// <summary>
        /// Builds the lookup dictionary from GameData.ItemDB.
        /// Safe to call multiple times — only runs once.
        /// Must be called after GameData.ItemDB is available.
        /// </summary>
        public static void Init()
        {
            if (_initialized)
                return;

            _initialized = true;
            _itemsByName = new Dictionary<string, Item>();

            if (GameData.ItemDB == null || GameData.ItemDB.ItemDB == null)
            {
                Debug.LogWarning("[AmarionCodex] ItemDatabase not available — loot links disabled");
                return;
            }

            foreach (var item in GameData.ItemDB.ItemDB)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemName))
                    continue;

                // First match wins for duplicate names (e.g. "Soul Gem" x4).
                // All instances share the same base stats so any one works.
                if (!_itemsByName.ContainsKey(item.ItemName))
                    _itemsByName[item.ItemName] = item;
            }

            Debug.Log($"[AmarionCodex] ItemLookup initialized with {_itemsByName.Count} items");
        }

        /// <summary>
        /// Resets all static state for hot-reload safety.
        /// </summary>
        public static void Reset()
        {
            _itemsByName = null;
            _initialized = false;
        }

        /// <summary>
        /// Finds an Item by its display name. Returns null if not found or not initialized.
        /// </summary>
        public static Item FindByName(string itemName)
        {
            if (_itemsByName == null || string.IsNullOrEmpty(itemName))
                return null;

            _itemsByName.TryGetValue(itemName, out var item);
            return item;
        }
    }
}
