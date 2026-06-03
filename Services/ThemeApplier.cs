using System.Drawing;
using System.Windows.Forms;
using SpiceChecker.Models;
using ThemeDefinition = SpiceChecker.Models.AppTheme;

namespace SpiceChecker.Services
{
    public static class ThemeApplier
    {
        public static void Apply(Form form, ThemeDefinition theme, DataGridView grid,
            Panel toolbarPanel, Panel filterPanel, ToolStrip? toolStrip = null)
        {
            bool hasBackdrop = theme.Backdrop != BackdropEffect.None;

            // BackColor de base ou semi-transparent pour Mica/Acrylic
            if (hasBackdrop)
            {
                form.BackColor = theme.IsDark
                    ? Color.FromArgb(32, 32, 32)
                    : Color.FromArgb(243, 243, 243);
            }
            else
            {
                form.BackColor = theme.FormBackground;
            }

            form.Font = theme.BaseFont;
            bool isFluent = theme.Id is ThemeId.Fluent11Light or ThemeId.Fluent11Dark;
            if (!isFluent)
                form.FormBorderStyle = FormBorderStyle.None;

            // Transparence des panels si backdrop actif
            if (hasBackdrop)
            {
                MakeTransparent(toolbarPanel);
                MakeTransparent(filterPanel);
                if (toolStrip != null)
                {
                    toolStrip.BackColor = Color.Transparent;
                    toolStrip.Renderer = new TransparentToolStripRenderer(theme);
                }
            }
            else
            {
                toolbarPanel.BackColor = theme.ToolbarBackground;
                filterPanel.BackColor = theme.FilterBarBackground;
                if (toolStrip != null)
                {
                    toolStrip.BackColor = theme.ToolbarBackground;
                    toolStrip.ForeColor = theme.TextColor;
                    toolStrip.Renderer = new ThemedToolStripRenderer(theme);
                }
            }

            if (toolStrip != null)
            {
                toolStrip.ForeColor = theme.TextColor;
            }

            ApplyToControls(toolbarPanel, theme);
            ApplyToControls(filterPanel, theme);

            grid.BackgroundColor = theme.GridBackground;
            grid.DefaultCellStyle.BackColor = theme.GridBackground;
            grid.DefaultCellStyle.ForeColor = theme.TextColor;
            grid.DefaultCellStyle.SelectionBackColor = theme.GridSelectionBackground;
            grid.DefaultCellStyle.SelectionForeColor = theme.GridSelectionForeground;
            grid.DefaultCellStyle.Font = theme.BaseFont;
            grid.AlternatingRowsDefaultCellStyle.BackColor = theme.GridAlternateRow;
            grid.ColumnHeadersDefaultCellStyle.BackColor = theme.GridHeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.GridHeaderForeground;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(theme.BaseFont, FontStyle.Bold);
            grid.GridColor = theme.BorderColor;
            grid.BorderStyle = theme.HasOuterBorder3D ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;

            // ── Effet DWM backdrop ────────────────────────────────────────────
            // DWM compose l'effet derrière la fenêtre ; GDI peint normalement par-dessus.
            // On ne touche pas BackColor avec Color.Black ni TransparencyKey.
            if (form.IsHandleCreated)
            {
                DwmHelper.SetTitleBarDarkMode(form.Handle, theme.IsDark);
                DwmHelper.ApplyBestEffect(form.Handle, theme.Backdrop, theme.BackdropFallbackTint);
            }
            else
            {
                form.HandleCreated += (s, e) =>
                {
                    DwmHelper.SetTitleBarDarkMode(form.Handle, theme.IsDark);
                    DwmHelper.ApplyBestEffect(form.Handle, theme.Backdrop, theme.BackdropFallbackTint);
                };
            }
        }

        private static void MakeTransparent(Panel panel)
        {
            panel.BackColor = Color.Transparent;
            var doubleBufferProperty = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            doubleBufferProperty?.SetValue(panel, true, null);
        }

        private static void ApplyToControls(Control parent, ThemeDefinition theme)
        {
            bool hasBackdrop = theme.Backdrop != BackdropEffect.None;

            foreach (Control ctrl in parent.Controls)
            {
                ctrl.Font = theme.BaseFont;
                ctrl.ForeColor = theme.TextColor;

                if (ctrl is Button btn)
                {
                    btn.BackColor = theme.ButtonBackground;
                    btn.ForeColor = theme.ButtonForeground;
                    btn.FlatStyle = theme.ButtonHas3DRelief ? FlatStyle.Standard : FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = theme.ButtonBorder;
                    btn.FlatAppearance.MouseOverBackColor = theme.ButtonHover;
                }
                else if (ctrl is ComboBox cmb)
                {
                    cmb.BackColor = theme.InputBackground;
                    cmb.ForeColor = theme.InputForeground;
                }
                else if (ctrl is TextBox txt)
                {
                    txt.BackColor = theme.InputBackground;
                    txt.ForeColor = theme.InputForeground;
                    txt.BorderStyle = theme.HasOuterBorder3D ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
                }
                else if (ctrl is Label lbl)
                {
                    lbl.ForeColor = theme.TextColor;
                    if (hasBackdrop) lbl.BackColor = Color.Transparent;
                }
                else if (ctrl is CheckBox chk)
                {
                    chk.ForeColor = theme.TextColor;
                    if (hasBackdrop) chk.BackColor = Color.Transparent;
                }
                else if (ctrl.HasChildren)
                {
                    ApplyToControls(ctrl, theme);
                }
            }
        }
    }

    internal class ThemedToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemeDefinition _theme;

        public ThemedToolStripRenderer(ThemeDefinition theme)
        {
            _theme = theme;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(_theme.ToolbarBackground);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            if (item.Selected || item.Pressed)
            {
                using var brush = new SolidBrush(_theme.ButtonHover);
                e.Graphics.FillRectangle(brush, new Rectangle(0, 0, item.Width, item.Height));
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(_theme.SeparatorColor);
            int x = e.Item.Width / 2;
            e.Graphics.DrawLine(pen, x, 4, x, e.Item.Height - 4);
        }
    }

    internal class TransparentToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemeDefinition _theme;

        public TransparentToolStripRenderer(ThemeDefinition theme)
        {
            _theme = theme;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // Ne rien dessiner = transparent
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            if (item.Selected || item.Pressed)
            {
                // Overlay semi-transparent pour hover
                using var brush = new SolidBrush(Color.FromArgb(40, _theme.ButtonHover));
                e.Graphics.FillRectangle(brush, new Rectangle(0, 0, item.Width, item.Height));
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(100, _theme.SeparatorColor));
            int x = e.Item.Width / 2;
            e.Graphics.DrawLine(pen, x, 4, x, e.Item.Height - 4);
        }
    }
}
