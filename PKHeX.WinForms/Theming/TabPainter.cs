using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Replaces the native 3D tab chrome of any standard <see cref="TabControl"/> with flat underline tabs.
/// Hooks WM_PAINT through a <see cref="NativeWindow"/>, so no designer changes are needed.
/// </summary>
internal sealed class TabPainter : NativeWindow
{
    private static readonly ConditionalWeakTable<TabControl, TabPainter> Attached = new();

    private readonly TabControl _tc;
    private int _hover = -1;

    private TabPainter(TabControl tc)
    {
        _tc = tc;
        tc.HandleCreated += (_, _) => AssignHandle(tc.Handle);
        tc.MouseMove += (_, e) => SetHover(HitTest(e.Location));
        tc.MouseLeave += (_, _) => SetHover(-1);
        tc.SelectedIndexChanged += (_, _) => tc.Invalidate();
        if (tc.IsHandleCreated)
            AssignHandle(tc.Handle);
    }

    public static void Attach(TabControl tc, ThemePalette p)
    {
        // Owner-drawn tab controls already style their own tabs.
        if (tc.DrawMode != TabDrawMode.Normal || tc.Appearance != TabAppearance.Normal)
            return;
        var back = Back(tc, p);
        foreach (TabPage tp in tc.TabPages)
        {
            tp.UseVisualStyleBackColor = false;
            tp.BackColor = back;
            tp.ForeColor = p.TextPrimary;
        }
        if (!Attached.TryGetValue(tc, out _))
            Attached.Add(tc, new TabPainter(tc));
        tc.Invalidate();
    }

    private static Color Back(Control c, ThemePalette p)
    {
        for (var parent = c.Parent; parent is not null; parent = parent.Parent)
        {
            var bc = parent.BackColor;
            if (bc.A == 255)
                return bc;
        }
        return p.Surface0;
    }

    private void SetHover(int index)
    {
        if (index == _hover)
            return;
        _hover = index;
        _tc.Invalidate();
    }

    private int HitTest(Point pt)
    {
        for (int i = 0; i < _tc.TabCount; i++)
        {
            if (_tc.GetTabRect(i).Contains(pt))
                return i;
        }
        return -1;
    }

    protected override void WndProc(ref Message m)
    {
        if (!_tc.IsDisposed && NativePaint.TryHandle(ref m, Handle, _tc.ClientRectangle, Paint))
            return;
        base.WndProc(ref m);
    }

    private void Paint(Graphics g)
    {
        var p = Theme.Current;
        var back = Back(_tc, p);
        g.Clear(back);
        if (_tc.TabCount == 0)
            return;

        var align = _tc.Alignment;
        var strip = Rectangle.Empty;
        for (int i = 0; i < _tc.TabCount; i++)
            strip = i == 0 ? _tc.GetTabRect(i) : Rectangle.Union(strip, _tc.GetTabRect(i));

        // Hidden tab strips (ItemSize ~ 1px) are used as plain page hosts; only clear the chrome.
        bool vertical = align is TabAlignment.Left or TabAlignment.Right;
        if ((vertical ? strip.Width : strip.Height) <= 4)
            return;

        using (var line = new Pen(p.Border))
        {
            var cr = _tc.ClientRectangle;
            switch (align)
            {
                case TabAlignment.Top: g.DrawLine(line, cr.Left, strip.Bottom, cr.Right, strip.Bottom); break;
                case TabAlignment.Bottom: g.DrawLine(line, cr.Left, strip.Top - 1, cr.Right, strip.Top - 1); break;
                case TabAlignment.Left: g.DrawLine(line, strip.Right, cr.Top, strip.Right, cr.Bottom); break;
                default: g.DrawLine(line, strip.Left - 1, cr.Top, strip.Left - 1, cr.Bottom); break;
            }
        }

        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        int bar = Math.Max(2, _tc.LogicalToDeviceUnits(2));
        for (int i = 0; i < _tc.TabCount; i++)
        {
            var r = _tc.GetTabRect(i);
            bool selected = i == _tc.SelectedIndex;
            bool hover = i == _hover;
            var page = _tc.TabPages[i];

            if (hover && !selected)
                Shapes.Fill(g, Rectangle.Inflate(r, -2, -3), 5, p.HoverSurface);

            var color = !page.Enabled ? p.TextDisabled : selected || hover ? p.TextPrimary : p.TextMuted;
            var textRect = r;
            if (_tc.ImageList is { } il && (page.ImageIndex >= 0 || !string.IsNullOrEmpty(page.ImageKey)))
            {
                var img = page.ImageIndex >= 0 && page.ImageIndex < il.Images.Count ? il.Images[page.ImageIndex] : il.Images[page.ImageKey];
                if (img is not null)
                {
                    g.DrawImage(img, r.X + 6, r.Y + ((r.Height - img.Height) / 2), img.Width, img.Height);
                    textRect = Rectangle.FromLTRB(r.X + 6 + img.Width, r.Y, r.Right, r.Bottom);
                }
            }
            TextRenderer.DrawText(g, page.Text, _tc.Font, textRect, color, flags);

            if (!selected)
                continue;
            var accent = align switch
            {
                TabAlignment.Top => new Rectangle(r.X + 6, r.Bottom - bar + 1, r.Width - 12, bar),
                TabAlignment.Bottom => new Rectangle(r.X + 6, r.Top - 1, r.Width - 12, bar),
                TabAlignment.Left => new Rectangle(r.Right - bar + 1, r.Y + 5, bar, r.Height - 10),
                _ => new Rectangle(r.Left - 1, r.Y + 5, bar, r.Height - 10),
            };
            Shapes.Fill(g, accent, 1, p.Accent);
        }
    }
}
