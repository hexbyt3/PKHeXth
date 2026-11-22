using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

public sealed class AccentStatusChip : Control, IThemedControl
{
    public enum ChipStatus { Unknown, Legal, Warning, Invalid }

    private ChipStatus _status = ChipStatus.Unknown;

    [Browsable(true), Category("Appearance")]
    public ChipStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
                return;
            _status = value;
            AccessibleName = $"Legality: {GetLabel()}";
            Invalidate();
        }
    }

    public string LegalText { get; set; } = "Legal";
    public string WarningText { get; set; } = "Fishy";
    public string InvalidText { get; set; } = "Invalid";
    public string UnknownText { get; set; } = "…";

    public AccentStatusChip()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor
                 | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Size = new Size(LogicalToDeviceUnits(88), LogicalToDeviceUnits(24));
        Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = $"Legality: {GetLabel()}";
    }

    public void ApplyTheme(ThemePalette palette) => Invalidate();

    public void SetFromLegality(bool isValid)
        => Status = isValid ? ChipStatus.Legal : ChipStatus.Invalid;

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Space or Keys.Enter;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var r = ClientRectangle;
        if (r.Width <= 0 || r.Height <= 0)
            return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var palette = Theme.Current;
        var (fill, border, text) = GetColors(palette);

        var pill = new Rectangle(0, 0, r.Width - 1, r.Height - 1);
        if (_hover)
            fill = palette.Mix(fill, text, 0.10f);
        Shapes.Fill(g, pill, pill.Height / 2, fill, Focused ? palette.FocusRing : border);

        bool compact = pill.Width < 48;
        int glyphSide = pill.Height - LogicalToDeviceUnits(12);
        var glyph = compact
            ? new Rectangle((pill.Width - glyphSide) / 2, (pill.Height - glyphSide) / 2, glyphSide, glyphSide)
            : new Rectangle(LogicalToDeviceUnits(10), (pill.Height - glyphSide) / 2, glyphSide, glyphSide);
        DrawGlyph(g, glyph, text);

        if (!compact)
        {
            var textBounds = Rectangle.FromLTRB(glyph.Right + LogicalToDeviceUnits(6), 0, pill.Right - LogicalToDeviceUnits(8), pill.Bottom);
            TextRenderer.DrawText(g, GetLabel(), Font, textBounds, text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    private bool _hover;
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }

    // Tinted pill: soft status-colored fill, stronger border, status-colored glyph and text.
    private (Color Fill, Color Border, Color Text) GetColors(ThemePalette p)
    {
        var status = _status switch
        {
            ChipStatus.Legal => p.Legal,
            ChipStatus.Warning => p.Warning,
            ChipStatus.Invalid => p.Invalid,
            _ => p.TextMuted,
        };
        var text = p.IsDark ? p.Mix(status, Color.White, 0.25f) : p.Mix(status, Color.Black, 0.15f);
        return (p.Mix(p.Surface0, status, p.IsDark ? 0.16f : 0.10f), p.Mix(p.Surface0, status, 0.45f), text);
    }

    private string GetLabel() => _status switch
    {
        ChipStatus.Legal => WinFormsTranslator.Tr("Main.Chip_Legal", LegalText),
        ChipStatus.Warning => WinFormsTranslator.Tr("Main.Chip_Fishy", WarningText),
        ChipStatus.Invalid => WinFormsTranslator.Tr("Main.Chip_Invalid", InvalidText),
        _ => UnknownText,
    };

    private void DrawGlyph(Graphics g, Rectangle r, Color color)
    {
        using var pen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        switch (_status)
        {
            case ChipStatus.Legal:
                g.DrawLines(pen,
                [
                    new Point(r.Left + 2, r.Top + (r.Height / 2)),
                    new Point(r.Left + (r.Width / 2) - 1, r.Bottom - 2),
                    new Point(r.Right - 2, r.Top + 2),
                ]);
                break;
            case ChipStatus.Warning:
                int cx = r.Left + (r.Width / 2);
                g.DrawLine(pen, cx, r.Top + 2, cx, r.Top + (r.Height * 2 / 3));
                using (var dot = new SolidBrush(color))
                    g.FillEllipse(dot, cx - 1, r.Bottom - 3, 2, 2);
                break;
            case ChipStatus.Invalid:
                g.DrawLine(pen, r.Left + 2, r.Top + 2, r.Right - 2, r.Bottom - 2);
                g.DrawLine(pen, r.Right - 2, r.Top + 2, r.Left + 2, r.Bottom - 2);
                break;
            case ChipStatus.Unknown:
                int qx = r.Left + (r.Width / 2);
                int qy = r.Top + (r.Height / 2);
                using (var dot = new SolidBrush(color))
                    g.FillEllipse(dot, qx - 1, qy - 1, 2, 2);
                g.DrawArc(pen, qx - 4, qy - 7, 8, 8, 180, 270);
                break;
        }
    }

    private static void AddPill(GraphicsPath path, Rectangle r, int radius)
    {
        if (r.Width <= 0 || r.Height <= 0)
            return;
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        path.AddArc(r.X, r.Y, d, d, 90, 180);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 180);
        path.CloseFigure();
    }
}
