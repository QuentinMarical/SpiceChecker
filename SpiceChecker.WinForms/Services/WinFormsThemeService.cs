using System.Drawing;
using SpiceChecker.Application.Services;

namespace SpiceChecker.WinForms.Services;

/// <summary>
/// Thème unique « Aero7 » modernisé : palette bleu Aero claire, boutons plats
/// et arrière-plan Mica (Windows 11) sur la fenêtre.
/// </summary>
public sealed class WinFormsThemeService : IThemeService
{
    public const string ThemeName = "Aero7";

    private static readonly Color Back = Color.FromArgb(237, 243, 250);
    private static readonly Color Fore = Color.FromArgb(23, 32, 42);
    private static readonly Color Accent = Color.FromArgb(46, 124, 214);
    private static readonly Color AccentFore = Color.White;
    private static readonly Color ButtonBack = Color.White;
    private static readonly Color ButtonBorder = Color.FromArgb(198, 214, 233);
    private static readonly Color InputBack = Color.White;

    /// <inheritdoc />
    public string CurrentTheme => ThemeName;

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableThemes() => [ThemeName];

    /// <inheritdoc />
    public void ApplyTheme(string themeName)
    {
        foreach (Form form in System.Windows.Forms.Application.OpenForms)
        {
            ApplyToForm(form);
        }
    }

    private static void ApplyToForm(Form form)
    {
        DwmHelper.ApplyFluentOrMica(form.Handle, dark: false);

        form.BackColor = Back;
        form.ForeColor = Fore;
        ApplyToControls(form.Controls);
    }

    private static void ApplyToControls(Control.ControlCollection controls)
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
                    ApplyToButton(button);
                    continue;

                case TextBox or ComboBox:
                    control.BackColor = InputBack;
                    control.ForeColor = Fore;
                    continue;

                case StatusStrip statusStrip:
                    statusStrip.BackColor = Back;
                    statusStrip.ForeColor = Fore;
                    continue;
            }

            control.BackColor = Back;
            control.ForeColor = Fore;

            if (control.HasChildren)
            {
                ApplyToControls(control.Controls);
            }
        }
    }

    private static void ApplyToButton(Button button)
    {
        var isPrimary = Equals(button.Tag, "primary");

        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;

        if (isPrimary)
        {
            button.BackColor = Accent;
            button.ForeColor = AccentFore;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(Accent, 0.2f);
        }
        else
        {
            button.BackColor = ButtonBack;
            button.ForeColor = Fore;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ButtonBorder;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 236, 248);
        }
    }
}
