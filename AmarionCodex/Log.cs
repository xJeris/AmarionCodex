using System;

namespace AmarionCodex
{
    /// <summary>
    /// Logging abstraction. LunarisEntry wires these delegates to
    /// Lunaris.Logging during Awake(). All shared code uses these
    /// instead of referencing Lunaris.Logging directly.
    /// </summary>
    public static class Log
    {
        public static Action<string> Info = _ => { };
        public static Action<string> Warning = _ => { };
        public static Action<string> Error = _ => { };
    }
}
