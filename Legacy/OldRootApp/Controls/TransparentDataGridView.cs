using System.Drawing;
using System.Windows.Forms;
using SpiceChecker.Services;

namespace SpiceChecker.Controls
{
    public class TransparentDataGridView : DataGridView
    {
        private static readonly Color DefaultTint = Color.FromArgb(60, 32, 32, 32);

        public TransparentDataGridView()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);
            UpdateStyles();
            BackColor = Color.FromArgb(255, 32, 32, 32);
            EnableHeadersVisualStyles = false;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new SolidBrush(ColorHelper.EnsureMinAlpha(DefaultTint, 30));
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);

            if (e.RowIndex < -1 || e.ColumnIndex < 0)
            {
                return;
            }

            using var pen = new Pen(GridColor);
            var bounds = e.CellBounds;
            bounds.Width -= 1;
            bounds.Height -= 1;
            e.Graphics.DrawRectangle(pen, bounds);
        }
    }
}
