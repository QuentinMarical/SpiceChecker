using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SpiceChecker.Models;
using ThemeDefinition = SpiceChecker.Models.AppTheme;

namespace SpiceChecker.Forms
{
    public class CustomTitleBar : UserControl
    {
        private ThemeDefinition _theme = null!;
        private string _title = "Spice Checker";
        private bool _mouseDown;
        private Point _lastCursor;
        private Form? _parentForm;

        private int _hoverBtn;

        public event EventHandler? CloseClicked;
        public event EventHandler? MaximizeClicked;
        public event EventHandler? MinimizeClicked;

        public CustomTitleBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
            Cursor = Cursors.Default;
        }

        public void Initialize(Form parentForm, ThemeDefinition theme, string title)
        {
            _parentForm = parentForm;
            _theme = theme;
            _title = title;
            Height = theme.TitleBarHeight;
            Dock = DockStyle.Top;
            Invalidate();
        }

        public void ApplyTheme(ThemeDefinition theme)
        {
            _theme = theme;
            Height = theme.TitleBarHeight;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Skip rendering if the theme uses native title bar
            if (_theme.HasNativeTitleBar) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            switch (_theme.Id)
            {
                case ThemeId.Aero7:
                {
                    // Couche 1 : fond bleu-gris glacé (simule le reflet du bureau)
                    using (var bgBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, w, h),
                        Color.FromArgb(168, 191, 220),
                        Color.FromArgb(196, 218, 240),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(bgBrush, 0, 0, w, h);
                    }

                    // Couche 2 : brillance blanche sur le tiers supérieur (effet vitre)
                    int glossH = Math.Max(1, h * 2 / 5);
                    using (var glossBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, w, glossH + 1),
                        Color.FromArgb(140, 255, 255, 255),
                        Color.FromArgb(0, 255, 255, 255),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(glossBrush, 0, 0, w, glossH);
                    }

                    // Couche 3 : reflet lumineux bas (bord interne lumineux)
                    using (var bottomGlow = new LinearGradientBrush(
                        new Rectangle(0, h - 6, w, 6),
                        Color.FromArgb(0, 255, 255, 255),
                        Color.FromArgb(60, 255, 255, 255),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(bottomGlow, 0, h - 6, w, 6);
                    }

                    // Couche 4 : fine ligne bleue de brillance tout en haut (1px)
                    using (var topLine = new Pen(Color.FromArgb(180, 220, 240, 255), 1f))
                        g.DrawLine(topLine, 0, 0, w, 0);

                    break;
                }

                case ThemeId.ModernLight:
                case ThemeId.ModernDark:
                    using (var modernBrush = new SolidBrush(_theme.TitleBarColor))
                    {
                        g.FillRectangle(modernBrush, 0, 0, w, h);
                    }
                    using (var topAccent = new Pen(_theme.AccentColor, 1f))
                    {
                        g.DrawLine(topAccent, 0, 0, w, 0);
                    }
                    break;

                case ThemeId.Fluent11Light:
                case ThemeId.Fluent11Dark:
                    using (var fluentBase = new SolidBrush(_theme.TitleBarColor))
                    {
                        g.FillRectangle(fluentBase, 0, 0, w, h);
                    }
                    using (var micaOverlay = new LinearGradientBrush(
                        new Rectangle(0, 0, w, h),
                        Color.FromArgb(30, 255, 255, 255),
                        Color.FromArgb(5, 255, 255, 255),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(micaOverlay, 0, 0, w, h);
                    }
                    using (var accentGlow = new LinearGradientBrush(
                        new Rectangle(0, 0, w, Math.Max(1, h / 3)),
                        Color.FromArgb(18, _theme.AccentColor),
                        Color.FromArgb(0, _theme.AccentColor),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(accentGlow, 0, 0, w, h / 3);
                    }
                    break;

                default:
                    if (_theme.TitleBarHasGradient)
                    {
                        using var brush = new LinearGradientBrush(
                            new Rectangle(0, 0, w, h),
                            _theme.TitleBarColor,
                            _theme.TitleBarGradientEnd,
                            LinearGradientMode.Horizontal);
                        g.FillRectangle(brush, 0, 0, w, h);
                    }
                    else
                    {
                        using var brush = new SolidBrush(_theme.TitleBarColor);
                        g.FillRectangle(brush, 0, 0, w, h);
                    }
                    break;
            }

            int iconOffset = 8;
            using var iconFont = new Font("Segoe UI", h * 0.45f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var iconBrush = new SolidBrush(Color.FromArgb(180, _theme.TitleBarTextColor));
            g.DrawString("S", iconFont, iconBrush, iconOffset, (h - iconFont.Height) / 2f);

            using var titleBrush = new SolidBrush(_theme.TitleBarTextColor);
            float titleX = iconOffset + iconFont.Height + 4;
            var titleRect = new RectangleF(titleX, 0, w - titleX - 138, h);
            var sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            if (_theme.Id == ThemeId.Aero7)
            {
                using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
                g.DrawString(_title, _theme.TitleBarFont, shadowBrush,
                    new RectangleF(titleX + 1, 1, titleRect.Width, h), sf);
            }
            g.DrawString(_title, _theme.TitleBarFont, titleBrush, titleRect, sf);

            int btnW = _theme.TitleBarHeight >= 30 ? 46 : 38;
            int btnH = h;
            int btnCloseX = w - btnW;
            int btnMaxX = w - btnW * 2;
            int btnMinX = w - btnW * 3;

            DrawWindowButton(g, new Rectangle(btnMinX, 0, btnW, btnH), 1);
            DrawWindowButton(g, new Rectangle(btnMaxX, 0, btnW, btnH), 2);
            DrawWindowButton(g, new Rectangle(btnCloseX, 0, btnW, btnH), 3);

            if (_theme.Id is ThemeId.Legacy95 or ThemeId.LunaXP)
            {
                using var lightPen = new Pen(Color.White);
                using var darkPen = new Pen(Color.FromArgb(128, 128, 128));
                g.DrawLine(lightPen, 0, h - 2, w, h - 2);
                g.DrawLine(darkPen, 0, h - 1, w, h - 1);
            }
            else if (_theme.Id == ThemeId.Aero7)
            {
                using var sepPen = new Pen(Color.FromArgb(120, 110, 165, 220));
                using var glowPen = new Pen(Color.FromArgb(120, 255, 255, 255));
                g.DrawLine(glowPen, 0, h - 2, w, h - 2);
                g.DrawLine(sepPen, 0, h - 1, w, h - 1);
            }
            else if (_theme.Id is ThemeId.Fluent11Light or ThemeId.Fluent11Dark)
            {
                using var sepPen = new Pen(Color.FromArgb(180, _theme.SeparatorColor));
                g.DrawLine(sepPen, 0, h - 1, w, h - 1);
            }
            else
            {
                using var sepPen = new Pen(_theme.SeparatorColor);
                g.DrawLine(sepPen, 0, h - 1, w, h - 1);
            }
        }

        private void DrawWindowButton(Graphics g, Rectangle rect, int btnId)
        {
            bool isHover = _hoverBtn == btnId;
            bool isClose = btnId == 3;
            bool isOldTheme = _theme.TitleBarHeight <= 22 || _theme.TitleButtonsIsOldStyle;

            if (isOldTheme)
            {
                Color btnBg = isHover && isClose ? Color.FromArgb(200, 50, 50) :
                              isHover ? _theme.TitleButtonHoverBg :
                              _theme.TitleBarGradientEnd == Color.FromArgb(192, 192, 192)
                                ? Color.FromArgb(192, 192, 192)
                                : Color.FromArgb(220, 220, 210);

                using var btnBrush = new SolidBrush(btnBg);
                g.FillRectangle(btnBrush, rect);
                ControlPaint.DrawButton(g, rect, ButtonState.Normal);

                var glyphRect = Rectangle.Inflate(rect, -12, -8);
                if (btnId == 1)
                {
                    using var pen = new Pen(Color.Black, 1f);
                    g.DrawLine(pen, glyphRect.Left, glyphRect.Bottom - 1, glyphRect.Right, glyphRect.Bottom - 1);
                }
                else if (btnId == 2)
                {
                    using var pen = new Pen(Color.Black, 1f);
                    if (_parentForm?.WindowState == FormWindowState.Maximized)
                    {
                        g.DrawRectangle(pen, glyphRect.X + 2, glyphRect.Y, glyphRect.Width - 3, glyphRect.Height - 3);
                        g.DrawRectangle(pen, glyphRect.X, glyphRect.Y + 2, glyphRect.Width - 3, glyphRect.Height - 3);
                    }
                    else
                    {
                        g.DrawRectangle(pen, glyphRect.X, glyphRect.Y, glyphRect.Width - 2, glyphRect.Height - 2);
                    }
                }
                else
                {
                    using var pen = new Pen(isHover ? Color.White : Color.Black, 1f);
                    g.DrawLine(pen, glyphRect.Left, glyphRect.Top, glyphRect.Right, glyphRect.Bottom);
                    g.DrawLine(pen, glyphRect.Right, glyphRect.Top, glyphRect.Left, glyphRect.Bottom);
                }

                return;
            }

            if (isHover)
            {
                Color bg = isClose ? _theme.TitleButtonCloseBg : _theme.TitleButtonHoverBg;
                if (_theme.Id is ThemeId.Fluent11Light or ThemeId.Fluent11Dark)
                {
                    using var hoverPath = CreateRoundedPath(Rectangle.Inflate(rect, -5, -4), 6);
                    using var btnBrush = new SolidBrush(Color.FromArgb(215, bg));
                    g.FillPath(btnBrush, hoverPath);
                }
                else
                {
                    using var btnBrush = new SolidBrush(bg);
                    g.FillRectangle(btnBrush, rect);
                }
            }

            string symbol = btnId switch
            {
                1 => "",
                2 => _parentForm?.WindowState == FormWindowState.Maximized ? "" : "",
                3 => "",
                _ => string.Empty
            };

            Color symColor = isClose && isHover ? Color.White : _theme.TitleBarTextColor;
            using var symFont = new Font("Segoe MDL2 Assets", Math.Max(11f, rect.Height * 0.36f), GraphicsUnit.Pixel);
            using var symBrush = new SolidBrush(symColor);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(symbol, symFont, symBrush, rect, sf);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int w = Width;
            int h = Height;
            int btnW = _theme?.TitleBarHeight >= 30 ? 46 : 38;
            int newHover = 0;
            if (e.X >= w - btnW && e.Y < h) newHover = 3;
            else if (e.X >= w - btnW * 2 && e.Y < h) newHover = 2;
            else if (e.X >= w - btnW * 3 && e.Y < h) newHover = 1;

            if (newHover != _hoverBtn)
            {
                _hoverBtn = newHover;
                Invalidate();
            }

            if (_mouseDown && newHover == 0 && _parentForm != null)
            {
                var delta = Cursor.Position - (Size)_lastCursor;
                _parentForm.Location = new Point(
                    _parentForm.Location.X + delta.X,
                    _parentForm.Location.Y + delta.Y);
                _lastCursor = Cursor.Position;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int w = Width;
            int btnW = _theme?.TitleBarHeight >= 30 ? 46 : 38;
            if (e.X < w - btnW * 3)
            {
                _mouseDown = true;
                _lastCursor = Cursor.Position;
            }

            if (e.Clicks == 2 && e.X < w - btnW * 3)
            {
                MaximizeClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _mouseDown = false;
            int w = Width;
            int h = Height;
            int btnW = _theme?.TitleBarHeight >= 30 ? 46 : 38;
            if (e.X >= w - btnW && e.Y < h) CloseClicked?.Invoke(this, EventArgs.Empty);
            else if (e.X >= w - btnW * 2 && e.Y < h) MaximizeClicked?.Invoke(this, EventArgs.Empty);
            else if (e.X >= w - btnW * 3 && e.Y < h) MinimizeClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverBtn = 0;
            _mouseDown = false;
            Invalidate();
        }
    }
}
