using System.Drawing;

namespace SpiceChecker.Services
{
    public static class ColorHelper
    {
        public static Color EnsureMinAlpha(Color color, byte minAlpha = 30)
        {
            if (color.A >= minAlpha)
            {
                return color;
            }

            if (color == Color.Transparent)
            {
                return Color.FromArgb(minAlpha, 0, 0, 0);
            }

            return Color.FromArgb(minAlpha, color.R, color.G, color.B);
        }
    }
}
