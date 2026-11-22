using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

public sealed class HeroDragoutCard : Panel, IThemedControl
{
    private Color _primary = Color.FromArgb(40, 120, 220);
    private Color _secondary = Color.Empty;
    private bool _isEgg;
    private bool _empty = true;

    public int CornerRadius { get; set; } = 8;

    public HeroDragoutCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw
                 | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        AccessibleRole = AccessibleRole.Pane;
        AccessibleName = "Preview";
    }

    public void ApplyTheme(ThemePalette palette) => Invalidate();

    public void SetTypes(Color primary, Color? secondary = null, bool isEgg = false, bool empty = false)
    {
        _primary = primary;
        _secondary = secondary ?? Color.Empty;
        _isEgg = isEgg;
        _empty = empty;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var r = ClientRectangle;
        if (r.Width <= 2 || r.Height <= 2)
            return;
        r.Inflate(-1, -1);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var palette = Theme.Current;

        using var path = Shapes.RoundRect(r, LogicalToDeviceUnits(CornerRadius));

        using (var surface = new SolidBrush(palette.Surface1))
            g.FillPath(surface, path);

        if (!_empty)
        {
            var a = GetTintA(palette);
            var b = GetTintB(palette, a);
            using var tint = new LinearGradientBrush(r, a, b, 45f);
            var clip = g.Clip;
            g.SetClip(path);
            g.FillRectangle(tint, r);
            g.Clip = clip;
        }

        using var pen = new Pen(palette.Border, 1f);
        g.DrawPath(pen, path);

        base.OnPaint(e);
    }

    private Color GetTintA(ThemePalette palette)
    {
        int alpha = palette.IsDark ? 80 : 55;
        if (_isEgg)
            return Color.FromArgb(alpha, palette.Surface2);
        return Color.FromArgb(alpha, _primary);
    }

    private Color GetTintB(ThemePalette palette, Color a)
    {
        int alpha = palette.IsDark ? 80 : 55;
        if (_isEgg)
            return Color.FromArgb(0, palette.Surface2);
        if (_secondary.IsEmpty)
            return Color.FromArgb(0, _primary);
        return Color.FromArgb(alpha, _secondary);
    }
}
