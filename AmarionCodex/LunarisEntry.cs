using System.IO;
using System.Reflection;
using UnityEngine;
using Lunaris;

[assembly: AssemblyMetadata("LunarisPluginId", "amarioncodex")]

namespace AmarionCodex
{
    [LunarisPlugin("Amarion Codex", PluginCore.PluginVersion, "Amarion",
        "In-game bestiary and NPC knowledge database")]
    public class LunarisEntry : Lunaris.LunarisPlugin
    {
        private PluginCore _core;

        private void Awake()
        {
            // Wire logging to Lunaris
            Log.Info = s => Logging.LogInfo(s);
            Log.Warning = s => Logging.LogWarning(s);
            Log.Error = s => Logging.LogError(s);

            // Read config via Lunaris API
            var openKey = Config.Read("OpenCodexKey", KeyCode.K);
            var chatCommand = Config.Read("ChatCommand", "/codex");

            _core = new PluginCore
            {
                OpenCodexKey = openKey,
                ChatCommand = chatCommand
            };

            // Lunaris plugins live in <GameDir>/plugins/
            string pluginDir = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            _core.Initialize(pluginDir);
        }

        private void Update() => _core?.OnUpdate();

        private void OnDestroy()
        {
            _core?.Shutdown();
            _core = null;
        }
    }
}
