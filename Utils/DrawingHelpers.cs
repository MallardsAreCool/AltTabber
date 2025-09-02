using System.Drawing;
using System.Drawing.Drawing2D;

namespace AltTabber.Utils
{
    internal static class DrawingHelpers
    {
        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) throw new PlatformNotSupportedException("GraphicsPath is only supported on Windows 6.1 and later.");

            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            // top left
            path.AddArc(arc, 180, 90);

            // top right
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
