using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Lunaris;

[assembly: AssemblyMetadata("LunarisPluginId", "amarioncodex")]

namespace AmarionCodex
{
    [LunarisPlugin("Amarion Codex", PluginCore.PluginVersion, "Amarion",
        "In-game bestiary and NPC knowledge database")]
    public class LunarisEntry : LunarisPlugin
    {
        public static LunarisConfig Settings { get; private set; }

        private PluginCore _core;

        private void Awake()
        {
            // Wire logging to Lunaris
            Log.Info = s => Logging.LogInfo(s);
            Log.Warning = s => Logging.LogWarning(s);
            Log.Error = s => Logging.LogError(s);

            // Register configuration via Lunaris
            Settings = Config.Register<LunarisConfig>().Get();

            _core = new PluginCore
            {
                OpenCodexKey = ParseKeyCode(Settings.OpenCodexKey),
                ChatCommand = Settings.ChatCommand
            };

            // Lunaris plugins live in <GameDir>/plugins/
            string pluginDir = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            _core.Initialize(pluginDir);
        }

        private static KeyCode ParseKeyCode(string name)
        {
            try { return (KeyCode)Enum.Parse(typeof(KeyCode), name, true); }
            catch { return KeyCode.K; }
        }

        private void Update() => _core?.OnUpdate();

        private void OnDestroy()
        {
            _core?.Shutdown();
            _core = null;
        }
    }
}
