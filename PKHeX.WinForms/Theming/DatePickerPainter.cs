using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Draws a <see cref="DateTimePicker"/> as a themed field (the native control ignores BackColor and stays white
/// in dark mode) and recolors its drop-down calendar. Input handling stays native.
/// </summary>
internal sealed class DatePickerPainter : NativeWindow
{
    private static readonly ConditionalWeakTable<DateTimePicker, DatePickerPainter> Attached = new();

    private readonly DateTimePicker _d;
    private bool _hover;

    private DatePickerPainter(DateTimePicker d)
    {
        _d = d;
        d.HandleCreated += (_, _) => AssignHandle(d.Handle);
        d.MouseEnter += (_, _) => { _hover = true; d.Invalidate(); };
        d.MouseLeave += (_, _) => { _hover = false; d.Invalidate(); };
        d.GotFocus += (_, _) => d.Invalidate();
        d.LostFocus += (_, _) => d.Invalidate();
        d.ValueChanged += (_, _) => d.Invalidate();
        d.EnabledChanged += (_, _) => d.Invalidate();
        d.DropDown += (_, _) => ThemeCalendar();
        if (d.IsHandleCreated)
            AssignHandle(d.Handle);
    }

    public static void Attach(DateTimePicker d)
    {
        if (!Attached.TryGetValue(d, out _))
            Attached.Add(d, new DatePickerPainter(d));
        d.Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        if (!_d.IsDisposed && NativePaint.TryHandle(ref m, Handle, _d.ClientRectangle, Paint))
            return;
        base.WndProc(ref m);
    }

    private void Paint(Graphics g)
    {
        var p = Theme.Current;
        var r = _d.ClientRectangle;
        bool active = _d.Enabled && (!_d.ShowCheckBox || _d.Checked);
        using (var bg = new SolidBrush(_d.Enabled ? p.Surface1 : p.Mix(p.Surface1, p.Surface0, 0.5f)))
            g.FillRectangle(bg, r);
        var border = _d.Focused ? p.Accent : _hover ? p.Mix(p.Border, p.TextPrimary, 0.25f) : p.Border;
        using (var pen = new Pen(border))
            g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);

        int x = _d.LogicalToDeviceUnits(4); // same text inset as combo boxes and text boxes
        if (_d.ShowCheckBox)
        {
            int s = _d.LogicalToDeviceUnits(13);
            var box = new Rectangle(x - 1, (r.Height - s) / 2, s, s);
            Shapes.Fill(g, box, 3, _d.Checked ? p.Accent : p.Surface1, _d.Checked ? p.Accent : p.TextDisabled);
            if (_d.Checked)
                TextRenderer.DrawText(g, "✓", _d.Font, box, p.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            x = box.Right + _d.LogicalToDeviceUnits(6);
        }

        int glyphW = _d.ShowUpDown ? 0 : _d.LogicalToDeviceUnits(24);
        var text = new Rectangle(x, 0, Math.Max(0, r.Width - x - glyphW), r.Height);
        TextRenderer.DrawText(g, _d.Text, _d.Font, text, active ? p.TextPrimary : p.TextDisabled,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (glyphW == 0)
            return;
        var glyph = new Rectangle(r.Width - glyphW, 0, glyphW, r.Height);
        var icon = Icons.Get(9.5f);
        TextRenderer.DrawText(g, icon is null ? "▾" : Icons.Calendar, icon ?? _d.Font, glyph, active ? p.TextMuted : p.TextDisabled,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    // The month calendar only honors custom colors once its visual style is removed.
    private void ThemeCalendar()
    {
        var cal = SendMessage(_d.Handle, DTM_GETMONTHCAL, IntPtr.Zero, IntPtr.Zero);
        if (cal == IntPtr.Zero)
            return;
        var p = Theme.Current;
        SetWindowTheme(cal, "", "");
        SetColor(cal, 0, p.Surface1);      // MCSC_BACKGROUND
        SetColor(cal, 1, p.TextPrimary);   // MCSC_TEXT
        SetColor(cal, 2, p.Accent);        // MCSC_TITLEBK
        SetColor(cal, 3, p.AccentText);    // MCSC_TITLETEXT
        SetColor(cal, 4, p.Surface1);      // MCSC_MONTHBK
        SetColor(cal, 5, p.TextDisabled);  // MCSC_TRAILINGTEXT
    }

    private static void SetColor(IntPtr cal, int index, Color c)
        => SendMessage(cal, MCM_SETCOLOR, index, c.R | (c.G << 8) | (c.B << 16));

    private const int DTM_GETMONTHCAL = 0x1008;
    private const int MCM_SETCOLOR = 0x100A;    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int w, int l);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] private static extern int SetWindowTheme(IntPtr hWnd, string app, string idList);
}
