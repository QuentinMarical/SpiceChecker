using System.Drawing;
using SpiceChecker.Application.Services;

namespace SpiceChecker.WinForms.Services;

/// <summary>
/// Service de thèmes WinForms avec prise en charge simplifiée de thèmes legacy et Fluent.
/// </summary>
public sealed class WinFormsThemeService : IThemeService
{
    private static readonly string[] Themes = ["Legacy95", "LunaXP", "Aero7", "Fluent11", "Mica"];

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

    private static void ApplyThemeToForm(Form form, string themeName)
    {
        if (themeName is "Fluent11" or "Mica")
        {
            var dark = string.Equals(themeName, "Fluent11", StringComparison.OrdinalIgnoreCase);
            DwmHelper.ApplyFluentOrMica(form.Handle, dark);
            form.BackColor = dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
            form.ForeColor = dark ? Color.WhiteSmoke : Color.Black;
            ApplyControlPalette(form.Controls, form.BackColor, form.ForeColor);
            return;
        }

        DwmHelper.DisableBackdrop(form.Handle);

        var (backColor, foreColor) = themeName switch
        {
            "Legacy95" => (Color.FromArgb(192, 192, 192), Color.Black),
            "LunaXP" => (Color.FromArgb(190, 210, 240), Color.Black),
            "Aero7" => (Color.FromArgb(220, 230, 241), Color.Black),
            _ => (SystemColors.Control, SystemColors.ControlText)
        };

        form.BackColor = backColor;
        form.ForeColor = foreColor;
        ApplyControlPalette(form.Controls, backColor, foreColor);
    }

    private static void ApplyControlPalette(Control.ControlCollection controls, Color backColor, Color foreColor)
    {
        foreach (Control control in controls)
        {
            if (control is DataGridView)
            {
                continue;
            }

            control.BackColor = backColor;
            control.ForeColor = foreColor;

            if (control.HasChildren)
            {
                ApplyControlPalette(control.Controls, backColor, foreColor);
            }
        }
    }
}
