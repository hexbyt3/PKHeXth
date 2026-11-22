using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Shared WM_PAINT / WM_PRINTCLIENT handling for painters that fully replace a native control's drawing.
/// </summary>
internal static class NativePaint
{
    private const int WM_PAINT = 0x000F;
    private const int WM_ERASEBKGND = 0x0014;
    private const int WM_PRINTCLIENT = 0x0318;

    /// <summary>
    /// Handles paint-related messages by calling <paramref name="paint"/> (double-buffered); returns false for
    /// any other message so the caller forwards it to the native window procedure.
    /// </summary>
    public static bool TryHandle(ref Message m, IntPtr hwnd, Rectangle client, Action<Graphics> paint)
    {
        switch (m.Msg)
        {
            case WM_ERASEBKGND:
                m.Result = 1;
                return true;
            case WM_PRINTCLIENT or WM_PAINT when m.WParam != IntPtr.Zero: // caller-supplied DC
                using (var dc = Graphics.FromHdc(m.WParam))
                    paint(dc);
                return true;
            case WM_PAINT:
                var ps = new PAINTSTRUCT();
                var hdc = BeginPaint(hwnd, ref ps);
                try
                {
                    using var g = Graphics.FromHdc(hdc);
                    using var buffer = BufferedGraphicsManager.Current.Allocate(g, client);
                    paint(buffer.Graphics);
                    buffer.Render(g);
                }
                finally
                {
                    EndPaint(hwnd, ref ps);
                }
                return true;
            default:
                return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public int fErase;
        public int left, top, right, bottom;
        public int fRestore;
        public int fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);
    [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);
}
