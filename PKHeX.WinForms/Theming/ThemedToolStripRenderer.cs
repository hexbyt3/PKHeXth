using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Menus and tool strips: flat strips, and drop-downs with inset rounded highlights, quiet separators,
/// muted shortcut text and accent checkmarks. Drop-down windows get rounded corners via <see cref="PopupStyle"/>.
/// </summary>
internal sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
{
    private const int HoverRadius = 6;
    private const int Inset = 5; // gap between the drop-down edge and an item's highlight

    public ThemedToolStripRenderer() : base(new ThemedColorTable()) => RoundedEdges = false;

    private static bool IsPopup(ToolStrip ts) => ts is ToolStripDropDown;

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var p = Theme.Current;
        var bg = IsPopup(e.ToolStrip) ? p.Surface1
            : e.ToolStrip.Parent is { BackColor.A: 255 } host ? host.BackColor
            : p.Surface0;
        using var brush = new SolidBrush(bg);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (!IsPopup(e.ToolStrip) || PopupStyle.HasRoundedCorners)
            return; // DWM draws the rounded border (and shadow) itself
        using var pen = new Pen(Theme.Current.Border, 1f);
        var b = e.AffectedBounds;
        e.Graphics.DrawRectangle(pen, b.X, b.Y, b.Width - 1, b.Height - 1);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // No separate icon gutter: the whole drop-down is one surface.
        using var brush = new SolidBrush(Theme.Current.Surface1);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;
        bool open = item is ToolStripMenuItem { DropDown.Visible: true };
        if (!item.Enabled || (!item.Selected && !open))
            return;

        var p = Theme.Current;
        var bounds = new Rectangle(Point.Empty, item.Size);
        if (item.Owner is MenuStrip)
        {
            var r = Rectangle.Inflate(bounds, -1, -3);
            Shapes.Fill(e.Graphics, r, HoverRadius, p.Mix(p.Canvas, p.TextPrimary, open ? 0.12f : 0.08f));
            return;
        }

        var row = Rectangle.FromLTRB(bounds.Left + Inset, bounds.Top + 1, bounds.Right - Inset, bounds.Bottom - 1);
        Shapes.Fill(e.Graphics, row, HoverRadius, p.Mix(p.Surface1, p.TextPrimary, p.IsDark ? 0.10f : 0.07f)); // neutral, like Windows 11 menus
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var p = Theme.Current;
        bool shortcut = e.Item.Owner is ToolStripDropDown && (e.TextFormat & TextFormatFlags.Right) != 0; // shortcut keys are drawn right-aligned
        e.TextColor = !e.Item.Enabled ? p.TextDisabled : shortcut ? p.TextMuted : p.TextPrimary;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        var p = Theme.Current;
        var color = e.Item is { Enabled: false } ? p.TextDisabled : p.TextMuted;
        if (e.Item?.Owner is not ToolStripDropDown)
        {
            e.ArrowColor = color;
            base.OnRenderArrow(e);
            return;
        }

        // Thin chevron instead of the filled triangle.
        var r = e.ArrowRectangle;
        int cx = r.Left + (r.Width / 2) - 1;
        int cy = r.Top + (r.Height / 2);
        int s = Math.Max(3, r.Height / 5);
        var g = e.Graphics;
        var prev = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(pen, [new Point(cx - (s / 2), cy - s), new Point(cx + (s / 2), cy), new Point(cx - (s / 2), cy + s)]);
        g.SmoothingMode = prev;
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var p = Theme.Current;
        var bounds = e.Item.Bounds;
        using var pen = new Pen(p.Mix(p.Border, p.Surface1, 0.35f), 1f);
        int y = bounds.Height / 2;
        e.Graphics.DrawLine(pen, Inset + 8, y, bounds.Width - Inset - 8, y);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var g = e.Graphics;
        var p = Theme.Current;
        var r = Rectangle.Inflate(e.ImageRectangle, 1, 1);
        Shapes.Fill(g, r, 4, p.Accent);

        var b = e.ImageRectangle;
        var prev = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(p.AccentText, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(pen,
        [
            new Point(b.Left + 3, b.Top + (b.Height / 2)),
            new Point(b.Left + (b.Width / 2) - 1, b.Bottom - 4),
            new Point(b.Right - 3, b.Top + 4),
        ]);
        g.SmoothingMode = prev;
    }

    protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
    {
        if (e.Item is ToolStripMenuItem { Checked: true } && e.Image is null)
            return; // check glyph drawn by OnRenderItemCheck
        var p = Theme.Current;
        if (e.Image is not Bitmap bmp || !p.IsDark || !IsDarkGlyph(bmp))
        {
            base.OnRenderItemImage(e);
            return;
        }

        // Black single-color icons vanish on a dark menu: paint them in the text color, keeping their alpha.
        var c = e.Item is { Enabled: false } ? p.TextDisabled : p.TextMuted;
        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(
        [
            [0, 0, 0, 0, 0],
            [0, 0, 0, 0, 0],
            [0, 0, 0, 0, 0],
            [0, 0, 0, 1, 0],
            [c.R / 255f, c.G / 255f, c.B / 255f, 0, 1],
        ]));
        var r = e.ImageRectangle;
        e.Graphics.DrawImage(bmp, r, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Image, object> DarkGlyphs = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Image, object> ColorImages = new();

    /// <summary> True for icons whose visible pixels are all dark and unsaturated (black glyphs). Cached per image. </summary>
    private static bool IsDarkGlyph(Bitmap bmp)
    {
        if (DarkGlyphs.TryGetValue(bmp, out _))
            return true;
        if (ColorImages.TryGetValue(bmp, out _))
            return false;
        bool glyph = true;
        int visible = 0;
        for (int y = 0; y < bmp.Height && glyph; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                var px = bmp.GetPixel(x, y);
                if (px.A < 64)
                    continue;
                visible++;
                if (px.GetBrightness() > 0.35f || px.GetSaturation() > 0.25f)
                {
                    glyph = false;
                    break;
                }
            }
        }
        glyph &= visible != 0;
        (glyph ? DarkGlyphs : ColorImages).AddOrUpdate(bmp, bmp);
        return glyph;
    }
}
