using System;
using System.Drawing;
using System.Windows.Forms;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Slim horizontal bar (rounded track + colored fill), used to visualize a base stat next to its row.
/// </summary>
public sealed class StatBar : Control, IThemedControl
{
    private int _value;
    private int _max = 255;
    private Color _fill = Color.Gray;

    public StatBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        TabStop = false;
        Size = new Size(48, 10);
        AccessibleRole = AccessibleRole.ProgressBar;
    }

    public void SetValue(int value, Color fill, int max = 255)
    {
        (_value, _fill, _max) = (value, fill, Math.Max(1, max));
        AccessibleDescription = $"{value} of {_max}";
        Invalidate();
    }

    public void ApplyTheme(ThemePalette palette) => Invalidate();

    protected override void OnPaint(PaintEventArgs e)
    {
        var p = Theme.Current;
        var g = e.Graphics;
        g.Clear(Parent?.BackColor is { A: 255 } bc ? bc : p.Surface0);
        int h = Math.Min(Height, LogicalToDeviceUnits(6));
        var track = new Rectangle(0, (Height - h) / 2, Width - 1, h);
        Shapes.Fill(g, track, h / 2, p.Surface2);
        int w = (int)Math.Round(track.Width * Math.Clamp(_value / (double)_max, 0, 1));
        if (w > 0)
            Shapes.Fill(g, track with { Width = Math.Max(w, h) }, h / 2, _fill);
    }
}
