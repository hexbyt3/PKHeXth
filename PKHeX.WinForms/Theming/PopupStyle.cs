using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Rounded corners and a palette border for pop-up windows (menus, combo box lists) on Windows 11.
/// Earlier Windows ignores the request and keeps square pop-ups with the renderer's border.
/// </summary>
internal static class PopupStyle
{
    public static bool HasRoundedCorners { get; } = Environment.OSVersion.Version.Build >= 22000;

    private static readonly ConditionalWeakTable<object, object> Hooked = new();
    private static readonly object Sentinel = new();

    public static void Hook(ToolStripDropDown dd)
    {
        if (!Hooked.TryAdd(dd, Sentinel))
            return;
        dd.Opening += (_, _) => PadEnds(dd);
        dd.Opened += (_, _) => Apply(dd.Handle);
    }

    public static void Hook(ComboBox cb)
    {
        if (!Hooked.TryAdd(cb, Sentinel))
            return;
        cb.DropDown += (_, _) =>
        {
            var list = GetComboParts(cb).List;
            if (list == IntPtr.Zero)
                return;
            Apply(list);
            if (Theme.Current.IsDark) // dark scrollbar in long lists
                SetWindowTheme(list, "DarkMode_Explorer", null);
        };
    }

    // Drop-down menus ignore their own Padding, so the first and last visible items carry the breathing room.
    private static void PadEnds(ToolStripDropDown dd)
    {
        ToolStripItem? first = null, last = null;
        foreach (ToolStripItem i in dd.Items)
        {
            if (!i.Available)
                continue;
            first ??= i;
            last = i;
            i.Margin = Padding.Empty;
        }
        if (first is null || last is null)
            return;
        first.Margin = first.Margin with { Top = 5 };
        last.Margin = last.Margin with { Bottom = 5 };
    }

    /// <summary> Native handles of a combo box's edit child (editable combos) and drop-down list. </summary>
    internal static (IntPtr Edit, IntPtr List) GetComboParts(ComboBox cb)
    {
        var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
        return cb.IsHandleCreated && GetComboBoxInfo(cb.Handle, ref info) ? (info.hwndItem, info.hwndList) : (IntPtr.Zero, IntPtr.Zero);
    }

    private static void Apply(IntPtr hwnd)
    {
        if (!HasRoundedCorners || hwnd == IntPtr.Zero)
            return;
        int round = 3; // DWMWCP_ROUNDSMALL: menus and lists use the small radius on Windows 11
        DwmSetWindowAttribute(hwnd, 33, ref round, sizeof(int)); // DWMWA_WINDOW_CORNER_PREFERENCE
        var c = Theme.Current.Border;
        int border = c.R | (c.G << 8) | (c.B << 16);
        DwmSetWindowAttribute(hwnd, 34, ref border, sizeof(int)); // DWMWA_BORDER_COLOR
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct COMBOBOXINFO
    {
        public int cbSize;
        public RECT rcItem;
        public RECT rcButton;
        public int stateButton;
        public IntPtr hwndCombo;
        public IntPtr hwndItem;
        public IntPtr hwndList;
    }

    [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(IntPtr hwnd, ref COMBOBOXINFO info);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] private static extern int SetWindowTheme(IntPtr hwnd, string app, string? idList);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
}
