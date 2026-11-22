using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PKHeX.WinForms.Theming;

internal static class Shapes
{
    public static GraphicsPath RoundRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0)
        {
            path.AddRectangle(r);
            return path;
        }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void Fill(Graphics g, Rectangle r, int radius, Color fill, Color border = default, float borderWidth = 1f)
    {
        var prev = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = RoundRect(r, radius))
        {
            if (fill.A != 0)
            {
                using var b = new SolidBrush(fill);
                g.FillPath(b, path);
            }
            if (border.A != 0)
            {
                using var pen = new Pen(border, borderWidth);
                g.DrawPath(pen, path);
            }
        }
        g.SmoothingMode = prev;
    }
}
