using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Draws a <see cref="ProgressBar"/> as a rounded track with an accent fill (the native bar ignores the palette).
/// </summary>
internal sealed class ProgressPainter : NativeWindow
{
    private static readonly ConditionalWeakTable<ProgressBar, ProgressPainter> Attached = new();

    private readonly ProgressBar _pb;

    private ProgressPainter(ProgressBar pb)
    {
        _pb = pb;
        pb.HandleCreated += (_, _) => AssignHandle(pb.Handle);
        if (pb.IsHandleCreated)
            AssignHandle(pb.Handle);
    }

    public static void Attach(ProgressBar pb)
    {
        if (!Attached.TryGetValue(pb, out _))
            Attached.Add(pb, new ProgressPainter(pb));
        pb.Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        if (!_pb.IsDisposed && NativePaint.TryHandle(ref m, Handle, _pb.ClientRectangle, Paint))
            return;
        base.WndProc(ref m);
    }

    private void Paint(Graphics g)
    {
        var p = Theme.Current;
        var r = _pb.ClientRectangle;
        g.Clear(_pb.Parent?.BackColor is { A: 255 } bc ? bc : p.Surface0);

        int h = Math.Min(r.Height, _pb.LogicalToDeviceUnits(8));
        var track = new Rectangle(0, (r.Height - h) / 2, r.Width - 1, h);
        Shapes.Fill(g, track, h / 2, p.Surface2);

        int range = _pb.Maximum - _pb.Minimum;
        if (_pb.Style == ProgressBarStyle.Marquee || range <= 0)
            return;
        int w = (int)((long)(_pb.Value - _pb.Minimum) * track.Width / range);
        if (w <= 0)
            return;
        Shapes.Fill(g, track with { Width = Math.Max(w, h) }, h / 2, _pb.Enabled ? p.Accent : p.TextDisabled);
    }
}
