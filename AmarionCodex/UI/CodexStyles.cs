using UnityEngine;

namespace AmarionCodex.UI
{
    /// <summary>
    /// Color palette and layout constants for the tome/parchment aesthetic.
    /// </summary>
    internal static class CodexStyles
    {
        // ── Parchment / Background ──
        public static readonly Color Parchment      = new Color32(232, 214, 181, 245);
        public static readonly Color ParchmentDark   = new Color32(200, 178, 141, 255);
        public static readonly Color ParchmentEdge   = new Color32(139, 109, 70, 255);
        public static readonly Color TitleBarTop     = new Color32(107, 79, 46, 255);
        public static readonly Color TitleBarBottom  = new Color32(90, 62, 27, 255);
        public static readonly Color StatusBar       = new Color32(90, 62, 27, 255);
        public static readonly Color BorderDark      = new Color32(61, 42, 16, 255);

        // ── Text Colors ──
        public static readonly Color InkDark         = new Color32(51, 33, 14, 255);
        public static readonly Color InkFaded        = new Color32(120, 90, 50, 255);
        public static readonly Color InkLight        = new Color32(168, 144, 96, 255);
        public static readonly Color InkRed          = new Color32(139, 35, 35, 255);
        public static readonly Color Undiscovered    = new Color32(160, 140, 110, 180);
        public static readonly Color TitleText       = new Color32(232, 214, 181, 255);
        public static readonly Color StatusText      = new Color32(168, 144, 96, 255);

        // ── Zone List ──
        public static readonly Color ZoneActive      = new Color32(180, 150, 60, 255);
        public static readonly Color ZoneCountBg     = new Color(0, 0, 0, 0.06f);

        // ── Buttons / Interactive ──
        public static readonly Color ButtonHover     = new Color(0.55f, 0.43f, 0.27f, 0.15f);
        public static readonly Color ScrollbarThumb  = new Color32(139, 109, 70, 255);
        public static readonly Color ScrollbarTrack  = new Color32(200, 178, 141, 100);

        // ── Search Field ──
        public static readonly Color SearchBg        = new Color32(240, 228, 204, 255);
        public static readonly Color SearchBorder    = new Color32(168, 144, 96, 255);
        public static readonly Color SearchFocus     = new Color32(107, 79, 46, 255);

        // ── Entry Row ──
        public static readonly Color RowBorder       = new Color(0.55f, 0.43f, 0.27f, 0.12f);
        public static readonly Color DetailDivider   = new Color32(168, 144, 96, 255);

        // ── Loot Tier Colors ──
        public static readonly Color TierCommon      = new Color32(107, 107, 79, 255);
        public static readonly Color TierUncommon    = new Color32(46, 107, 46, 255);
        public static readonly Color TierRare        = new Color32(46, 79, 139, 255);
        public static readonly Color TierLegendary   = new Color32(139, 107, 46, 255);

        // ── Layout Constants ──
        public const float WindowWidth    = 860f;
        public const float WindowHeight   = 640f;
        public const float LeftPanelWidth = 215f;
        public const float TitleBarHeight = 32f;
        public const float StatusBarHeight = 22f;
        public const float SearchBarHeight = 38f;
        public const float EntryRowHeight = 30f;
        public const float ZoneRowHeight  = 34f;
        public const float Padding        = 8f;

        // ── Font Sizes ──
        public const float TitleFontSize        = 18f;
        public const float ZoneHeaderFontSize   = 20f;
        public const float ZoneNameFontSize     = 11f;
        public const float ZoneLevelFontSize    = 10f;
        public const float EntryNameFontSize    = 14f;
        public const float EntryLevelFontSize   = 12f;
        public const float DetailTitleFontSize  = 22f;
        public const float DetailBodyFontSize   = 13f;
        public const float DetailSectionFontSize = 15f;
        public const float SearchFontSize       = 13f;
        public const float StatusFontSize       = 11f;
        public const float BadgeFontSize        = 10f;
    }
}
