using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Single app bar: brand mark, menu, and the loaded trainer's summary on the right.
/// Child controls (menu strip, legality chip, update link) are positioned by <see cref="OnLayout"/>.
/// </summary>
public sealed class TopHeader : Panel, IThemedControl
{
    public string Wordmark { get; set; } = "PKHeX";
    public int LogoInset { get; set; } = 14;

    private string _game = string.Empty;
    private string _ot = string.Empty;
    private string _ids = string.Empty;
    private Color _gameColor;

    /// <summary> Primary action shown when no save file is open. </summary>
    public Button OpenSave { get; } = new() { Text = "Open save file…", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Tag = ThemeWalker.PrimaryTag, Padding = new Padding(10, 1, 10, 1), Name = "B_HeaderOpen" };

    /// <summary> Primary action shown once a save file is open. </summary>
    public Button ExportSave { get; } = new() { Text = "Export save…", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Tag = ThemeWalker.PrimaryTag, Padding = new Padding(10, 1, 10, 1), Name = "B_HeaderExport", Visible = false };

    public TopHeader()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Theme.Current.Canvas;
        Dock = DockStyle.Top;
        Height = LogicalToDeviceUnits(52);
        AccessibleRole = AccessibleRole.TitleBar;
        AccessibleName = Wordmark;
        Controls.Add(OpenSave);
        Controls.Add(ExportSave);
    }

    public void ApplyTheme(ThemePalette palette)
    {
        BackColor = palette.Canvas;
        Invalidate();
    }

    public void SetSave(SaveFile? sav, bool fromFile = true)
    {
        OpenSave.Visible = !fromFile;
        ExportSave.Visible = fromFile && sav is { State.Exportable: true };
        if (sav is null || !fromFile)
        {
            _game = _ot = _ids = string.Empty;
            AccessibleDescription = WinFormsTranslator.Tr("Main.SL_NoSave", "No save loaded");
        }
        else
        {
            _game = sav.Version.ToString();
            _ot = string.IsNullOrWhiteSpace(sav.OT) ? "?" : sav.OT;
            _ids = $"TID {sav.DisplayTID}  ·  SID {sav.DisplaySID}  ·  {(LanguageID)sav.Language}";
            _gameColor = GameAccentResolver.Resolve(sav.Version);
            AccessibleDescription = $"{_game} · {_ot} · TID {sav.DisplayTID} SID {sav.DisplaySID}";
        }
        Invalidate();
    }

    private int LogoSize => LogicalToDeviceUnits(24);
    private Font WordmarkFont => _wordmarkFont ??= new Font(Font.FontFamily, Theme.Pt(11f), FontStyle.Bold);
    private Font? _wordmarkFont;

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _wordmarkFont?.Dispose();
        _wordmarkFont = null; // re-created at the new zoom
    }

    private int BrandRight => LogicalToDeviceUnits(LogoInset) + LogoSize + LogicalToDeviceUnits(8)
                              + TextRenderer.MeasureText(Wordmark, WordmarkFont).Width;

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int right = Width - LogicalToDeviceUnits(12);
        int gap = LogicalToDeviceUnits(16);
        foreach (Control c in Controls)
        {
            switch (c)
            {
                case MenuStrip ms:
                    ms.Location = new Point(BrandRight + gap, (Height - ms.Height) / 2);
                    break;
                case Button b when b.Visible:
                    b.Location = new Point(right - b.Width, (Height - b.Height) / 2);
                    right = b.Left - gap;
                    break;
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var p = Theme.Current;
        g.Clear(p.Canvas);

        int inset = LogicalToDeviceUnits(LogoInset);
        int logo = LogoSize;
        var mark = new Rectangle(inset, (Height - logo) / 2, logo, logo);
        DrawMark(g, mark, p);

        var textSize = TextRenderer.MeasureText(g, Wordmark, WordmarkFont);
        var textBounds = new Rectangle(mark.Right + LogicalToDeviceUnits(8), (Height - textSize.Height) / 2, textSize.Width + 4, textSize.Height);
        TextRenderer.DrawText(g, Wordmark, WordmarkFont, textBounds, p.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        DrawTrainer(g, p);

        if (Theme.Settings.MenuAccentUnderline)
        {
            var line = new Rectangle(0, Height - 2, Width, 2);
            using var fade = new LinearGradientBrush(line, Color.FromArgb(200, p.Accent), Color.FromArgb(0, p.Accent), 0f);
            g.FillRectangle(fade, line);
        }
    }

    // Abstract capsule mark: accent tile with a ring split by a band.
    private void DrawMark(Graphics g, Rectangle mark, ThemePalette p)
    {
        using (var path = Shapes.RoundRect(mark, LogicalToDeviceUnits(7)))
        using (var fill = new LinearGradientBrush(mark, p.AccentHover, p.Mix(p.Accent, Color.Black, 0.25f), 60f))
            g.FillPath(fill, path);

        var ring = Rectangle.Inflate(mark, -LogicalToDeviceUnits(5), -LogicalToDeviceUnits(5));
        float w = Math.Max(1.6f, LogicalToDeviceUnits(2));
        using var pen = new Pen(Color.FromArgb(235, Color.White), w);
        g.DrawEllipse(pen, ring);
        int cy = ring.Top + (ring.Height / 2);
        g.DrawLine(pen, ring.Left, cy, ring.Right, cy);
        int dot = Math.Max(4, ring.Width / 3);
        var center = new Rectangle(ring.Left + ((ring.Width - dot) / 2), cy - (dot / 2), dot, dot);
        using (var hole = new SolidBrush(p.Accent))
            g.FillEllipse(hole, center);
        g.DrawEllipse(pen, center);
    }

    private void DrawTrainer(Graphics g, ThemePalette p)
    {
        if (_game.Length == 0)
            return;

        int right = Width - LogicalToDeviceUnits(12);
        int left = BrandRight;
        foreach (Control c in Controls)
        {
            if (!c.Visible)
                continue;
            if (c is Button)
                right = Math.Min(right, c.Left);
            else
                left = Math.Max(left, c.Right);
        }
        right -= LogicalToDeviceUnits(16);
        left += LogicalToDeviceUnits(16);

        using var otFont = new Font(Font.FontFamily, Theme.Pt(9.5f), FontStyle.Bold);
        using var badgeFont = new Font(Font.FontFamily, Theme.Pt(8f), FontStyle.Bold);
        var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        int idsW = TextRenderer.MeasureText(g, _ids, Font, Size.Empty, flags).Width;
        int otW = TextRenderer.MeasureText(g, _ot, otFont, Size.Empty, flags).Width;
        int badgeTextW = TextRenderer.MeasureText(g, _game, badgeFont, Size.Empty, flags).Width;
        int badgeW = badgeTextW + LogicalToDeviceUnits(16);
        int gap = LogicalToDeviceUnits(10);
        int total = badgeW + gap + otW + gap + idsW;

        bool showIds = right - left >= total;
        if (!showIds)
            total = badgeW + gap + otW;
        if (right - left < total)
            return;

        int x = right - total;
        int cy = Height / 2;
        int bh = LogicalToDeviceUnits(20);
        var badge = new Rectangle(x, cy - (bh / 2), badgeW, bh);
        Shapes.Fill(g, badge, bh / 2, _gameColor);
        var badgeText = _gameColor.GetBrightness() > 0.7f ? Color.FromArgb(0x16, 0x1A, 0x22) : Color.White;
        TextRenderer.DrawText(g, _game, badgeFont, badge, badgeText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        x = badge.Right + gap;

        TextRenderer.DrawText(g, _ot, otFont, new Rectangle(x, 0, otW + 2, Height), p.TextPrimary, flags);
        x += otW + gap;

        if (showIds)
            TextRenderer.DrawText(g, _ids, Font, new Rectangle(x, 0, idsW + 2, Height), p.TextMuted, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _wordmarkFont?.Dispose();
        base.Dispose(disposing);
    }
}
