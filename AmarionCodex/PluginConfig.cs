using BepInEx.Configuration;
using UnityEngine;

namespace AmarionCodex
{
    internal class PluginConfig
    {
        public ConfigEntry<KeyCode> OpenCodexKey;
        public ConfigEntry<string> ChatCommand;

        public PluginConfig(ConfigFile config)
        {
            OpenCodexKey = config.Bind("General", "OpenCodexKey", KeyCode.K,
                "Key to open/close the Amarion Codex window");
            ChatCommand = config.Bind("General", "ChatCommand", "/codex",
                "Chat command to open the codex (always also accepts /bestiary)");
        }
    }
}
