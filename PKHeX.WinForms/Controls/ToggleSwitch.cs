using System;
using System.Drawing;
using System.Windows.Forms;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// On/off switch: accent track with a white knob when on, outlined track when off. Click, Space or Enter toggles.
/// </summary>
public sealed class ToggleSwitch : Control, IThemedControl
{
    private bool _checked;
    private bool _hover;

    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        Size = new Size(LogicalToDeviceUnits(40), LogicalToDeviceUnits(22));
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.CheckButton;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;
            _checked = value;
            AccessibleDescription = value ? "On" : "Off";
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ApplyTheme(ThemePalette palette) => Invalidate();

    protected override void OnClick(EventArgs e)
    {
        Focus();
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Space or Keys.Enter || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is not (Keys.Space or Keys.Enter))
            return;
        Checked = !Checked;
        e.Handled = true;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var p = Theme.Current;
        var g = e.Graphics;
        var parent = Parent?.BackColor is { A: 255 } bc ? bc : p.Surface0;
        g.Clear(parent);

        var track = new Rectangle(1, 1, Width - 3, Height - 3);
        var fill = _checked ? (_hover ? p.AccentHover : p.Accent) : p.Surface1;
        var border = Focused ? p.FocusRing : _checked ? fill : _hover ? p.Mix(p.Border, p.TextPrimary, 0.3f) : p.Mix(p.Border, p.TextPrimary, 0.15f);
        if (!Enabled)
            (fill, border) = (p.Mix(fill, parent, 0.5f), p.Mix(border, parent, 0.5f));
        Shapes.Fill(g, track, track.Height / 2, fill, border, Focused ? 2f : 1f);

        int pad = LogicalToDeviceUnits(4);
        int knob = track.Height - (2 * pad);
        int x = _checked ? track.Right - pad - knob : track.Left + pad;
        var knobColor = _checked ? p.AccentText : p.TextMuted;
        Shapes.Fill(g, new Rectangle(x, track.Top + pad, knob, knob), knob / 2, Enabled ? knobColor : p.TextDisabled);
    }
}
