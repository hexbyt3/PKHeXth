using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHeX.WinForms.Theming;

public static class Theme
{
    private static ThemePalette _current = ThemePalette.CreateLight(GameAccentResolver.Default);
    private static ThemeSettings _settings = new();
    private static readonly ConditionalWeakTable<Form, object> _hooked = new();
    private static readonly object _sentinel = new();

    [ThreadStatic]
    private static bool _applying;

    public static ThemePalette Current => _current;

    /// <summary> Size of the main window's UI relative to the original PKHeX layout (fonts, fields, spacing, slot tiles). </summary>
    public const float DefaultUiScale = 1.2f;

    /// <summary> Current zoom of the main window; changes when the window is resized. </summary>
    public static float UiScale { get; internal set; } = DefaultUiScale;

    /// <summary> A point size picked at the default scale, at the current zoom. </summary>
    public static float Pt(float size) => size * UiScale / DefaultUiScale;

    /// <summary> Status / heatmap fill (stat colors, EV warnings): as-is in light mode, blended into the field in dark mode. </summary>
    public static Color Tint(Color c) => _current.IsDark ? _current.Mix(_current.Surface1, c, 0.42f) : c;

    /// <summary> Readable text color on <paramref name="back"/>. </summary>
    public static Color TextOn(Color back) => back.GetBrightness() > 0.55f ? Color.Black : _current.IsDark ? _current.TextPrimary : Color.White;
    public static ThemeSettings Settings => _settings;

    public static event EventHandler? Changed;

    public static void Initialize(PKHeXSettings pkSettings)
    {
        _settings = pkSettings.Theme;

        if (pkSettings.Startup.DarkMode && _settings.Mode == ThemeMode.System)
        {
            _settings.Mode = ThemeMode.Dark;
            pkSettings.Startup.DarkMode = false;
        }

        ApplyColorMode();
        RebuildPalette();

        ToolStripManager.Renderer = new ThemedToolStripRenderer();

        Application.Idle += OnIdleHookForms;
    }

    public static void SetMode(ThemeMode mode)
    {
        if (_settings.Mode == mode)
            return;
        _settings.Mode = mode;
        ApplyColorMode();
        RebuildPalette();
        RaiseChanged();
    }

    public static void SetAccent(AccentSource source, Color custom)
    {
        _settings.AccentSource = source;
        if (source == AccentSource.Custom)
            _settings.AccentColor = custom;
        RebuildPalette();
        RaiseChanged();
    }

    private static Color _saveAccent = GameAccentResolver.Default;

    /// <summary> Re-reads the theme settings (mode, accent) and repaints every open form. </summary>
    public static void Refresh()
    {
        ApplyColorMode();
        RebuildPalette();
        RaiseChanged();
    }

    public static void OnSaveFileLoaded(SaveFile? sav)
    {
        _saveAccent = GameAccentResolver.Resolve(sav);
        if (_settings.AccentSource != AccentSource.FromSave)
            return;
        var newAccent = _saveAccent;
        if (newAccent.ToArgb() == _current.Accent.ToArgb())
            return;
        _current = _current.WithAccent(newAccent);
        RaiseChanged();
    }

    public static void Apply(Control root)
    {
        if (_applying)
            return;
        _applying = true;
        try
        {
            ThemeWalker.Apply(root, _current);
            if (root is Form { IsHandleCreated: true } f)
                TintTitleBar(f);
        }
        finally
        {
            _applying = false;
        }
    }

    // Windows 11: paint the caption in the form's own background so the title bar blends into the app.
    // Older Windows ignores these.
    private static void TintTitleBar(Form f)
    {
        if (SystemInformation.HighContrast)
            return;
#pragma warning disable WFO5001, WFO9001
        f.FormCaptionBackColor = f.BackColor;
        f.FormCaptionTextColor = _current.TextPrimary;
        f.FormBorderColor = _current.Border;
#pragma warning restore WFO5001, WFO9001
    }

    private static void ApplyColorMode()
    {
#pragma warning disable WFO5001
        var mode = _settings.Mode switch
        {
            ThemeMode.Light => SystemColorMode.Classic,
            ThemeMode.Dark => SystemColorMode.Dark,
            _ => SystemColorMode.System,
        };
        Application.SetColorMode(mode);
#pragma warning restore WFO5001
    }

    private static void RebuildPalette()
    {
        if (SystemInformation.HighContrast)
        {
            _current = HighContrastPalette();
            return;
        }

        var accent = _settings.AccentSource switch
        {
            AccentSource.Custom => _settings.AccentColor,
            AccentSource.FromSave => _saveAccent,
            _ => GameAccentResolver.Default,
        };

        bool dark = _settings.Mode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => SystemColors.Control.GetBrightness() < 0.5f,
        };

        _current = dark ? ThemePalette.CreateDark(accent) : ThemePalette.CreateLight(accent);
    }

    private static ThemePalette HighContrastPalette() => new()
    {
        IsDark = SystemColors.Window.GetBrightness() < 0.5f,
        Canvas = SystemColors.Control,
        Surface0 = SystemColors.Control,
        Surface1 = SystemColors.Window,
        Surface2 = SystemColors.ControlLight,
        Border = SystemColors.ControlDark,
        TextPrimary = SystemColors.ControlText,
        TextMuted = SystemColors.GrayText,
        TextDisabled = SystemColors.GrayText,
        Accent = SystemColors.Highlight,
        AccentHover = SystemColors.HotTrack,
        AccentText = SystemColors.HighlightText,
        Legal = SystemColors.ControlText,
        Warning = SystemColors.ControlText,
        Invalid = SystemColors.ControlText,
        Info = SystemColors.ControlText,
    };

    private static void RaiseChanged()
    {
        foreach (var kv in _hooked)
        {
            var f = kv.Key;
            if (!f.IsDisposed)
                Apply(f);
        }
        Changed?.Invoke(null, EventArgs.Empty);
    }

    private static void OnIdleHookForms(object? sender, EventArgs e) => HookOpenForms();

    /// <summary> Themes every open form that is not themed yet (normally done on the first idle after it opens). </summary>
    public static void HookOpenForms()
    {
        // Application.OpenForms spans threads (the splash screen closes on its own thread), so it can change while
        // being read: copy it first, retrying if it changed mid-copy. Forms on other UI threads are skipped.
        Form[] forms = [];
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                forms = [.. Application.OpenForms.Cast<Form>()];
                break;
            }
            catch (InvalidOperationException)
            {
            }
        }

        foreach (var f in forms)
        {
            if (f.IsDisposed || f.InvokeRequired)
                continue;
            if (!_hooked.TryAdd(f, _sentinel))
                continue;
            HookForm(f);
        }
    }

    private static void HookForm(Form f)
    {
        try
        {
            Controls.SettingsView.UpgradePropertyGrids(f); // plugin settings windows get the settings page
            if (f.IsHandleCreated)
                Apply(f);
            else
                f.HandleCreated += OnFormHandleCreated;
        }
        catch
        {
        }

        f.Disposed += OnFormDisposed;
    }

    private static void OnFormHandleCreated(object? sender, EventArgs e)
    {
        if (sender is not Form f)
            return;
        f.HandleCreated -= OnFormHandleCreated;
        TryApply(f);
    }

    private static void OnFormDisposed(object? sender, EventArgs e)
    {
        if (sender is not Form f)
            return;
        f.Disposed -= OnFormDisposed;
        f.HandleCreated -= OnFormHandleCreated;
        _hooked.Remove(f);
    }

    private static void TryApply(Control c)
    {
        try { Apply(c); }
        catch { }
    }
}
