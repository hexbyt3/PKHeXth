using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Drawing.PokeSprite;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Summary band above the entity editor: sprite card, name, gender/shiny marks, type chips, level, legality chip.
/// Child controls (sprite card, legality chip) are positioned by <see cref="OnLayout"/>.
/// </summary>
public sealed class EntityHeader : Panel, IThemedControl
{
    private string _title = string.Empty;
    private string _detail = string.Empty;
    private string _gender = string.Empty;
    private Color _genderColor;
    private bool _shiny;
    private readonly List<(string Text, Color Color)> _chips = [];

    private Font _titleFont = null!;
    private Font _chipFont = null!;

    public EntityHeader()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        Dock = DockStyle.Top;
        Height = LogicalToDeviceUnits(74);
        CreateFonts();
        AccessibleRole = AccessibleRole.Pane;
        AccessibleName = "Entity summary";
    }

    // Point sizes follow the window zoom (the font changes when the main window rescales).
    private void CreateFonts()
    {
        _titleFont?.Dispose();
        _chipFont?.Dispose();
        _titleFont = new Font(Font.FontFamily, Theme.Pt(13f), FontStyle.Bold);
        _chipFont = new Font(Font.FontFamily, Theme.Pt(7.5f), FontStyle.Bold);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        CreateFonts();
        Invalidate();
    }

    public void ApplyTheme(ThemePalette palette)
    {
        BackColor = palette.Surface0;
        Invalidate();
    }

    public void SetEntity(PKM pk)
    {
        var strings = GameInfo.Strings;
        _chips.Clear();
        _shiny = false;
        _gender = string.Empty;

        if (pk.Species == 0)
        {
            _title = WinFormsTranslator.Tr("Main.Hint_NoPokemon", "No Pokémon");
            _detail = WinFormsTranslator.Tr("Main.Hint_PickSpecies", "Pick a species or drop a file to start");
        }
        else
        {
            var species = (uint)pk.Species < strings.specieslist.Length ? strings.specieslist[pk.Species] : $"#{pk.Species}";
            _title = pk.IsNicknamed && !string.IsNullOrWhiteSpace(pk.Nickname) ? pk.Nickname : species;
            _shiny = pk.IsShiny;
            if (pk.IsEgg)
            {
                _chips.Add(("EGG", Color.FromArgb(0x8A, 0x8F, 0x9C)));
                _detail = species;
            }
            else
            {
                var pi = pk.PersonalInfo;
                AddTypeChip(pi.Type1, strings);
                if (pi.Type2 != pi.Type1)
                    AddTypeChip(pi.Type2, strings);
                _detail = _title == species ? $"Lv. {pk.CurrentLevel}" : $"{species}  ·  Lv. {pk.CurrentLevel}";
                (_gender, _genderColor) = pk.Gender switch
                {
                    0 => ("♂", Color.FromArgb(0x4C, 0x9A, 0xFF)),
                    1 => ("♀", Color.FromArgb(0xFF, 0x6B, 0x9A)),
                    _ => (string.Empty, Color.Empty),
                };
            }
        }
        AccessibleDescription = $"{_title} {_detail}";
        Invalidate();
    }

    private void AddTypeChip(byte type, GameStrings strings)
    {
        var name = type < strings.Types.Count ? strings.Types[type] : "?";
        _chips.Add((name.ToUpperInvariant(), TypeColor.GetTypeSpriteColor(type)));
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        foreach (Control c in Controls)
        {
            if (c is AccentStatusChip chip)
                chip.Location = new Point(Width - chip.Width - Px(4), (Height - chip.Height) / 2);
            else
                c.Location = new Point(Px(6), (Height - c.Height) / 2);
        }
    }

    private int TextLeft()
    {
        int left = Px(6);
        foreach (Control c in Controls)
        {
            if (c is not AccentStatusChip && c.Visible)
                left = Math.Max(left, c.Right);
        }
        return left + Px(14);
    }

    // Layout offsets picked at the default scale, at the current zoom.
    private int Px(int value) => LogicalToDeviceUnits((int)Math.Round(Theme.Pt(value)));

    private int TextRight()
    {
        int right = Width - Px(8);
        foreach (Control c in Controls)
        {
            if (c is AccentStatusChip { Visible: true })
                right = Math.Min(right, c.Left - Px(10));
        }
        return right;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var p = Theme.Current;
        g.Clear(p.Surface0);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var line = new Pen(p.Border))
            g.DrawLine(line, Px(6), Height - 1, Width - Px(6), Height - 1);

        int x = TextLeft();
        int right = TextRight();
        if (right - x < 40)
            return;

        const TextFormatFlags measure = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        int mid = Height / 2;

        // Line 1: name, gender, shiny
        var titleSize = TextRenderer.MeasureText(g, _title, _titleFont, Size.Empty, measure);
        int line1 = mid - Px(2) - titleSize.Height;
        int titleW = Math.Min(titleSize.Width + 2, right - x);
        TextRenderer.DrawText(g, _title, _titleFont, new Rectangle(x, line1, titleW, titleSize.Height), p.TextPrimary, flags);
        int cx = x + titleW + Px(6);
        if (_gender.Length != 0)
        {
            var gs = TextRenderer.MeasureText(g, _gender, _titleFont, Size.Empty, measure);
            if (cx + gs.Width < right)
                TextRenderer.DrawText(g, _gender, _titleFont, new Rectangle(cx, line1, gs.Width, gs.Height), _genderColor, flags);
            cx += gs.Width + Px(4);
        }
        if (_shiny && cx + Px(16) < right)
        {
            var gs = TextRenderer.MeasureText(g, "★", _titleFont, Size.Empty, measure);
            TextRenderer.DrawText(g, "★", _titleFont, new Rectangle(cx, line1, gs.Width, gs.Height), Color.FromArgb(0xF5, 0xB8, 0x2E), flags);
        }

        // Line 2: type chips, then muted detail
        int chipH = Px(18);
        int line2 = mid + Px(4);
        cx = x;
        foreach (var (text, color) in _chips)
        {
            int w = TextRenderer.MeasureText(g, text, _chipFont, Size.Empty, measure).Width + Px(16);
            if (cx + w > right)
                break;
            var r = new Rectangle(cx, line2, w, chipH);
            Shapes.Fill(g, r, chipH / 2, color);
            var fore = color.GetBrightness() > 0.68f ? Color.FromArgb(0x16, 0x1A, 0x22) : Color.White;
            TextRenderer.DrawText(g, text, _chipFont, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            cx = r.Right + Px(5);
        }
        if (_chips.Count != 0)
            cx += Px(4);
        if (cx < right)
            TextRenderer.DrawText(g, _detail, Font, new Rectangle(cx, line2, right - cx, chipH), p.TextMuted, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _chipFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
