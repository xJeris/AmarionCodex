using System.IO;
using AmarionCodex.Data;
using AmarionCodex.UI;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AmarionCodex
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.amarion.codex";
        public const string PluginName = "Amarion Codex";
        public const string PluginVersion = "0.2.3";

        internal static Plugin Instance;
        internal static Harmony HarmonyInstance;
        internal static PluginConfig Cfg;

        private CodexWindow _codexWindow;
        private GameObject _windowGO;

        private void Awake()
        {
            Instance = this;
            Cfg = new PluginConfig(Config);

            // Load static bestiary data from JSON
            string pluginDir = Path.GetDirectoryName(Info.Location);
            BestiaryDatabase.Load(pluginDir);

            if (!BestiaryDatabase.IsLoaded)
                Logger.LogWarning("Bestiary database failed to load — codex will be empty");

            HarmonyInstance = new Harmony(PluginGuid);
            HarmonyInstance.PatchAll(typeof(Plugin).Assembly);
        }

        private void Update()
        {
            if (GameData.PlayerTyping)
                return;

            // Don't process keybind while typing in the codex search field
            if (_codexWindow != null && _codexWindow.IsSearchFocused)
                return;

            if (Input.GetKeyDown(Cfg.OpenCodexKey.Value))
                ToggleCodexWindow();
        }

        internal void ToggleCodexWindow()
        {
            // Don't open until the bestiary data is loaded
            if (!BestiaryDatabase.IsLoaded)
                return;

            if (_codexWindow == null)
            {
                // Initialize item lookup for clickable loot (needs GameData.ItemDB)
                ItemLookup.Init();

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
