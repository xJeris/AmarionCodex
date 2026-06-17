using AmarionCodex.Data;
using AmarionCodex.UI;
using HarmonyLib;
using UnityEngine;

namespace AmarionCodex
{
    /// <summary>
    /// Core plugin logic. LunarisEntry delegates to this class.
    /// </summary>
    internal class PluginCore
    {
        public const string PluginGuid = "com.amarion.codex";
        public const string PluginName = "Amarion Codex";
        public const string PluginVersion = "0.3.3";

        internal static PluginCore Instance;

        internal KeyCode OpenCodexKey;
        internal string ChatCommand;

        private CodexWindow _codexWindow;
        private GameObject _windowGO;
        private Harmony _harmony;

        public void Initialize(string pluginDir)
        {
            Instance = this;

            BestiaryDatabase.Load(pluginDir);

            if (!BestiaryDatabase.IsLoaded)
                Log.Warning("Bestiary database failed to load — codex will be empty");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(PluginCore).Assembly);
        }

        public void OnUpdate()
        {
            if (GameData.PlayerTyping)
                return;

            if (_codexWindow != null && _codexWindow.IsSearchFocused)
                return;

            if (Input.GetKeyDown(OpenCodexKey))
                ToggleCodexWindow();
        }

        internal void ToggleCodexWindow()
        {
            if (!BestiaryDatabase.IsLoaded)
                return;

            if (_codexWindow == null)
            {
                ItemLookup.Init();

                _windowGO = CodexWindowBuilder.Build();
                _codexWindow = _windowGO.GetComponent<CodexWindow>();

                if (GameData.Misc != null && GameData.Misc.UIWindows != null)
                    GameData.Misc.UIWindows.Add(_codexWindow.WindowPanel);
            }

            _codexWindow.Toggle();
        }

        public void Shutdown()
        {
            _harmony?.UnpatchSelf();

            if (_codexWindow != null && GameData.Misc != null && GameData.Misc.UIWindows != null)
                GameData.Misc.UIWindows.Remove(_codexWindow.WindowPanel);

            if (_windowGO != null)
                Object.Destroy(_windowGO);

            // Reset all static state for hot-reload safety (Lunaris)
            EncounterTracker.Reset();
            BestiaryDatabase.Reset();
            ItemLookup.Reset();

            // Clear logging delegates
            Log.Info = _ => { };
            Log.Warning = _ => { };
            Log.Error = _ => { };

            Instance = null;
        }
    }
}
