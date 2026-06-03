using System.Drawing;
using SpiceChecker.Services;

namespace SpiceChecker.Models
{
    public enum BackdropEffect { None, Mica, Acrylic, Tabbed }

    public enum ThemeId
    {
        Legacy95,
        LunaXP,
        Aero7,
        ModernLight,
        ModernDark,
        Fluent11Light,
        Fluent11Dark
    }

    public class AppTheme
    {
        private Color _formBackground = Color.FromArgb(220, 243, 243, 243);
        private Color _toolbarBackground = Color.FromArgb(220, 243, 243, 243);
        private Color _filterBarBackground = Color.FromArgb(220, 243, 243, 243);
        private Color _gridBackground = Color.FromArgb(220, 243, 243, 243);

        public ThemeId Id { get; set; }
        public string DisplayName { get; set; } = "";

        public Color TitleBarColor { get; set; }
        public Color TitleBarGradientEnd { get; set; }
        public bool TitleBarHasGradient { get; set; }
        public Color TitleBarTextColor { get; set; }
        public Font TitleBarFont { get; set; } = SystemFonts.CaptionFont;
        public int TitleBarHeight { get; set; } = 30;
        public Color TitleButtonHoverBg { get; set; }
        public Color TitleButtonCloseBg { get; set; } = Color.FromArgb(196, 43, 28);
        public bool TitleButtonsIsOldStyle { get; set; } = false;

        public Color FormBackground
        {
            get => _formBackground;
            set => _formBackground = value.IsEmpty
                ? Color.FromArgb(220, 243, 243, 243)
                : ColorHelper.EnsureMinAlpha(value, 30);
        }

        public Color ToolbarBackground
        {
            get => _toolbarBackground;
            set => _toolbarBackground = value.IsEmpty
                ? FormBackground
                : ColorHelper.EnsureMinAlpha(value, 30);
        }

        public Color FilterBarBackground
        {
            get => _filterBarBackground;
            set => _filterBarBackground = value.IsEmpty
                ? FormBackground
                : ColorHelper.EnsureMinAlpha(value, 30);
        }

        public Color GridBackground
        {
            get => _gridBackground;
            set => _gridBackground = value.IsEmpty
                ? FormBackground
                : ColorHelper.EnsureMinAlpha(value, 30);
        }

        public Color BackgroundColor => GridBackground;

        public Color GridAlternateRow { get; set; }
        public Color GridSelectionBackground { get; set; }
        public Color GridSelectionForeground { get; set; }
        public Color GridHeaderBackground { get; set; }
        public Color GridHeaderForeground { get; set; }

        public Color TextColor { get; set; }
        public Color TextMutedColor { get; set; }
        public Font BaseFont { get; set; } = new Font("Segoe UI", 9f);

        public Color BorderColor { get; set; }
        public int BorderWidth { get; set; } = 1;
        public bool HasOuterBorder3D { get; set; } = false;

        public Color ButtonBackground { get; set; }
        public Color ButtonForeground { get; set; }
        public Color ButtonBorder { get; set; }
        public Color ButtonHover { get; set; }
        public bool ButtonHas3DRelief { get; set; } = false;

        public Color InputBackground { get; set; }
        public Color InputForeground { get; set; }
        public Color InputBorder { get; set; }

        public Color AccentColor { get; set; }
        public Color SeparatorColor { get; set; }

        public BackdropEffect Backdrop { get; init; } = BackdropEffect.None;
        public Color BackdropFallbackTint { get; init; } = Color.FromArgb(200, 32, 32, 32);

        public bool IsDark => Id is ThemeId.ModernDark or ThemeId.Fluent11Dark;
        public bool HasNativeTitleBar => Id is ThemeId.Fluent11Light or ThemeId.Fluent11Dark;
        public bool RequiresTransparentControls => Backdrop != BackdropEffect.None;
    }
}
