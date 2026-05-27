using AmarionCodex.Data;
using HarmonyLib;
using UnityEngine.UI;

namespace AmarionCodex.Patches
{
    /// <summary>
    /// Intercepts /codex and /bestiary chat commands before the game's own CheckCommands runs.
    /// CheckCommands is private, called from CheckInput(). After CheckCommands returns,
    /// CheckInput() already clears typed.text and calls CloseInputBox(), so we just need
    /// to prevent the "Command not recognized" message by returning early.
    /// </summary>
    [HarmonyPatch(typeof(TypeText), "CheckCommands")]
    internal static class ChatCommandPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TypeText __instance)
        {
            // Access the typed text field (public Text typed)
            Text typedField = __instance.typed;
            if (typedField == null || string.IsNullOrEmpty(typedField.text))
                return true;

            string text = typedField.text.ToLower().Trim();

            if (text == "/codex" || text == "/bestiary" || text == Plugin.Cfg.ChatCommand.Value.ToLower())
            {
                Plugin.Instance.ToggleCodexWindow();
                return false; // skip original CheckCommands — CheckInput handles cleanup
            }

            if (text == "/codexreset")
            {
                EncounterTracker.Clear();
                ZoneSpawnRegistry.Clear();
                EncounterTracker.Save();
                ZoneSpawnRegistry.Save();
                BestiaryDataProvider.ClearCache();
                return false;
            }

            return true; // let original run
        }
    }
}
