using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Repaints the 1px non-client border of single-border inputs (text boxes, numeric fields) in palette colors:
/// subtle at rest, brighter on hover, accent while focused.
/// </summary>
internal sealed class BorderPainter : NativeWindow
{
    private static readonly ConditionalWeakTable<Control, BorderPainter> Attached = new();

    private readonly Control _c;
    private bool _hover;

    private BorderPainter(Control c)
    {
        _c = c;
        c.HandleCreated += (_, _) => AssignHandle(c.Handle);
        c.GotFocus += (_, _) => Redraw();
        c.LostFocus += (_, _) => Redraw();
        c.EnabledChanged += (_, _) => Redraw();
        c.MouseEnter += (_, _) => { _hover = true; Redraw(); };
        c.MouseLeave += (_, _) => { _hover = false; Redraw(); };
        if (c.IsHandleCreated)
            AssignHandle(c.Handle);
    }

    public static void Attach(Control c)
    {
        if (!Attached.TryGetValue(c, out var existing))
            Attached.Add(c, existing = new BorderPainter(c));
        existing.Redraw();
    }

    // Single-line text boxes: the frame lives in a padded non-client area so the text sits vertically centered with
    // a left/right inset that matches the combo boxes (Windows text boxes cannot center text themselves).
    private bool _padded;
    private Padding _inset;

    public static void AttachPadded(TextBoxBase tb)
    {
        if (tb.Multiline)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
            Attach(tb);
            return;
        }
        if (tb.BorderStyle != BorderStyle.None)
        {
            // Keep the designer height (the padding takes the place of the border), but give short boxes (20px hex
            // fields) the regular field height so rows line up.
            int height = tb.Height;
            int min = Math.Max(tb.LogicalToDeviceUnits(23), tb.Font.Height + 9); // same height as a combo box at this font
            if (height < min)
            {
                tb.Margin = tb.Margin with { Top = Math.Max(0, tb.Margin.Top - ((min - height) / 2)) };
                height = min;
            }
            tb.AutoSize = false;
            tb.BorderStyle = BorderStyle.None;
            tb.Height = height;
            // Values in small boxes (levels, IDs, IVs...) read better centered.
            if (tb.Width < tb.LogicalToDeviceUnits(72))
            {
                if (tb is TextBox { TextAlign: HorizontalAlignment.Left } t)
                    t.TextAlign = HorizontalAlignment.Center;
                else if (tb is MaskedTextBox { TextAlign: HorizontalAlignment.Left } m)
                    m.TextAlign = HorizontalAlignment.Center;
            }
        }
        if (!Attached.TryGetValue(tb, out var painter))
        {
            Attached.Add(tb, painter = new BorderPainter(tb) { _padded = true });
            tb.Resize += (_, _) => painter.Reframe();
            tb.FontChanged += (_, _) => painter.Reframe();
            tb.HandleCreated += (_, _) => painter.Reframe();
        }
        painter.Reframe();
    }

    private void Reframe()
    {
        if (_c.IsHandleCreated)
            SetWindowPos(_c.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
    }

    private Padding ComputeInset(int width, int height)
    {
        // Measured glyph height (not the font's line spacing): tall fonts like the in-game one still center.
        // +2: some fonts (the in-game one) draw descenders below their measured height, which would clip "g", "y"...
        int text = TextRenderer.MeasureText("Ag", _c.Font, Size.Empty, TextFormatFlags.NoPadding).Height + 4;
        int top = Math.Max(1, (height - text) / 2);
        int bottom = Math.Max(1, height - text - top);
        int side = _c.LogicalToDeviceUnits(width < _c.LogicalToDeviceUnits(72) ? 2 : 6); // small boxes (PID, IDs, levels) keep their digits
        return new Padding(side, top, side, bottom);
    }

    private void CalcSize(ref Message m)
    {
        if (m.WParam != IntPtr.Zero)
        {
            var p = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
            p.rgrc0 = Shrink(p.rgrc0);
            Marshal.StructureToPtr(p, m.LParam, false);
        }
        else
        {
            var r = Marshal.PtrToStructure<RECT>(m.LParam);
            Marshal.StructureToPtr(Shrink(r), m.LParam, false);
        }
        m.Result = IntPtr.Zero;
    }

    private RECT Shrink(RECT r)
    {
        _inset = ComputeInset(r.Right - r.Left, r.Bottom - r.Top);
        r.Left += _inset.Left;
        r.Top += _inset.Top;
        r.Right -= _inset.Right;
        r.Bottom -= _inset.Bottom;
        return r;
    }

    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCHITTEST = 0x0084;
    private const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010, SWP_FRAMECHANGED = 0x0020;

    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        public RECT rgrc0, rgrc1, rgrc2;
        public IntPtr lppos;
    }

    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    private void Redraw()
    {
        if (_c.IsHandleCreated)
            RedrawWindow(_c.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE);
    }

    private const int WM_PAINT = 0x000F;
    private const int WM_NCPAINT = 0x0085;
    private const uint RDW_INVALIDATE = 0x0001;
    private const uint RDW_FRAME = 0x0400;

    protected override void WndProc(ref Message m)
    {
        if (_padded && m.Msg == WM_NCCALCSIZE)
        {
            CalcSize(ref m);
            return;
        }
        if (_padded && m.Msg == WM_NCHITTEST)
        {
            m.Result = 1; // HTCLIENT: clicks in the padding still place the caret
            return;
        }
        base.WndProc(ref m);
        if (m.Msg is WM_NCPAINT or WM_PAINT && !_c.IsDisposed) // .NET may draw the frame in either pass
            Paint();
    }

    private void Paint()
    {
        var p = Theme.Current;
        bool focused = _c.ContainsFocus;
        var color = !_c.Enabled ? p.Mix(p.Border, p.Surface0, 0.4f)
            : focused ? p.Accent
            : _hover ? p.Mix(p.Border, p.TextPrimary, 0.25f)
            : p.Border;

        var hdc = GetWindowDC(Handle);
        if (hdc == IntPtr.Zero)
            return;
        try
        {
            GetWindowRect(Handle, out var r);
            int w = r.Right - r.Left, h = r.Bottom - r.Top;
            using var g = Graphics.FromHdc(hdc);
            if (_padded) // the padding belongs to the field: fill it so the text area and its margins read as one box
            {
                using var margin = new Region(new Rectangle(0, 0, w, h));
                margin.Exclude(new Rectangle(_inset.Left, _inset.Top, w - _inset.Horizontal, h - _inset.Vertical));
                using var back = new SolidBrush(_c.BackColor);
                g.FillRegion(back, margin);
            }
            // Rounded like the combo boxes: paint the corners in the parent color, then a rounded 1px frame.
            var frame = new Rectangle(0, 0, w - 1, h - 1);
            using var path = Shapes.RoundRect(frame, Math.Min(_c.LogicalToDeviceUnits(4), h / 3));
            using (var outside = new Region(new Rectangle(0, 0, w, h)))
            {
                outside.Exclude(path);
                using var parent = new SolidBrush(_c.Parent?.BackColor is { A: 255 } pb ? pb : p.Surface0);
                g.FillRegion(parent, outside);
            }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(color);
            g.DrawPath(pen, path);
        }
        finally
        {
            ReleaseDC(Handle, hdc);
        }
    }

    private static readonly ConditionalWeakTable<RichTextBox, object> Framed = new();

    /// <summary>
    /// Removes a rich text box's native frame, pads its text, and has the parent draw a themed rounded frame around it.
    /// </summary>
    public static void FrameRichText(RichTextBox rtb)
    {
        rtb.BorderStyle = BorderStyle.None;
        if (!Framed.TryAdd(rtb, rtb))
            return;

        void Pad()
        {
            if (!rtb.IsHandleCreated || !rtb.Multiline)
                return;
            var r = Rectangle.Inflate(rtb.ClientRectangle, -rtb.LogicalToDeviceUnits(6), -rtb.LogicalToDeviceUnits(4));
            var rect = new RECT { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };
            SendMessage(rtb.Handle, EM_SETRECT, IntPtr.Zero, ref rect);
        }
        void Hook(Control? parent)
        {
            if (parent is null)
                return;
            parent.Paint += (_, e) =>
            {
                if (rtb.Parent != parent || !rtb.Visible)
                    return;
                var p = Theme.Current;
                var frame = Rectangle.Inflate(rtb.Bounds, 1, 1);
                frame.Width--;
                frame.Height--;
                Shapes.Fill(e.Graphics, frame, rtb.LogicalToDeviceUnits(4), Color.Empty, rtb.Focused ? p.Accent : p.Border);
            };
            parent.Invalidate();
        }

        rtb.HandleCreated += (_, _) => Pad();
        rtb.Resize += (_, _) => Pad();
        rtb.GotFocus += (_, _) => rtb.Parent?.Invalidate(Rectangle.Inflate(rtb.Bounds, 2, 2));
        rtb.LostFocus += (_, _) => rtb.Parent?.Invalidate(Rectangle.Inflate(rtb.Bounds, 2, 2));
        rtb.ParentChanged += (_, _) => Hook(rtb.Parent);
        Hook(rtb.Parent);
        Pad();
    }

    private const int EM_SETRECT = 0x00B3;
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr w, ref RECT r);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hWnd, IntPtr rect, IntPtr rgn, uint flags);
}
