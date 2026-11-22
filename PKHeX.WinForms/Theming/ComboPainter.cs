using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Draws the closed state of a <see cref="ComboBox"/>: rounded field, chevron, hover/focus border, and (for list-only
/// combos) the selected text. Editable combos keep their native edit child for typing; only the frame around it
/// and the drop-down button area are painted here.
/// </summary>
internal sealed class ComboPainter : NativeWindow
{
    private static readonly ConditionalWeakTable<ComboBox, ComboPainter> Attached = new();

    private readonly ComboBox _c;
    private bool _hover;

    private ComboPainter(ComboBox c)
    {
        _c = c;
        c.HandleCreated += (_, _) => { AssignHandle(c.Handle); SetEditMargin(); };
        c.MouseEnter += (_, _) => SetHover(true);
        c.MouseLeave += (_, _) => SetHover(c.ClientRectangle.Contains(c.PointToClient(Cursor.Position)));
        c.Enter += (_, _) => c.Invalidate();
        c.Leave += (_, _) => c.Invalidate();
        c.DropDown += (_, _) => c.Invalidate();
        c.DropDownClosed += (_, _) => c.Invalidate();
        c.SelectedIndexChanged += (_, _) => c.Invalidate();
        c.TextChanged += (_, _) => c.Invalidate();
        c.EnabledChanged += (_, _) => c.Invalidate();
        c.BackColorChanged += (_, _) => c.Invalidate(); // PKHeX tints invalid selections via BackColor
        if (c.IsHandleCreated)
        {
            AssignHandle(c.Handle);
            SetEditMargin();
        }
    }

    // Editable combos: indent the typing area so its text starts where list-only combos and text boxes start theirs.
    private void SetEditMargin()
    {
        var edit = PopupStyle.GetComboParts(_c).Edit;
        if (edit != IntPtr.Zero && _c.DropDownStyle != ComboBoxStyle.DropDownList)
            SendMessage(edit, EM_SETMARGINS, (IntPtr)EC_LEFTMARGIN, (IntPtr)_c.LogicalToDeviceUnits(3));
    }

    private const int EM_SETMARGINS = 0x00D3;
    private const int EC_LEFTMARGIN = 0x0001;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr w, IntPtr l);

    public static void Attach(ComboBox c)
    {
        if (!Attached.TryGetValue(c, out _))
            Attached.Add(c, new ComboPainter(c));
        c.Invalidate();
    }

    private void SetHover(bool hover)
    {
        if (_hover == hover)
            return;
        _hover = hover;
        _c.Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        if (!_c.IsDisposed && NativePaint.TryHandle(ref m, Handle, _c.ClientRectangle, Paint))
            return;
        base.WndProc(ref m);
    }

    private void Paint(Graphics g)
    {
        var p = Theme.Current;
        var r = _c.ClientRectangle;
        var parent = _c.Parent?.BackColor is { A: 255 } pb ? pb : p.Surface0;
        g.Clear(parent);

        bool focused = _c.ContainsFocus || _c.DroppedDown;
        // PKHeX flags invalid picks by tinting BackColor (e.g. salmon); keep the hint but tone it for dark mode.
        var back = _c.BackColor == p.Surface1 ? p.Surface1 : Theme.Tint(_c.BackColor);
        var fill = _c.Enabled ? back : p.Mix(back, parent, 0.5f);
        var border = !_c.Enabled ? p.Mix(p.Border, parent, 0.4f)
            : focused ? p.Accent
            : _hover ? p.Mix(p.Border, p.TextPrimary, 0.25f)
            : p.Border;
        int radius = _c.LogicalToDeviceUnits(5);
        Shapes.Fill(g, new Rectangle(0, 0, r.Width - 1, r.Height - 1), radius, fill, border);

        bool narrow = r.Width < _c.LogicalToDeviceUnits(80); // small combos keep room for their text
        int button = Math.Min(r.Height, _c.LogicalToDeviceUnits(narrow ? 16 : 24));
        var glyphRect = new Rectangle(r.Width - button - 2, 0, button, r.Height);
        var chevronColor = !_c.Enabled ? p.TextDisabled : focused ? p.Accent : _hover ? p.TextPrimary : p.TextMuted;
        var icon = Icons.Get(7.5f);
        TextRenderer.DrawText(g, icon is null ? "▾" : Icons.ChevronDown, icon ?? _c.Font, glyphRect, chevronColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        if (_c.DropDownStyle != ComboBoxStyle.DropDownList)
            return; // the edit child draws the text

        var text = _c.SelectedIndex >= 0 ? _c.GetItemText(_c.SelectedItem) : _c.Text;
        var textRect = Rectangle.FromLTRB(_c.LogicalToDeviceUnits(4), 0, glyphRect.Left, r.Height);
        TextRenderer.DrawText(g, text, _c.Font, textRect, _c.Enabled ? (back == p.Surface1 ? _c.ForeColor : Theme.TextOn(back)) : p.TextDisabled,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }}
