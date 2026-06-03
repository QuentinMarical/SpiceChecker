using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SpiceChecker;

namespace SpiceChecker.Services
{
    public enum AppTheme
    {
        Legacy95,
        LunaXP,
        AeroSeven,
        ModernLight,
        ModernDark,
        FluentLight,
        FluentDark
    }

    public static class ThemeManager
    {
        private const string AeroGlassPanelName = "__aeroGlassPanel";

        public static AppTheme Current { get; private set; } = AppTheme.FluentLight;

        public static event EventHandler? ThemeChanged;

        internal readonly record struct ThemeColors(
            Color FormBack,
            Color TitleBarBack,
            Color TitleBarFore,
            Color ButtonFace,
            Color ButtonHighlight,
            Color ButtonShadow,
            Color ButtonDarkShadow,
            Color WindowBack,
            Color WindowFore,
            Color GridHeaderBack,
            Color GridHeaderFore,
            Color GridBack,
            Color GridAlternate,
            Color GridFore,
            Color GridLine,
            Color ToolbarBack,
            Color ToolbarFore,
            Color ToolbarBorder,
            Color StatusBack,
            Color StatusFore,
            Color StatusBorder,
            Color SelectionBack,
            Color SelectionFore,
            string FontName,
            float FontSize,
            int HeaderHeight,
            int RowHeight,
            FormBorderStyle BorderStyle,
            Padding FormPadding,
            bool UseFakeTitleBarPanel,
            int FakeTitleBarHeight,
            bool Use3DBorders,
            FlatStyle ButtonFlatStyle,
            int ButtonBorderSize,
            bool UseSystemToolStripRenderer,
            DataGridViewCellBorderStyle GridCellBorderStyle,
            bool DarkInputs,
            bool FluentPadding,
            bool LegacyButtonSystemStyle);

        public static AppTheme ParseThemeName(string name)
        {
            return name switch
            {
                nameof(AppTheme.Legacy95) => AppTheme.Legacy95,
                nameof(AppTheme.LunaXP) => AppTheme.LunaXP,
                nameof(AppTheme.AeroSeven) => AppTheme.AeroSeven,
                nameof(AppTheme.ModernLight) => AppTheme.ModernLight,
                nameof(AppTheme.ModernDark) => AppTheme.ModernDark,
                nameof(AppTheme.FluentLight) => AppTheme.FluentLight,
                nameof(AppTheme.FluentDark) => AppTheme.FluentDark,
                _ => AppTheme.FluentLight
            };
        }

        public static void Apply(AppTheme theme, Form form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var colors = GetThemeColors(theme);
            Current = theme;

            if (theme == AppTheme.Legacy95)
            {
                TrySetLegacyTextRendering();
            }

            form.SuspendLayout();

            form.BackColor = colors.FormBack;
            form.ForeColor = colors.WindowFore;
            form.Font = new Font(colors.FontName, colors.FontSize, FontStyle.Regular);
            form.FormBorderStyle = colors.BorderStyle;
            form.Padding = colors.FormPadding;

            ApplyTitleBarPanel(form, theme, colors);

            foreach (Control control in form.Controls)
            {
                ApplyToControl(control, colors);
            }

            form.ResumeLayout(true);
            form.Refresh();

            ThemeChanged?.Invoke(form, EventArgs.Empty);
        }

        private static void ApplyToControl(Control c, ThemeColors colors)
        {
            c.Font = new Font(colors.FontName, colors.FontSize, c.Font.Style);

            if (c is DataGridView grid)
            {
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersHeight = colors.HeaderHeight;
                grid.RowTemplate.Height = colors.RowHeight;
                grid.GridColor = colors.GridLine;
                grid.DefaultCellStyle.SelectionBackColor = colors.SelectionBack;
                grid.DefaultCellStyle.SelectionForeColor = colors.SelectionFore;
                grid.ColumnHeadersDefaultCellStyle.BackColor = colors.GridHeaderBack;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = colors.GridHeaderFore;
                grid.ColumnHeadersDefaultCellStyle.Font = new Font(colors.FontName, colors.FontSize, FontStyle.Bold);
                grid.ColumnHeadersDefaultCellStyle.Padding = colors.FluentPadding ? new Padding(4, 2, 4, 2) : Padding.Empty;
                grid.AlternatingRowsDefaultCellStyle.BackColor = colors.GridAlternate;
                grid.AlternatingRowsDefaultCellStyle.ForeColor = colors.GridFore;
                grid.DefaultCellStyle.BackColor = colors.GridBack;
                grid.DefaultCellStyle.ForeColor = colors.GridFore;
                grid.DefaultCellStyle.Font = new Font(colors.FontName, colors.FontSize, FontStyle.Regular);
                grid.BackgroundColor = colors.GridBack;
                grid.BorderStyle = BorderStyle.None;
                grid.CellBorderStyle = colors.GridCellBorderStyle;
            }
            else if (c is StatusStrip status)
            {
                status.BackColor = colors.StatusBack;
                status.ForeColor = colors.StatusFore;
                status.SizingGrip = Current == AppTheme.Legacy95;
                status.RenderMode = ToolStripRenderMode.Professional;
                status.Renderer = new ToolStripProfessionalRenderer(new NeutralColorTable(colors));
            }
            else if (c is ToolStrip strip)
            {
                strip.BackColor = colors.ToolbarBack;
                strip.ForeColor = colors.ToolbarFore;
                strip.Font = new Font(colors.FontName, colors.FontSize, FontStyle.Regular);
                strip.Renderer = colors.UseSystemToolStripRenderer
                    ? new ToolStripSystemRenderer()
                    : (Current == AppTheme.LunaXP
                        ? new ToolStripProfessionalRenderer(new XpColorTable())
                        : new ToolStripProfessionalRenderer(new NeutralColorTable(colors)));
            }
            else if (c is Button button)
            {
                ApplyToButton(button, colors);
            }
            else if (c is TextBoxBase textBox)
            {
                if (colors.Use3DBorders)
                {
                    textBox.BorderStyle = BorderStyle.Fixed3D;
                }
                else
                {
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }

                textBox.BackColor = colors.DarkInputs ? Color.FromArgb(43, 43, 43) : colors.WindowBack;
                textBox.ForeColor = colors.DarkInputs ? Color.White : colors.WindowFore;
            }
            else if (c is ComboBox comboBox)
            {
                comboBox.FlatStyle = colors.Use3DBorders ? FlatStyle.Standard : FlatStyle.Flat;
                comboBox.BackColor = colors.DarkInputs ? Color.FromArgb(43, 43, 43) : colors.WindowBack;
                comboBox.ForeColor = colors.DarkInputs ? Color.White : colors.WindowFore;
            }
            else if (c is CheckBox checkBox)
            {
                checkBox.FlatStyle = colors.Use3DBorders ? FlatStyle.Standard : FlatStyle.Flat;
                checkBox.BackColor = colors.DarkInputs ? Color.FromArgb(43, 43, 43) : colors.FormBack;
                checkBox.ForeColor = colors.DarkInputs ? Color.White : colors.WindowFore;
            }
            else if (c is Label)
            {
                c.BackColor = colors.FormBack;
                c.ForeColor = colors.DarkInputs ? Color.White : colors.WindowFore;
            }
            else if (c is Panel || c is TableLayoutPanel || c is GroupBox)
            {
                c.BackColor = colors.FormBack;
                c.ForeColor = colors.WindowFore;
            }
            else
            {
                c.BackColor = colors.FormBack;
                c.ForeColor = colors.WindowFore;
            }

            if (c is MainForm mainForm)
            {
                ConfigureTitleBarPanel(mainForm, colors);
            }

            foreach (Control child in c.Controls)
            {
                ApplyToControl(child, colors);
            }
        }

        private static ThemeColors GetThemeColors(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Legacy95 => new ThemeColors(
                    FormBack: Color.FromArgb(192, 192, 192),
                    TitleBarBack: Color.FromArgb(0, 0, 128),
                    TitleBarFore: Color.FromArgb(255, 255, 255),
                    ButtonFace: Color.FromArgb(192, 192, 192),
                    ButtonHighlight: Color.FromArgb(255, 255, 255),
                    ButtonShadow: Color.FromArgb(128, 128, 128),
                    ButtonDarkShadow: Color.FromArgb(0, 0, 0),
                    WindowBack: Color.FromArgb(255, 255, 255),
                    WindowFore: Color.FromArgb(0, 0, 0),
                    GridHeaderBack: Color.FromArgb(192, 192, 192),
                    GridHeaderFore: Color.FromArgb(0, 0, 0),
                    GridBack: Color.FromArgb(255, 255, 255),
                    GridAlternate: Color.FromArgb(255, 255, 255),
                    GridFore: Color.FromArgb(0, 0, 0),
                    GridLine: Color.FromArgb(128, 128, 128),
                    ToolbarBack: Color.FromArgb(192, 192, 192),
                    ToolbarFore: Color.FromArgb(0, 0, 0),
                    ToolbarBorder: Color.FromArgb(128, 128, 128),
                    StatusBack: Color.FromArgb(192, 192, 192),
                    StatusFore: Color.FromArgb(0, 0, 0),
                    StatusBorder: Color.FromArgb(128, 128, 128),
                    SelectionBack: Color.FromArgb(0, 0, 128),
                    SelectionFore: Color.FromArgb(255, 255, 255),
                    FontName: GetLegacyFontName(),
                    FontSize: 8f,
                    HeaderHeight: 22,
                    RowHeight: 18,
                    BorderStyle: FormBorderStyle.Fixed3D,
                    FormPadding: new Padding(0),
                    UseFakeTitleBarPanel: false,
                    FakeTitleBarHeight: 0,
                    Use3DBorders: true,
                    ButtonFlatStyle: FlatStyle.Standard,
                    ButtonBorderSize: 1,
                    UseSystemToolStripRenderer: true,
                    GridCellBorderStyle: DataGridViewCellBorderStyle.Single,
                    DarkInputs: false,
                    FluentPadding: false,
                    LegacyButtonSystemStyle: true),

                AppTheme.LunaXP => new ThemeColors(
                    FormBack: Color.FromArgb(236, 233, 216),
                    TitleBarBack: Color.FromArgb(10, 36, 106),
                    TitleBarFore: Color.FromArgb(255, 255, 255),
                    ButtonFace: Color.FromArgb(236, 233, 216),
                    ButtonHighlight: Color.FromArgb(255, 255, 255),
                    ButtonShadow: Color.FromArgb(172, 168, 153),
                    ButtonDarkShadow: Color.FromArgb(49, 106, 197),
                    WindowBack: Color.FromArgb(255, 255, 255),
                    WindowFore: Color.FromArgb(0, 0, 0),
                    GridHeaderBack: Color.FromArgb(212, 208, 200),
                    GridHeaderFore: Color.FromArgb(0, 0, 0),
                    GridBack: Color.FromArgb(255, 255, 255),
                    GridAlternate: Color.FromArgb(240, 240, 240),
                    GridFore: Color.FromArgb(0, 0, 0),
                    GridLine: Color.FromArgb(172, 168, 153),
                    ToolbarBack: Color.FromArgb(236, 233, 216),
                    ToolbarFore: Color.FromArgb(0, 0, 0),
                    ToolbarBorder: Color.FromArgb(172, 168, 153),
                    StatusBack: Color.FromArgb(236, 233, 216),
                    StatusFore: Color.FromArgb(0, 0, 0),
                    StatusBorder: Color.FromArgb(172, 168, 153),
                    SelectionBack: Color.FromArgb(49, 106, 197),
                    SelectionFore: Color.FromArgb(255, 255, 255),
                    FontName: "Tahoma",
                    FontSize: 8f,
                    HeaderHeight: 23,
                    RowHeight: 19,
                    BorderStyle: FormBorderStyle.FixedSingle,
                    FormPadding: new Padding(0),
                    UseFakeTitleBarPanel: false,
                    FakeTitleBarHeight: 0,
                    Use3DBorders: false,
                    ButtonFlatStyle: FlatStyle.Standard,
                    ButtonBorderSize: 1,
                    UseSystemToolStripRenderer: false,
                    GridCellBorderStyle: DataGridViewCellBorderStyle.SingleHorizontal,
                    DarkInputs: false,
                    FluentPadding: false,
                    LegacyButtonSystemStyle: false),

                AppTheme.AeroSeven => new ThemeColors(
                    FormBack: Color.FromArgb(240, 240, 240),
                    TitleBarBack: Color.FromArgb(191, 214, 246),
                    TitleBarFore: Color.FromArgb(0, 0, 0),
                    ButtonFace: Color.FromArgb(245, 245, 245),
                    ButtonHighlight: Color.FromArgb(255, 255, 255),
                    ButtonShadow: Color.FromArgb(216, 216, 216),
                    ButtonDarkShadow: Color.FromArgb(74, 144, 217),
                    WindowBack: Color.FromArgb(255, 255, 255),
                    WindowFore: Color.FromArgb(0, 0, 0),
                    GridHeaderBack: Color.FromArgb(227, 236, 243),
                    GridHeaderFore: Color.FromArgb(21, 66, 139),
                    GridBack: Color.FromArgb(255, 255, 255),
                    GridAlternate: Color.FromArgb(245, 249, 255),
                    GridFore: Color.FromArgb(0, 0, 0),
                    GridLine: Color.FromArgb(224, 232, 240),
                    ToolbarBack: Color.FromArgb(245, 245, 245),
                    ToolbarFore: Color.FromArgb(0, 0, 0),
                    ToolbarBorder: Color.FromArgb(216, 216, 216),
                    StatusBack: Color.FromArgb(245, 245, 245),
                    StatusFore: Color.FromArgb(0, 0, 0),
                    StatusBorder: Color.FromArgb(208, 208, 208),
                    SelectionBack: Color.FromArgb(51, 153, 255),
                    SelectionFore: Color.FromArgb(255, 255, 255),
                    FontName: "Segoe UI",
                    FontSize: 9f,
                    HeaderHeight: 24,
                    RowHeight: 20,
                    BorderStyle: FormBorderStyle.FixedSingle,
                    FormPadding: new Padding(0, 0, 0, 0),
                    UseFakeTitleBarPanel: true,
                    FakeTitleBarHeight: 28,
                    Use3DBorders: false,
                    ButtonFlatStyle: FlatStyle.Standard,
                    ButtonBorderSize: 1,
                    UseSystemToolStripRenderer: false,
                    GridCellBorderStyle: DataGridViewCellBorderStyle.SingleHorizontal,
                    DarkInputs: false,
                    FluentPadding: false,
                    LegacyButtonSystemStyle: false),

                AppTheme.ModernLight => new ThemeColors(
                    FormBack: Color.FromArgb(255, 255, 255),
                    TitleBarBack: Color.FromArgb(255, 255, 255),
                    TitleBarFore: Color.FromArgb(0, 0, 0),
                    ButtonFace: Color.FromArgb(225, 225, 225),
                    ButtonHighlight: Color.FromArgb(255, 255, 255),
                    ButtonShadow: Color.FromArgb(224, 224, 224),
                    ButtonDarkShadow: Color.FromArgb(0, 90, 158),
                    WindowBack: Color.FromArgb(255, 255, 255),
                    WindowFore: Color.FromArgb(0, 0, 0),
                    GridHeaderBack: Color.FromArgb(0, 120, 215),
                    GridHeaderFore: Color.FromArgb(255, 255, 255),
                    GridBack: Color.FromArgb(255, 255, 255),
                    GridAlternate: Color.FromArgb(249, 249, 249),
                    GridFore: Color.FromArgb(0, 0, 0),
                    GridLine: Color.FromArgb(237, 237, 237),
                    ToolbarBack: Color.FromArgb(243, 243, 243),
                    ToolbarFore: Color.FromArgb(0, 0, 0),
                    ToolbarBorder: Color.FromArgb(224, 224, 224),
                    StatusBack: Color.FromArgb(243, 243, 243),
                    StatusFore: Color.FromArgb(0, 0, 0),
                    StatusBorder: Color.FromArgb(224, 224, 224),
                    SelectionBack: Color.FromArgb(204, 228, 247),
                    SelectionFore: Color.FromArgb(0, 0, 0),
                    FontName: "Segoe UI",
                    FontSize: 9f,
                    HeaderHeight: 25,
                    RowHeight: 22,
                    BorderStyle: FormBorderStyle.FixedSingle,
                    FormPadding: new Padding(0),
                    UseFakeTitleBarPanel: false,
                    FakeTitleBarHeight: 0,
                    Use3DBorders: false,
                    ButtonFlatStyle: FlatStyle.Flat,
                    ButtonBorderSize: 0,
                    UseSystemToolStripRenderer: false,
                    GridCellBorderStyle: DataGridViewCellBorderStyle.SingleHorizontal,
                    DarkInputs: false,
                    FluentPadding: false,
                    LegacyButtonSystemStyle: false),

                AppTheme.ModernDark => new ThemeColors(
                    FormBack: Color.FromArgb(32, 32, 32),
                    TitleBarBack: Color.FromArgb(32, 32, 32),
                    TitleBarFore: Color.FromArgb(255, 255, 255),
                    ButtonFace: Color.FromArgb(58, 58, 58),
                    ButtonHighlight: Color.FromArgb(90, 90, 90),
                    ButtonShadow: Color.FromArgb(58, 58, 58),
                    ButtonDarkShadow: Color.FromArgb(0, 90, 158),
                    WindowBack: Color.FromArgb(43, 43, 43),
                    WindowFore: Color.FromArgb(255, 255, 255),
                    GridHeaderBack: Color.FromArgb(0, 120, 215),
                    GridHeaderFore: Color.FromArgb(255, 255, 255),
                    GridBack: Color.FromArgb(43, 43, 43),
                    GridAlternate: Color.FromArgb(38, 38, 38),
                    GridFore: Color.FromArgb(255, 255, 255),
                    GridLine: Color.FromArgb(58, 58, 58),
                    ToolbarBack: Color.FromArgb(43, 43, 43),
                    ToolbarFore: Color.FromArgb(255, 255, 255),
                    ToolbarBorder: Color.FromArgb(58, 58, 58),
                    StatusBack: Color.FromArgb(26, 26, 26),
                    StatusFore: Color.FromArgb(204, 204, 204),
                    StatusBorder: Color.FromArgb(58, 58, 58),
                    SelectionBack: Color.FromArgb(0, 90, 158),
                    SelectionFore: Color.FromArgb(255, 255, 255),
                    FontName: "Segoe UI",
                    FontSize: 9f,
                    HeaderHeight: 25,
                    RowHeight: 22,
                    BorderStyle: FormBorderStyle.FixedSingle,
                    FormPadding: new Padding(0),
                    UseFakeTitleBarPanel: false,
                    FakeTitleBarHeight: 0,
                    Use3DBorders: false,
                    ButtonFlatStyle: FlatStyle.Flat,
                    ButtonBorderSize: 0,
                    UseSystemToolStripRenderer: false,
                    GridCellBorderStyle: DataGridViewCellBorderStyle.SingleHorizontal,
                    DarkInputs: true,
                    FluentPadding: false,
                    LegacyButtonSystemStyle: false),

                AppTheme.FluentDark => new ThemeColors(
                    FormBack: Color.FromArgb(32, 32, 32),
                    TitleBarBack: Color.FromArgb(32, 32, 32),
                    TitleBarFore: Color.FromArgb(255, 255, 255),
                    ButtonFace: Color.FromArgb(44, 44, 44),
                    ButtonHighlight: Color.FromArgb(61, 61, 61),
                    ButtonShadow: Color.FromArgb(61, 61, 61),
                    ButtonDarkShadow: Color.FromArgb(0, 120, 212),
                    WindowBack: Color.FromArgb(28, 28, 28),
                    WindowFore: Color.FromArgb(255, 255, 255),
                    GridHeaderBack: Color.FromArgb(44, 44, 44),
                    GridHeaderFore: Color.FromArgb(96, 205, 255),
                    GridBack: Color.FromArgb(28, 28, 28),
                    GridAlternate: Color.FromArgb(37, 37, 37),
                    GridFore: Color.FromArgb(255, 255, 255),
                    GridLine: Color.FromArgb(61, 61, 61),
                    ToolbarBack: Color.FromArgb(44, 44, 44),
                    ToolbarFore: Color.FromArgb(255, 255, 255),
                    ToolbarBorder: Color.FromArgb(61, 61, 61),
                    StatusBack: Color.FromArgb(26, 26, 26),
                    StatusFore: Color.FromArgb(170, 170, 170),
                    StatusBorder: Color.FromArgb(61, 61, 61),
                    SelectionBack: Color.FromArgb(0, 120, 212),
                    SelectionFore: Color.FromArgb(255, 255, 255),
                    FontName: "Segoe UI",
                    FontSize: 9f,
                    HeaderHeight: 32,
                    RowHeight: 28,
                    BorderStyle: FormBorderStyle.FixedSingle,
                    FormPadding: new Padding(0, 8, 0, 0),
                    UseFakeTitleBarPanel: true,
                    FakeTitleBarHeight: 32,
                    Use3DBorders: false,
                    ButtonFlatStyle: FlatStyle.Flat,
                    ButtonBorderSize: 0,
                    UseSystemToolStripRenderer: false,
                    GridCellBorderStyle: DataGridViewCellBorderStyle.SingleHorizontal,
                    DarkInputs: true,
                    FluentPadding: true,
                    LegacyButtonSystemStyle: false),

                _ => new ThemeColors(
                    FormBack: Color.FromArgb(243, 243, 243),
                    TitleBarBack: Color.FromArgb(243, 243, 243),
                    TitleBarFore: Color.FromArgb(0, 0, 0),
                    ButtonFace: Color.FromArgb(255, 255, 255),
                    ButtonHighlight: Color.FromArgb(255, 255, 255),
                    ButtonShadow: Color.FromArgb(224, 224, 224),
                    ButtonDarkShadow: Color.FromArgb(0, 62, 146),
                    WindowBack: Color.FromArgb(255, 255, 255),
                    WindowFore: Color.FromArgb(0, 0, 0),
                    GridHeaderBack: Color.FromArgb(0, 103, 192),
                    GridHeaderFore: Color.FromArgb(255, 255, 255),
                    GridBack: Color.FromArgb(255, 255, 255),
                    GridAlternate: Color.FromArgb(249, 249, 249),
                    GridFore: Color.FromArgb(0, 0, 0),
                    GridLine: Color.FromArgb(235, 235, 235),
                    ToolbarBack: Color.FromArgb(236, 236, 236),
                    ToolbarFore: Color.FromArgb(0, 0, 0),
                    ToolbarBorder: Color.FromArgb(224, 224, 224),
                    StatusBack: Color.FromArgb(236, 236, 236),
                    StatusFore: Color.FromArgb(0, 0, 0),
                    StatusBorder: Color.FromArgb(224, 224, 224),
                    SelectionBack: Color.FromArgb(208, 228, 247),
                    SelectionFore: Color.FromArgb(0, 0, 0),
                    FontName: GetFluentLightFontName(),
                    FontSize: 9f,
                    HeaderHeight: 32,
                    RowHeight: 28,
                    BorderStyle: FormBorderStyle.FixedSingle,
                    FormPadding: new Padding(0, 8, 0, 0),
                    UseFakeTitleBarPanel: true,
                    FakeTitleBarHeight: 32,
                    Use3DBorders: false,
                    ButtonFlatStyle: FlatStyle.Flat,
                    ButtonBorderSize: 0,
                    UseSystemToolStripRenderer: false,
                    GridCellBorderStyle: DataGridViewCellBorderStyle.SingleHorizontal,
                    DarkInputs: false,
                    FluentPadding: true,
                    LegacyButtonSystemStyle: false)
            };
        }

        private static string GetLegacyFontName()
        {
            return FontFamily.Families.Any(f => string.Equals(f.Name, "MS Sans Serif", StringComparison.OrdinalIgnoreCase))
                ? "MS Sans Serif"
                : "Microsoft Sans Serif";
        }

        private static string GetFluentLightFontName()
        {
            return FontFamily.Families.Any(f => string.Equals(f.Name, "Segoe UI Variable", StringComparison.OrdinalIgnoreCase))
                ? "Segoe UI Variable"
                : "Segoe UI";
        }

        private static void TrySetLegacyTextRendering()
        {
            try
            {
                Application.SetCompatibleTextRenderingDefault(false);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void ApplyTitleBarPanel(Form form, AppTheme theme, ThemeColors colors)
        {
            if (form is MainForm mainForm)
            {
                ConfigureTitleBarPanel(mainForm, colors);
            }
        }

        private static void ConfigureTitleBarPanel(MainForm mainForm, ThemeColors colors)
        {
            var panel = mainForm.TitleBarPanel;
            if (panel == null)
                return;

            if (!colors.UseFakeTitleBarPanel)
            {
                panel.Visible = false;
                panel.Height = 0;
                return;
            }

            panel.Dock = DockStyle.Top;
            panel.Visible = true;
            panel.Height = colors.FakeTitleBarHeight;
            panel.BackColor = colors.TitleBarBack;
            panel.ForeColor = colors.TitleBarFore;
            panel.BringToFront();
        }

        private static void ApplyToButton(Button button, ThemeColors colors)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = colors.ButtonFlatStyle;
            button.BackColor = colors.ButtonFace;
            button.ForeColor = colors.WindowFore;
            button.FlatAppearance.BorderSize = colors.ButtonBorderSize;
            button.FlatAppearance.BorderColor = colors.ButtonShadow;
            button.FlatAppearance.MouseOverBackColor = colors.ButtonFlatStyle == FlatStyle.Flat ? colors.SelectionBack : colors.ButtonHighlight;
            button.FlatAppearance.MouseDownBackColor = colors.ButtonShadow;
        }

        private sealed class XpColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => Color.FromArgb(236, 233, 216);
            public override Color ToolStripGradientMiddle => Color.FromArgb(236, 233, 216);
            public override Color ToolStripGradientEnd => Color.FromArgb(236, 233, 216);
            public override Color MenuItemSelected => Color.FromArgb(49, 106, 197);
            public override Color MenuBorder => Color.FromArgb(49, 106, 197);
            public override Color ImageMarginGradientBegin => Color.FromArgb(236, 233, 216);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(236, 233, 216);
            public override Color ImageMarginGradientEnd => Color.FromArgb(236, 233, 216);
        }

        private sealed class NeutralColorTable : ProfessionalColorTable
        {
            private readonly ThemeColors _colors;

            public NeutralColorTable(ThemeColors colors)
            {
                _colors = colors;
            }

            public override Color ToolStripGradientBegin => _colors.ToolbarBack;
            public override Color ToolStripGradientMiddle => _colors.ToolbarBack;
            public override Color ToolStripGradientEnd => _colors.ToolbarBack;
            public override Color MenuBorder => _colors.ToolbarBorder;
            public override Color MenuItemBorder => _colors.ToolbarBorder;
            public override Color MenuItemSelected => _colors.SelectionBack;
            public override Color StatusStripGradientBegin => _colors.StatusBack;
            public override Color StatusStripGradientEnd => _colors.StatusBack;
            public override Color SeparatorDark => _colors.StatusBorder;
            public override Color SeparatorLight => _colors.StatusBorder;
        }
    }
}
