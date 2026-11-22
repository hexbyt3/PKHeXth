using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

public sealed class EditorSidebar : Control, IThemedControl
{
    private readonly List<Item> _items = [];
    private readonly HashSet<string> _invalid = [];
    private readonly ToolTip _tip = new() { ShowAlways = true, AutoPopDelay = 8000, InitialDelay = 600 };
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private string _tipFor = string.Empty;

    [DefaultValue(40)]
    public int ItemHeight { get; set; } = 40;

    protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
    {
        base.ScaleControl(factor, specified);
        ItemHeight = (int)Math.Round(ItemHeight * factor.Height); // rows grow with the rest of the layout
    }

    public event EventHandler? SelectedIndexChanged;

    public EditorSidebar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Current.Surface0;
        ForeColor = Theme.Current.TextPrimary;
        TabStop = true;
        AccessibleRole = AccessibleRole.PageTabList;
    }

    [Browsable(false)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _items.Count || _selectedIndex == value)
                return;
            _selectedIndex = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void AddItem(string name, string text, Color pip, string glyph = "")
        => InsertItem(_items.Count, name, text, pip, glyph);

    public void InsertItem(int index, string name, string text, Color pip, string glyph = "")
    {
        _items.Insert(index, new Item(name, text, pip, glyph));
        int before = _selectedIndex;
        if (_selectedIndex == -1)
            _selectedIndex = 0;
        else if (index <= _selectedIndex)
            _selectedIndex++;
        Invalidate();
        if (_selectedIndex != before)
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveItem(string name)
    {
        int i = IndexOf(name);
        if (i < 0)
            return;
        _items.RemoveAt(i);
        _invalid.Remove(name);
        int before = _selectedIndex;
        if (_items.Count == 0)
            _selectedIndex = -1;
        else if (i < _selectedIndex)
            _selectedIndex--;
        else if (i == _selectedIndex)
            _selectedIndex = Math.Min(_selectedIndex, _items.Count - 1);
        Invalidate();
        if (_selectedIndex != before)
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SelectByName(string name)
    {
        var i = IndexOf(name);
        if (i >= 0)
            SelectedIndex = i;
    }

    public int IndexOf(string name)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Name == name)
                return i;
        }
        return -1;
    }

    /// <summary> Relabels every item from the translation key "Tab_{Name}" (the keys the old tab strip used). </summary>
    public void Translate(Func<string, string, string> translate)
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i] = _items[i] with { Text = translate($"Tab_{_items[i].Name}", _items[i].Text) };
        Invalidate();
    }

    public void SetItemInvalid(string name, bool invalid)
    {
        bool changed = invalid ? _invalid.Add(name) : _invalid.Remove(name);
        if (changed)
            Invalidate();
    }

    public void ClearInvalid()
    {
        if (_invalid.Count == 0)
            return;
        _invalid.Clear();
        Invalidate();
    }

    public void ApplyTheme(ThemePalette palette)
    {
        BackColor = palette.Surface0;
        ForeColor = palette.TextPrimary;
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTheme(Theme.Current);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _tip.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int idx = HitTest(e.Y);
        if (idx == _hoverIndex)
            return;
        _hoverIndex = idx;
        UpdateTip();
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverIndex == -1)
            return;
        _hoverIndex = -1;
        UpdateTip();
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;
        int idx = HitTest(e.Y);
        if (idx < 0)
            return;
        Focus();
        SelectedIndex = idx;
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_items.Count == 0)
            return;
        switch (e.KeyCode)
        {
            case Keys.Down:
            case Keys.PageDown:
                SelectedIndex = (_selectedIndex + 1) % _items.Count;
                e.Handled = true;
                break;
            case Keys.Up:
            case Keys.PageUp:
                SelectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;
                e.Handled = true;
                break;
            case Keys.Home:
                SelectedIndex = 0;
                e.Handled = true;
                break;
            case Keys.End:
                SelectedIndex = _items.Count - 1;
                e.Handled = true;
                break;
        }
    }

    private void UpdateTip()
    {
        if (_hoverIndex < 0)
        {
            _tip.Hide(this);
            _tipFor = string.Empty;
            return;
        }
        var item = _items[_hoverIndex];
        var msg = _invalid.Contains(item.Name) ? $"{item.Text} (invalid)" : item.Text;
        if (msg == _tipFor)
            return;
        _tipFor = msg;
        _tip.SetToolTip(this, msg);
    }

    private int HitTest(int y)
    {
        if (ItemHeight <= 0)
            return -1;
        int i = y / ItemHeight;
        return (uint)i < (uint)_items.Count ? i : -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var p = Theme.Current;
        var g = e.Graphics;
        using (var bg = new SolidBrush(p.Surface0))
            g.FillRectangle(bg, ClientRectangle);

        for (int i = 0; i < _items.Count; i++)
            DrawItem(g, i, p);
    }

    private void DrawItem(Graphics g, int index, ThemePalette p)
    {
        var rect = new Rectangle(0, index * ItemHeight, Width, ItemHeight);
        bool selected = index == _selectedIndex;
        bool hover = index == _hoverIndex;

        var pill = Rectangle.FromLTRB(rect.X + 6, rect.Y + 3, rect.Right - 6, rect.Bottom - 3);
        if (selected)
            Shapes.Fill(g, pill, 8, p.Mix(p.Surface0, p.Accent, p.IsDark ? 0.18f : 0.12f));
        else if (hover)
            Shapes.Fill(g, pill, 8, p.HoverSurface);
        if (selected)
            Shapes.Fill(g, new Rectangle(pill.X, pill.Y + 8, 3, pill.Height - 16), 1, p.Accent);

        var item = _items[index];
        bool invalid = _invalid.Contains(item.Name);
        DrawIcon(g, rect, item, selected, invalid, p);
        DrawLabel(g, rect, item.Text, p, selected, invalid);

        if (selected && Focused && ShowFocusCues)
            DrawFocusRing(g, rect, p);
    }

    private Font? IconFont => Icons.Get((float)Math.Round(Font.Size + 2)); // null when no icon font: draw a colored dot instead

    private int IconSize => (int)(ItemHeight * 0.45); // 18px at the original 40px row

    private void DrawIcon(Graphics g, Rectangle rect, Item item, bool selected, bool invalid, ThemePalette p)
    {
        var box = new Rectangle(rect.X + 15, rect.Y + ((rect.Height - IconSize) / 2), IconSize, IconSize);
        var color = invalid ? p.Invalid : selected ? item.Pip : p.TextMuted;
        if (IconFont is not null && item.Glyph.Length != 0)
        {
            TextRenderer.DrawText(g, item.Glyph, IconFont, box, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        else
        {
            Shapes.Fill(g, Rectangle.Inflate(box, -5, -5), 4, color);
        }
        if (invalid) // attention dot
            Shapes.Fill(g, new Rectangle(box.Right - 5, box.Y - 1, 7, 7), 4, p.Invalid, p.Surface0);
    }

    private void DrawLabel(Graphics g, Rectangle rect, string text, ThemePalette p, bool selected, bool invalid)
    {
        int textX = rect.X + 15 + IconSize + 10;
        var textRect = new Rectangle(textX, rect.Y, rect.Right - textX - 4, rect.Height);

        using var flags = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };
        var color = invalid ? p.Invalid : selected ? p.TextPrimary : p.TextMuted;
        using var brush = new SolidBrush(color);
        if (!selected)
        {
            g.DrawString(text, Font, brush, textRect, flags);
            return;
        }
        using var bold = new Font(Font, FontStyle.Bold);
        g.DrawString(text, bold, brush, textRect, flags);
    }

    private static void DrawFocusRing(Graphics g, Rectangle rect, ThemePalette p)
    {
        Shapes.Fill(g, Rectangle.FromLTRB(rect.X + 6, rect.Y + 3, rect.Right - 7, rect.Bottom - 4), 8, Color.Empty, p.FocusRing);
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new SidebarAccessible(this);

    private sealed record Item(string Name, string Text, Color Pip, string Glyph);

    private sealed class SidebarAccessible(EditorSidebar owner) : ControlAccessibleObject(owner)
    {
        public override AccessibleRole Role => AccessibleRole.PageTabList;
        public override int GetChildCount() => owner._items.Count;
        public override AccessibleObject? GetChild(int index)
            => (uint)index < (uint)owner._items.Count ? new ItemAccessible(owner, index) : null;
    }

    private sealed class ItemAccessible(EditorSidebar owner, int index) : AccessibleObject
    {
        public override AccessibleRole Role => AccessibleRole.PageTab;
        public override string Name => owner._items[index].Text;
        public override Rectangle Bounds
        {
            get
            {
                var local = new Rectangle(0, index * owner.ItemHeight, owner.Width, owner.ItemHeight);
                return owner.RectangleToScreen(local);
            }
        }
        public override AccessibleStates State
        {
            get
            {
                var s = AccessibleStates.Selectable | AccessibleStates.Focusable;
                if (index == owner._selectedIndex)
                {
                    s |= AccessibleStates.Selected;
                    if (owner.Focused)
                        s |= AccessibleStates.Focused;
                }
                return s;
            }
        }
        public override string Description
            => owner._invalid.Contains(owner._items[index].Name) ? "Contains invalid data" : string.Empty;
        public override void DoDefaultAction() => owner.SelectedIndex = index;
        public override AccessibleObject Parent => owner.AccessibilityObject;
    }
}
