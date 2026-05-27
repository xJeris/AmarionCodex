using AmarionCodex.Data;
using HarmonyLib;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Persists encounter data whenever the game saves.
    /// Also loads encounter data when GameManager.Start() fires (entering the game world).
    /// </summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.SaveGameData))]
    internal static class SavePatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            if (GameData.CurrentCharacterSlot == null)
                return;

            EncounterTracker.Save(GameData.CurrentCharacterSlot.index);
            ZoneSpawnRegistry.Save();
        }
    }

    /// <summary>
    /// Loads encounter data when the player enters the game world.
    /// PlayerControl.Start() runs after character selection and scene load,
    /// so CurrentCharacterSlot is guaranteed to be set.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControl), "Start")]
    internal static class LoadPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            if (GameData.CurrentCharacterSlot == null)
                return;

            ZoneSpawnRegistry.Load();
            EncounterTracker.Load(GameData.CurrentCharacterSlot.index);
        }
    }
}
