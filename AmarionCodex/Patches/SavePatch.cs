using AmarionCodex.Data;
using HarmonyLib;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Persists encounter data whenever the game saves.
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

            EncounterTracker.Load(GameData.CurrentCharacterSlot.index);

            // Auto-discover Reliquary Hall NPCs on zone entry.
            // The Reliquary Ward and Fiend are one-time-per-account kills that
            // despawn permanently, so grant discovery credit just for zoning in.
            string scene = GameData.SceneName ?? "";
            string zone = BestiaryDatabase.ResolveSceneToZone(scene);
            if (zone == "Reliquary Hall")
            {
                EncounterTracker.Discover("reliquary ward", scene);
                EncounterTracker.Discover("reliquary fiend", scene);
            }
        }
    }
}
