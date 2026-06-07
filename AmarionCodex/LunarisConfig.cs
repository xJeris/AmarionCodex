using Lunaris.Config;

namespace AmarionCodex
{
    public class LunarisConfig
    {
        [ConfigSection("General")]
        [ConfigDescription("Key to open/close the Amarion Codex window (e.g. K, L, F9). Must be a valid Unity KeyCode name.")]
        public string OpenCodexKey = "K";

        [ConfigSection("General")]
        [ConfigDescription("Chat command to open the codex (always also accepts /bestiary).")]
        public string ChatCommand = "/codex";
    }
}
