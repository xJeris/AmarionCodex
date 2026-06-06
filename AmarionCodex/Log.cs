using System;

namespace AmarionCodex
{
    /// <summary>
    /// Loader-agnostic logging abstraction. Each entry point (BepInEx Plugin
    /// or Lunaris LunarisEntry) wires these delegates to its own logging API
    /// during Awake(). All shared code uses these instead of referencing
    /// BepInEx.Logging or Lunaris.Logging directly.
    /// </summary>
    public static class Log
    {
        public static Action<string> Info = _ => { };
        public static Action<string> Warning = _ => { };
        public static Action<string> Error = _ => { };
    }
}
