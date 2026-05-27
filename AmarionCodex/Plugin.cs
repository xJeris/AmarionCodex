using AmarionCodex.Data;
using AmarionCodex.UI;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AmarionCodex
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.amarion.codex";
        public const string PluginName = "Amarion Codex";
        public const string PluginVersion = "0.1.0";

        internal static Plugin Instance;
        internal static ManualLogSource Log;
        internal static Harmony HarmonyInstance;
        internal static PluginConfig Cfg;

        private CodexWindow _codexWindow;
        private GameObject _windowGO;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Cfg = new PluginConfig(Config);

            HarmonyInstance = new Harmony(PluginGuid);
            HarmonyInstance.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void Update()
        {
            if (GameData.PlayerTyping)
                return;

            if (Input.GetKeyDown(Cfg.OpenCodexKey.Value))
                ToggleCodexWindow();
        }

        internal void ToggleCodexWindow()
        {
            // Don't open until the knowledge database is ready
            if (GameData.KnowledgeDatabase == null)
                return;

            if (_codexWindow == null)
            {
                _windowGO = CodexWindowBuilder.Build();
                _codexWindow = _windowGO.GetComponent<CodexWindow>();

                // Register with the game's UI window list so Escape closes it
                if (GameData.Misc != null && GameData.Misc.UIWindows != null)
                    GameData.Misc.UIWindows.Add(_codexWindow.WindowPanel);
            }

            _codexWindow.Toggle();
        }

        private void OnDestroy()
        {
            HarmonyInstance?.UnpatchSelf();

            if (_codexWindow != null && GameData.Misc != null && GameData.Misc.UIWindows != null)
                GameData.Misc.UIWindows.Remove(_codexWindow.WindowPanel);

            if (_windowGO != null)
                Destroy(_windowGO);
        }
    }
}
