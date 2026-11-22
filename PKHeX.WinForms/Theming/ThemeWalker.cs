using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

internal static class ThemeWalker
{
    public const string OptOutTag = "notheme";

    /// <summary> Set a button's Tag to this to render it as the primary (accent) action. </summary>
    public const string PrimaryTag = "primary";

    public static void Apply(Control root, ThemePalette p)
    {
        if (root is null)
            return;
        ApplyToControl(root, p);

        if (root is Form f && f.ContextMenuStrip is { } ctx)
            StyleContextMenuStrip(ctx, p);

        foreach (Control child in root.Controls)
            Apply(child, p);
    }

    private static readonly Dictionary<(string, float, FontStyle), Font> ModernFonts = [];

    // Designer leftovers: Courier New hex fields become a modern code font at the same size (values still fit),
    // tiny Microsoft Sans Serif text becomes the UI font at a readable size.
    private static void ModernizeFont(Control c)
    {
        var f = c.Font;
        (string Family, float Size)? target = f.Name switch
        {
            "Courier New" => (FontUtil.MonospaceFamily.Name, f.Size),
            "Microsoft Sans Serif" => (Control.DefaultFont.Name, Math.Max(f.Size, Control.DefaultFont.Size)),
            _ => null,
        };
        if (target is not { } t)
            return;
        var key = (t.Family, t.Size, f.Style);
        if (!ModernFonts.TryGetValue(key, out var font))
            ModernFonts[key] = font = new Font(t.Family, t.Size, f.Style);
        c.Font = font;
    }

    private static void ApplyToControl(Control c, ThemePalette p)
    {
        if (c.Tag is string tag && tag.Contains(OptOutTag))
            return;

        ModernizeFont(c);

        if (c is IThemedControl themed)
        {
            themed.ApplyTheme(p);
            if (c.ContextMenuStrip is { } selfCtx)
                StyleContextMenuStrip(selfCtx, p);
            return;
        }

        switch (c)
        {
            case Form form:
                form.BackColor = p.Surface0;
                form.ForeColor = p.TextPrimary;
                break;

            case MenuStrip ms:
                ms.BackColor = ms.Parent is { BackColor.A: 255 } host ? host.BackColor : p.Surface0;
                ms.ForeColor = p.TextPrimary;
                foreach (ToolStripItem i in ms.Items)
                    StyleMenuItem(i, p);
                break;

            case StatusStrip ss:
                ss.BackColor = p.Canvas;
                ss.RenderMode = ToolStripRenderMode.ManagerRenderMode;
                ss.ForeColor = p.TextMuted;
                foreach (ToolStripItem i in ss.Items)
                {
                    i.BackColor = p.Canvas;
                    if (i.ForeColor.IsSystemColor || i.ForeColor == p.TextPrimary)
                        i.ForeColor = p.TextMuted;
                }
                break;

            case ToolStrip ts:
                ts.BackColor = p.Surface0;
                ts.ForeColor = p.TextPrimary;
                foreach (ToolStripItem i in ts.Items)
                    StyleMenuItem(i, p);
                break;

            case TabControl tc:
                TabPainter.Attach(tc, p);
                break;

            case GroupBox gb:
                gb.BackColor = Color.Transparent;
                gb.ForeColor = p.TextMuted;
                if (_themedGroups.TryAdd(gb, _sentinel))
                    gb.Paint += PaintThemedGroupBox;
                gb.Invalidate();
                break;

            case DateTimePicker dtp:
                DatePickerPainter.Attach(dtp);
                break;

            case Button btn:
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = p.Border;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.MouseOverBackColor = p.Surface2;
                btn.FlatAppearance.MouseDownBackColor = p.PressedSurface;
                // Call-to-action buttons get the accent fill: explicit tag, or the conventional Save/OK of a dialog.
                bool primary = btn.Tag is PrimaryTag || btn.Name is "B_Save" or "B_OK";
                btn.BackColor = primary ? p.Accent : p.Surface1;
                btn.ForeColor = primary ? p.AccentText : p.TextPrimary;
                btn.UseVisualStyleBackColor = false;
                if (btn.BackgroundImage is null && _themedButtons.TryAdd(btn, _sentinel))
                    btn.Paint += PaintThemedButton;
                btn.Invalidate();
                break;

            case CheckBox cb:
                StyleCheckBox(cb, p);
                break;

            case RadioButton rb:
                StyleRadioButton(rb, p);
                break;

            case LinkLabel ll:
                ll.BackColor = Color.Transparent;
                ll.LinkColor = p.Accent;
                ll.ActiveLinkColor = p.AccentHover;
                ll.VisitedLinkColor = p.Accent;
                ll.ForeColor = p.TextPrimary;
                break;

            case Label lbl:
                lbl.BackColor = Color.Transparent;
                lbl.ForeColor = lbl.Text.AsSpan().TrimEnd() is [.., ':' or '：'] ? p.TextMuted : p.TextPrimary; // captions recede, values stand out
                break;

            case RichTextBox rtb:
                rtb.BackColor = p.Surface1;
                rtb.ForeColor = p.TextPrimary;
                BorderPainter.FrameRichText(rtb); // rich edit draws its own bright frame, so the parent draws one instead
                break;

            case TextBox tb:
                SetFieldColors(tb, p);
                BorderPainter.AttachPadded(tb);
                break;

            case MaskedTextBox mtb:
                SetFieldColors(mtb, p);
                mtb.HidePromptOnLeave = true; // no "0____" placeholder underscores unless the field is being edited
                BorderPainter.AttachPadded(mtb);
                break;

            case NumericUpDown nud:
                SetFieldColors(nud, p);
                nud.BorderStyle = BorderStyle.FixedSingle;
                BorderPainter.Attach(nud);
                break;

            case ComboBox cbx:
                SetFieldColors(cbx, p);
                cbx.FlatStyle = FlatStyle.Flat; // ComboPainter draws the frame; flat keeps the edit child on BackColor
                if (cbx.MaxDropDownItems == 8) // designer default: show more of long lists
                    cbx.MaxDropDownItems = 14;
                if (cbx.DropDownStyle != ComboBoxStyle.DropDownList)
                    ClearStraySelection(cbx);
                UseThemedList(cbx);
                PopupStyle.Hook(cbx);
                ComboPainter.Attach(cbx);
                break;

            case ListBox lb:
                lb.BackColor = p.Surface1;
                lb.ForeColor = p.TextPrimary;
                lb.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ListView lv:
                lv.BackColor = p.Surface1;
                lv.ForeColor = p.TextPrimary;
                lv.BorderStyle = BorderStyle.FixedSingle;
                break;

            case TreeView tv:
                tv.BackColor = p.Surface1;
                tv.ForeColor = p.TextPrimary;
                tv.BorderStyle = BorderStyle.FixedSingle;
                tv.LineColor = p.Border;
                break;

            case DataGridView dgv:
                StyleDataGridView(dgv, p);
                break;

            case PropertyGrid pg:
                pg.BackColor = p.Surface0;
                pg.ViewBackColor = p.Surface1;
                pg.ViewForeColor = p.TextPrimary;
                pg.ViewBorderColor = p.Border;
                pg.CategoryForeColor = p.TextMuted;
                pg.CategorySplitterColor = p.Border;
                pg.LineColor = p.Border;
                pg.HelpBackColor = p.Surface1;
                pg.HelpForeColor = p.TextPrimary;
                pg.HelpBorderColor = p.Border;
                pg.CommandsBackColor = p.Surface1;
                pg.CommandsForeColor = p.TextPrimary;
                pg.CommandsBorderColor = p.Border;
                pg.DisabledItemForeColor = p.TextDisabled;
                pg.SelectedItemWithFocusBackColor = p.Accent;
                pg.SelectedItemWithFocusForeColor = p.AccentText;
                break;

            case SplitContainer sc:
                sc.BackColor = p.Canvas;
                sc.Panel1.BackColor = p.Canvas;
                sc.Panel2.BackColor = p.Canvas;
                break;

            case ProgressBar pb:
                ProgressPainter.Attach(pb);
                break;

            case TrackBar tb:
                tb.BackColor = p.Surface0;
                tb.ForeColor = p.Accent;
                break;

            case ScrollBar sb:
                sb.BackColor = p.Surface2;
                sb.ForeColor = p.TextPrimary;
                break;

            case PictureBox pic:
                if (pic.BackColor != Color.Transparent)
                    pic.BackColor = p.Surface0;
                break;

            case Panel pan:
                // Designer defaults and light system colors (ButtonHighlight, Window...) become the card surface.
                if (pan.BackColor == Color.White || IsLightSystemColor(pan.BackColor))
                {
                    pan.BackColor = p.Surface0;
                    if (pan.BorderStyle == BorderStyle.FixedSingle) // hard native frame around e.g. database result grids
                        pan.BorderStyle = BorderStyle.None;
                }
                pan.ForeColor = p.TextPrimary;
                break;

            case UserControl uc:
                uc.BackColor = p.Surface0;
                uc.ForeColor = p.TextPrimary;
                break;
        }

        if (c.ContextMenuStrip is { } attached)
            StyleContextMenuStrip(attached, p);
    }

    private static void StyleMenuItem(ToolStripItem item, ThemePalette p)
    {
        item.ForeColor = p.TextPrimary;
        if (item is ToolStripMenuItem && item.Owner is ToolStripDropDown)
            item.Padding = new Padding(2, 3, 2, 3); // roomier dropdown rows
        switch (item)
        {
            case ToolStripMenuItem tsmi:
                tsmi.BackColor = Color.Transparent;
                foreach (ToolStripItem sub in tsmi.DropDownItems)
                    StyleMenuItem(sub, p);
                if (tsmi.DropDown is { } dd)
                {
                    dd.BackColor = p.Surface1;
                    dd.ForeColor = p.TextPrimary;
                    PopupStyle.Hook(dd);
                }
                break;
            case ToolStripComboBox cb:
                cb.BackColor = p.Surface1;
                cb.ForeColor = p.TextPrimary;
                break;
            case ToolStripTextBox tb:
                tb.BackColor = p.Surface1;
                tb.ForeColor = p.TextPrimary;
                break;
        }
    }

    private static void StyleContextMenuStrip(ContextMenuStrip ctx, ThemePalette p)
    {
        ctx.BackColor = p.Surface1;
        ctx.ForeColor = p.TextPrimary;
        PopupStyle.Hook(ctx);
        foreach (ToolStripItem i in ctx.Items)
            StyleMenuItem(i, p);
    }

    // Field surfaces of both palettes: a field still showing one of these (or a system color) has no status color.
    private static readonly int[] DefaultFieldColors =
    [
        ThemePalette.CreateLight(Color.Empty).Surface1.ToArgb(), ThemePalette.CreateDark(Color.Empty).Surface1.ToArgb(),
        Color.White.ToArgb(),
    ];

    // Restyles a field unless PKHeX gave it a status color (base stat heat, EV warning, invalid pick...).
    private static void SetFieldColors(Control c, ThemePalette p)
    {
        if (!c.BackColor.IsSystemColor && Array.IndexOf(DefaultFieldColors, c.BackColor.ToArgb()) < 0)
            return;
        c.BackColor = p.Surface1;
        c.ForeColor = p.TextPrimary;
    }

    private static bool IsLightSystemColor(Color c) => c.IsSystemColor && c.ToKnownColor() is KnownColor.Control
        or KnownColor.ButtonFace or KnownColor.ButtonHighlight or KnownColor.Window or KnownColor.ControlLight
        or KnownColor.ControlLightLight or KnownColor.Info or KnownColor.Menu;

    private static Color ResolveParentBack(Control c, ThemePalette p)
    {
        for (var parent = c.Parent; parent is not null; parent = parent.Parent)
        {
            var bc = parent.BackColor;
            if (bc.A != 0 && bc != Color.Transparent)
                return bc;
        }
        return p.Surface0;
    }

    private static readonly ConditionalWeakTable<CheckBox, object> _themedCheckBoxes = new();
    private static readonly ConditionalWeakTable<RadioButton, object> _themedRadioButtons = new();
    private static readonly ConditionalWeakTable<Button, object> _themedButtons = new();
    private static readonly ConditionalWeakTable<Button, string> _tileGlyphs = new();

    /// <summary> Renders <paramref name="b"/> as a tile: <paramref name="glyph"/> (icon font) above its text. </summary>
    public static void SetTileGlyph(Button b, string glyph)
    {
        _tileGlyphs.AddOrUpdate(b, glyph);
        b.Invalidate();
    }
    private static readonly ConditionalWeakTable<ComboBox, object> _themedCombos = new();
    private static readonly ConditionalWeakTable<GroupBox, object> _themedGroups = new();

    // Replaces the etched native frame with a rounded 1px border and a semibold caption sitting in a gap.
    private static void PaintThemedGroupBox(object? sender, PaintEventArgs e)
    {
        if (sender is not GroupBox gb)
            return;
        var p = Theme.Current;
        var g = e.Graphics;
        g.Clear(ResolveParentBack(gb, p));

        using var font = new Font(gb.Font, FontStyle.Bold);
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        var size = string.IsNullOrEmpty(gb.Text) ? Size.Empty : TextRenderer.MeasureText(g, gb.Text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        int top = size.Height / 2;
        var frame = new Rectangle(0, top, gb.Width - 1, gb.Height - top - 1);
        Shapes.Fill(g, frame, gb.LogicalToDeviceUnits(6), Color.Empty, p.Border);
        if (size.IsEmpty)
            return;

        var caption = new Rectangle(gb.LogicalToDeviceUnits(8), 0, Math.Min(size.Width + 8, gb.Width - 16), size.Height);
        using (var gap = new SolidBrush(ResolveParentBack(gb, p)))
            g.FillRectangle(gap, caption);
        caption.Offset(4, 0);
        TextRenderer.DrawText(g, gb.Text, font, caption, gb.Enabled ? p.TextMuted : p.TextDisabled, flags);
    }

    // Editable combos keep their text selected after a programmatic change, which reads as a stray highlight bar
    // while unfocused.
    private static void ClearStraySelection(ComboBox cbx)
    {
        void Unselect() { if (!cbx.Focused && cbx.IsHandleCreated && cbx.SelectionLength != 0) cbx.SelectionLength = 0; }
        if (_themedCombos.TryAdd(cbx, _sentinel))
        {
            // The control re-selects its text after raising SelectedIndexChanged, so clear it afterwards.
            cbx.SelectedIndexChanged += (_, _) => { if (cbx.IsHandleCreated) cbx.BeginInvoke(Unselect); };
            cbx.Leave += (_, _) => Unselect();
            cbx.VisibleChanged += (_, _) => Unselect();
            cbx.SizeChanged += (_, _) => { if (cbx.IsHandleCreated) cbx.BeginInvoke(Unselect); }; // resizing re-selects the text too
        }
        // Values loaded before theming (sub-form constructors) are still selected; clear once the control is up.
        if (cbx.IsHandleCreated)
            cbx.BeginInvoke(Unselect);
    }

    private static readonly ConditionalWeakTable<ComboBox, object> _drawnCombos = new();

    // Owner-draws the rows of combos that use the default drawing; combos with their own DrawItem (e.g. move
    // legality colors) are left alone.
    private static void UseThemedList(ComboBox cbx)
    {
        if (cbx.DrawMode != DrawMode.Normal && !_drawnCombos.TryGetValue(cbx, out _))
            return;
        cbx.DrawMode = DrawMode.OwnerDrawVariable; // variable: list rows can be taller than the closed field
        cbx.ItemHeight = cbx.Font.Height + 2; // closed height matches editable combos at the current font
        if (!_drawnCombos.TryAdd(cbx, _sentinel))
            return;
        cbx.FontChanged += (_, _) => cbx.ItemHeight = cbx.Font.Height + 2; // window zoom
        cbx.DrawItem += DrawThemedComboItem;
        cbx.MeasureItem += (_, e) => e.ItemHeight = (int)(cbx.LogicalToDeviceUnits(26) * Theme.UiScale / Theme.DefaultUiScale);
    }

    private static void DrawThemedComboItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox cb)
            return;
        var p = Theme.Current;
        bool edit = (e.State & DrawItemState.ComboBoxEdit) != 0;
        bool selected = !edit && (e.State & DrawItemState.Selected) != 0;
        var fore = !cb.Enabled ? p.TextDisabled : cb.ForeColor;
        using (var bg = new SolidBrush(edit ? cb.BackColor : p.Surface1))
            e.Graphics.FillRectangle(bg, e.Bounds);
        if (selected) // neutral rounded row highlight with a small accent marker (Windows 11 style)
        {
            var row = Rectangle.FromLTRB(e.Bounds.Left + 4, e.Bounds.Top + 2, e.Bounds.Right - 4, e.Bounds.Bottom - 2);
            Shapes.Fill(e.Graphics, row, 5, p.Mix(p.Surface1, p.TextPrimary, p.IsDark ? 0.10f : 0.07f));
            Shapes.Fill(e.Graphics, new Rectangle(row.Left, row.Top + (row.Height / 4), 3, row.Height / 2), 1, p.Accent);
        }
        if ((uint)e.Index >= (uint)cb.Items.Count)
            return;
        var text = cb.GetItemText(cb.Items[e.Index]);
        var r = Rectangle.FromLTRB(e.Bounds.X + (edit ? 3 : 12), e.Bounds.Y, e.Bounds.Right - 6, e.Bounds.Bottom);
        TextRenderer.DrawText(e.Graphics, text, e.Font ?? cb.Font, r, fore,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    // Repaints a flat button as a rounded pill-ish surface: fill, 1px border, image + text.
    private static void PaintThemedButton(object? sender, PaintEventArgs e)
    {
        if (sender is not Button b)
            return;
        var p = Theme.Current;
        var g = e.Graphics;
        var r = b.ClientRectangle;
        if (r.Width < 4 || r.Height < 4)
            return;

        using (var parent = new SolidBrush(ResolveParentBack(b, p)))
            g.FillRectangle(parent, r);

        bool hover = b.Enabled && r.Contains(b.PointToClient(Cursor.Position));
        bool pressed = hover && (Control.MouseButtons & MouseButtons.Left) != 0;
        var baseFill = b.BackColor.A == 0 ? p.Surface1 : b.BackColor;
        var fill = !b.Enabled ? p.Mix(baseFill, ResolveParentBack(b, p), 0.5f)
            : pressed ? p.PressedSurface
            : hover ? p.Mix(baseFill, p.TextPrimary, 0.08f)
            : baseFill;
        var border = b.Focused ? p.Accent : hover ? p.Mix(p.Border, p.TextPrimary, 0.15f) : p.Border;
        int radius = Math.Min(b.LogicalToDeviceUnits(6), r.Height / 2);
        Shapes.Fill(g, new Rectangle(0, 0, r.Width - 1, r.Height - 1), radius, fill, border);

        if (_tileGlyphs.TryGetValue(b, out var glyph) && Icons.Get(15f) is { } iconFont)
        {
            // Tile: icon on top, label below.
            int split = (int)(r.Height * 0.56);
            var iconColor = !b.Enabled ? p.TextDisabled : hover ? p.AccentHover : p.Accent;
            TextRenderer.DrawText(g, glyph, iconFont, new Rectangle(0, 2, r.Width, split), iconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, b.Text.Trim('‎'), b.Font, new Rectangle(4, split + 2, r.Width - 8, r.Height - split - 4),
                b.Enabled ? p.TextPrimary : p.TextDisabled,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            return;
        }

        var content = Rectangle.Inflate(r, -3, -1);
        bool hasText = !string.IsNullOrEmpty(b.Text);
        if (b.Image is { } img)
        {
            int x = hasText ? content.X + 2 : (r.Width - img.Width) / 2;
            int y = (r.Height - img.Height) / 2;
            if (b.Enabled)
                g.DrawImage(img, x, y, img.Width, img.Height);
            else
                ControlPaint.DrawImageDisabled(g, img, x, y, fill);
            if (hasText)
                content = Rectangle.FromLTRB(x + img.Width + 2, content.Top, content.Right, content.Bottom);
        }
        if (!hasText)
            return;

        var flags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | (b.UseMnemonic ? 0 : TextFormatFlags.NoPrefix);
        flags |= b.TextAlign switch
        {
            ContentAlignment.TopLeft => TextFormatFlags.Top | TextFormatFlags.Left,
            ContentAlignment.TopCenter => TextFormatFlags.Top | TextFormatFlags.HorizontalCenter,
            ContentAlignment.TopRight => TextFormatFlags.Top | TextFormatFlags.Right,
            ContentAlignment.MiddleLeft => TextFormatFlags.VerticalCenter | TextFormatFlags.Left,
            ContentAlignment.MiddleRight => TextFormatFlags.VerticalCenter | TextFormatFlags.Right,
            ContentAlignment.BottomLeft => TextFormatFlags.Bottom | TextFormatFlags.Left,
            ContentAlignment.BottomCenter => TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter,
            ContentAlignment.BottomRight => TextFormatFlags.Bottom | TextFormatFlags.Right,
            _ => TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter,
        };
        var text = !b.Enabled ? p.TextDisabled : b.ForeColor.IsSystemColor ? p.TextPrimary : b.ForeColor;
        TextRenderer.DrawText(g, b.Text, b.Font, content, text, flags);
    }
    private static readonly object _sentinel = new();

    private static void StyleCheckBox(CheckBox cb, ThemePalette p)
    {
        cb.BackColor = ResolveParentBack(cb, p);
        cb.ForeColor = p.TextPrimary;
        cb.FlatStyle = FlatStyle.Flat;
        cb.FlatAppearance.BorderColor = p.Border;
        cb.FlatAppearance.CheckedBackColor = p.Accent;

        // CheckBox.Appearance.Button can owner-theme correctly; Normal appearance has a Windows-drawn
        // glyph that ignores palette colors. Hook a Paint override to draw a themed glyph.
        if (_themedCheckBoxes.TryAdd(cb, _sentinel))
            cb.Paint += cb.Appearance == Appearance.Button ? PaintToggleButton : PaintThemedCheckBox;
        cb.Invalidate();
    }

    private static void StyleRadioButton(RadioButton rb, ThemePalette p)
    {
        rb.BackColor = ResolveParentBack(rb, p);
        rb.ForeColor = p.TextPrimary;
        rb.FlatStyle = FlatStyle.Flat;
        rb.FlatAppearance.BorderColor = p.Border;
        rb.FlatAppearance.CheckedBackColor = p.Accent;

        if (_themedRadioButtons.TryAdd(rb, _sentinel))
            rb.Paint += rb.Appearance == Appearance.Button ? PaintToggleButton : PaintThemedRadioButton;
        rb.Invalidate();
    }

    // Check boxes / radio buttons shown as buttons (segmented choices): accent fill when on, field surface when off.
    private static void PaintToggleButton(object? sender, PaintEventArgs e)
    {
        if (sender is not ButtonBase b)
            return;
        bool on = b is RadioButton { Checked: true } or CheckBox { Checked: true };
        var p = Theme.Current;
        var g = e.Graphics;
        var r = b.ClientRectangle;
        using (var parent = new SolidBrush(ResolveParentBack(b, p)))
            g.FillRectangle(parent, r);

        bool hover = b.Enabled && r.Contains(b.PointToClient(Cursor.Position));
        var fill = on ? (hover ? p.AccentHover : p.Accent) : hover ? p.HoverSurface : p.Surface1;
        var border = on ? fill : b.Focused ? p.Accent : p.Border;
        if (!b.Enabled)
            (fill, border) = (p.Mix(fill, ResolveParentBack(b, p), 0.5f), p.Mix(border, ResolveParentBack(b, p), 0.5f));
        Shapes.Fill(g, new Rectangle(0, 0, r.Width - 1, r.Height - 1), Math.Min(b.LogicalToDeviceUnits(6), r.Height / 2), fill, border);

        var text = !b.Enabled ? p.TextDisabled : on ? p.AccentText : p.TextPrimary;
        TextRenderer.DrawText(g, b.Text, b.Font, Rectangle.Inflate(r, -4, 0), text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    private static void PaintThemedCheckBox(object? sender, PaintEventArgs e)
    {
        if (sender is not CheckBox cb)
            return;
        var pal = Theme.Current;

        int size = (int)Math.Round(13 * (cb.DeviceDpi / 96.0));
        int y = (cb.ClientSize.Height - size) / 2;
        int x = 0;
        if (cb.CheckAlign == ContentAlignment.MiddleRight || cb.CheckAlign == ContentAlignment.TopRight || cb.CheckAlign == ContentAlignment.BottomRight)
            x = cb.ClientSize.Width - size;
        if (cb.CheckAlign == ContentAlignment.TopCenter || cb.CheckAlign == ContentAlignment.BottomCenter || cb.CheckAlign == ContentAlignment.MiddleCenter)
            x = (cb.ClientSize.Width - size) / 2;

        var box = new Rectangle(x, y, size, size);
        var g = e.Graphics;

        // Erase the default glyph with the control's BackColor.
        using (var bg = new SolidBrush(cb.BackColor))
            g.FillRectangle(bg, Rectangle.Inflate(box, 2, 2));

        var fill = cb.Checked ? pal.Accent : pal.Surface1;
        Shapes.Fill(g, box with { Width = box.Width - 1, Height = box.Height - 1 }, 3, fill, cb.Checked ? pal.Accent : pal.TextDisabled);

        if (cb.CheckState == CheckState.Checked)
        {
            DrawCheckmark(g, box, pal.AccentText);
        }
        else if (cb.CheckState == CheckState.Indeterminate)
        {
            using var ind = new SolidBrush(pal.TextMuted);
            var pad = Math.Max(2, size / 4);
            g.FillRectangle(ind, box.X + pad, box.Y + pad, box.Width - (pad * 2), box.Height - (pad * 2));
        }
    }

    private static void PaintThemedRadioButton(object? sender, PaintEventArgs e)
    {
        if (sender is not RadioButton rb)
            return;
        var pal = Theme.Current;

        int size = (int)Math.Round(13 * (rb.DeviceDpi / 96.0));
        int y = (rb.ClientSize.Height - size) / 2;
        int x = 0;
        if (rb.CheckAlign == ContentAlignment.MiddleRight || rb.CheckAlign == ContentAlignment.TopRight || rb.CheckAlign == ContentAlignment.BottomRight)
            x = rb.ClientSize.Width - size;
        if (rb.CheckAlign == ContentAlignment.TopCenter || rb.CheckAlign == ContentAlignment.BottomCenter || rb.CheckAlign == ContentAlignment.MiddleCenter)
            x = (rb.ClientSize.Width - size) / 2;

        var box = new Rectangle(x, y, size, size);
        var g = e.Graphics;
        var prevSmoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var bg = new SolidBrush(rb.BackColor))
            g.FillRectangle(bg, Rectangle.Inflate(box, 2, 2));

        var fill = rb.Checked ? pal.Accent : pal.Surface1;
        using (var fillBrush = new SolidBrush(fill))
            g.FillEllipse(fillBrush, box);
        using (var pen = new Pen(pal.Border))
            g.DrawEllipse(pen, box.X, box.Y, box.Width - 1, box.Height - 1);

        if (rb.Checked)
        {
            int pad = Math.Max(2, size / 4);
            using var dot = new SolidBrush(pal.AccentText);
            g.FillEllipse(dot, box.X + pad, box.Y + pad, box.Width - (pad * 2), box.Height - (pad * 2));
        }

        g.SmoothingMode = prevSmoothing;
    }

    private static void DrawCheckmark(Graphics g, Rectangle box, Color color)
    {
        var prev = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float w = box.Width;
        float h = box.Height;
        var pts = new[]
        {
            new PointF(box.X + (w * 0.20f), box.Y + (h * 0.52f)),
            new PointF(box.X + (w * 0.44f), box.Y + (h * 0.74f)),
            new PointF(box.X + (w * 0.80f), box.Y + (h * 0.28f)),
        };
        using var pen = new Pen(color, Math.Max(1.5f, w / 8f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(pen, pts);
        g.SmoothingMode = prev;
    }

    private static void StyleDataGridView(DataGridView dgv, ThemePalette p)
    {
        dgv.EnableHeadersVisualStyles = false;
        dgv.BackgroundColor = p.Surface1;
        dgv.GridColor = p.Border;
        dgv.BorderStyle = BorderStyle.FixedSingle;
        dgv.ForeColor = p.TextPrimary;
        dgv.DefaultCellStyle.BackColor = p.Surface1;
        dgv.DefaultCellStyle.ForeColor = p.TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = p.Mix(p.Surface1, p.TextPrimary, p.IsDark ? 0.16f : 0.12f); // neutral, like lists and menus
        dgv.DefaultCellStyle.SelectionForeColor = p.TextPrimary;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = p.Surface2;
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = p.TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = p.Surface0;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = p.TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = p.Surface2;
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = p.TextPrimary;
        dgv.RowHeadersDefaultCellStyle.BackColor = p.Surface0;
        dgv.RowHeadersDefaultCellStyle.ForeColor = p.TextMuted;
        dgv.RowHeadersDefaultCellStyle.SelectionBackColor = p.Surface2;
        dgv.RowHeadersDefaultCellStyle.SelectionForeColor = p.TextPrimary;
    }
}
