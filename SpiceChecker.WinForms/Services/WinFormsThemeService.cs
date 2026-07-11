using System.Drawing;
using SpiceChecker.Application.Services;

namespace SpiceChecker.WinForms.Services;

/// <summary>
/// Service de thèmes WinForms : palette complète (fond, texte, accent, boutons) par thème,
/// avec prise en charge des arrière-plans Fluent / Mica via DWM.
/// </summary>
public sealed class WinFormsThemeService : IThemeService
{
    private static readonly string[] Themes = ["Legacy95", "LunaXP", "Aero7", "Fluent11", "Mica"];

    private sealed record Palette(
        Color Back,
        Color Fore,
        Color Accent,
        Color AccentFore,
        Color ButtonBack,
        Color ButtonFore,
        Color InputBack,
        Color InputFore,
        FlatStyle ButtonStyle,
        bool UseBackdrop,
        bool DarkBackdrop);

    /// <inheritdoc />
    public string CurrentTheme { get; private set; } = "Fluent11";

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableThemes() => Themes;

    /// <inheritdoc />
    public void ApplyTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
        {
            return;
        }

        CurrentTheme = themeName;
        foreach (Form form in System.Windows.Forms.Application.OpenForms)
        {
            ApplyThemeToForm(form, themeName);
        }
    }

    private static Palette GetPalette(string themeName) => themeName switch
    {
        "Legacy95" => new Palette(
            Back: Color.FromArgb(192, 192, 192),
            Fore: Color.Black,
            Accent: Color.FromArgb(0, 0, 128),
            AccentFore: Color.White,
            ButtonBack: Color.FromArgb(192, 192, 192),
            ButtonFore: Color.Black,
            InputBack: Color.White,
            InputFore: Color.Black,
            ButtonStyle: FlatStyle.Standard,
            UseBackdrop: false,
            DarkBackdrop: false),

        "LunaXP" => new Palette(
            Back: Color.FromArgb(236, 233, 216),
            Fore: Color.Black,
            Accent: Color.FromArgb(49, 106, 197),
            AccentFore: Color.White,
            ButtonBack: Color.FromArgb(236, 233, 216),
            ButtonFore: Color.Black,
            InputBack: Color.White,
            InputFore: Color.Black,
            ButtonStyle: FlatStyle.Standard,
            UseBackdrop: false,
            DarkBackdrop: false),

        "Aero7" => new Palette(
            Back: Color.FromArgb(220, 230, 241),
            Fore: Color.Black,
            Accent: Color.FromArgb(74, 144, 217),
            AccentFore: Color.White,
            ButtonBack: Color.FromArgb(240, 244, 249),
            ButtonFore: Color.Black,
            InputBack: Color.White,
            InputFore: Color.Black,
            ButtonStyle: FlatStyle.Flat,
            UseBackdrop: false,
            DarkBackdrop: false),

        "Fluent11" => new Palette(
            Back: Color.FromArgb(32, 32, 32),
            Fore: Color.WhiteSmoke,
            Accent: Color.FromArgb(0, 120, 212),
            AccentFore: Color.White,
            ButtonBack: Color.FromArgb(55, 55, 55),
            ButtonFore: Color.WhiteSmoke,
            InputBack: Color.FromArgb(43, 43, 43),
            InputFore: Color.WhiteSmoke,
            ButtonStyle: FlatStyle.Flat,
            UseBackdrop: true,
            DarkBackdrop: true),

        _ => new Palette( // Mica (clair)
            Back: Color.FromArgb(243, 243, 243),
            Fore: Color.Black,
            Accent: Color.FromArgb(0, 95, 184),
            AccentFore: Color.White,
            ButtonBack: Color.White,
            ButtonFore: Color.Black,
            InputBack: Color.White,
            InputFore: Color.Black,
            ButtonStyle: FlatStyle.Flat,
            UseBackdrop: true,
            DarkBackdrop: false)
    };

    private static void ApplyThemeToForm(Form form, string themeName)
    {
        var palette = GetPalette(themeName);

        if (palette.UseBackdrop)
        {
            DwmHelper.ApplyFluentOrMica(form.Handle, palette.DarkBackdrop);
        }
        else
        {
            DwmHelper.DisableBackdrop(form.Handle);
        }

        form.BackColor = palette.Back;
        form.ForeColor = palette.Fore;
        ApplyControlPalette(form.Controls, palette);
    }

    private static void ApplyControlPalette(Control.ControlCollection controls, Palette palette)
    {
        foreach (Control control in controls)
        {
            switch (control)
            {
                // La grille garde son propre style (zone de données à fond clair,
                // teintes de sévérité gérées par la vue).
                case DataGridView:
                    continue;

                case Button button:
                    ApplyButtonPalette(button, palette);
                    continue;

                case TextBox or ComboBox:
                    control.BackColor = palette.InputBack;
                    control.ForeColor = palette.InputFore;
                    continue;

                case StatusStrip statusStrip:
                    statusStrip.BackColor = palette.Back;
                    statusStrip.ForeColor = palette.Fore;
                    foreach (ToolStripItem item in statusStrip.Items)
                    {
                        if (item.ForeColor != Color.Firebrick
                            && item.ForeColor != Color.DarkOrange
                            && item.ForeColor != Color.SteelBlue
                            && item.ForeColor != Color.SeaGreen)
                        {
                            item.ForeColor = palette.Fore;
                        }
                    }

                    continue;
            }

            control.BackColor = palette.Back;
            control.ForeColor = palette.Fore;

            if (control.HasChildren)
            {
                ApplyControlPalette(control.Controls, palette);
            }
        }
    }

    private static void ApplyButtonPalette(Button button, Palette palette)
    {
        var isPrimary = Equals(button.Tag, "primary");

        button.FlatStyle = palette.ButtonStyle;
        button.UseVisualStyleBackColor = palette.ButtonStyle == FlatStyle.Standard && !isPrimary;

        if (isPrimary)
        {
            button.BackColor = palette.Accent;
            button.ForeColor = palette.AccentFore;
        }
        else
        {
            button.BackColor = palette.ButtonBack;
            button.ForeColor = palette.ButtonFore;
        }

        if (palette.ButtonStyle == FlatStyle.Flat)
        {
            button.FlatAppearance.BorderSize = isPrimary ? 0 : 1;
            button.FlatAppearance.BorderColor = ControlPaint.Dark(palette.ButtonBack, 0.1f);
            button.FlatAppearance.MouseOverBackColor = isPrimary
                ? ControlPaint.Light(palette.Accent, 0.2f)
                : ControlPaint.Dark(palette.ButtonBack, 0.05f);
        }
    }
}
