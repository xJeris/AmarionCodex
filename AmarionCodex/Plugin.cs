using System.IO;
using BepInEx;
using UnityEngine;

namespace AmarionCodex
{
    [BepInPlugin(PluginCore.PluginGuid, PluginCore.PluginName, PluginCore.PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        internal static PluginConfig Cfg;

        private PluginCore _core;

        private void Awake()
        {
            // Wire logging to BepInEx
            Log.Info = s => Logger.LogInfo(s);
            Log.Warning = s => Logger.LogWarning(s);
            Log.Error = s => Logger.LogError(s);

            Cfg = new PluginConfig(Config);

            _core = new PluginCore
            {
                OpenCodexKey = Cfg.OpenCodexKey.Value,
                ChatCommand = Cfg.ChatCommand.Value
            };

            string pluginDir = Path.GetDirectoryName(Info.Location);
            _core.Initialize(pluginDir);
        }

        private void Update() => _core?.OnUpdate();

        private void OnDestroy() => _core?.Shutdown();
    }
}
