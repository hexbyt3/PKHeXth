using System.Drawing;

namespace PKHeX.WinForms.Theming;

public sealed record ThemePalette
{
    public required bool IsDark { get; init; }
    public required Color Canvas { get; init; }
    public required Color Surface0 { get; init; }
    public required Color Surface1 { get; init; }
    public required Color Surface2 { get; init; }
    public required Color Border { get; init; }
    public required Color TextPrimary { get; init; }
    public required Color TextMuted { get; init; }
    public required Color TextDisabled { get; init; }
    public required Color Accent { get; init; }
    public required Color AccentHover { get; init; }
    public required Color AccentText { get; init; }
    public required Color Legal { get; init; }
    public required Color Warning { get; init; }
    public required Color Invalid { get; init; }
    public required Color Info { get; init; }

    public Color HoverSurface => Mix(Surface0, Surface2, 0.55f);
    public Color PressedSurface => Mix(Accent, Surface0, 0.85f);
    public Color FocusRing => Accent;
    public Color AccentMuted => Mix(Accent, Surface0, 0.70f);

    // Canvas = window backdrop, Surface0 = cards, Surface1 = fields, Surface2 = raised/hover.
    public static ThemePalette CreateLight(Color accent) => new()
    {
        IsDark = false,
        Canvas = Color.FromArgb(0xE6, 0xE9, 0xEF),
        Surface0 = Color.FromArgb(0xFA, 0xFB, 0xFC),
        Surface1 = Color.FromArgb(0xFF, 0xFF, 0xFF),
        Surface2 = Color.FromArgb(0xEC, 0xEF, 0xF3),
        Border = Color.FromArgb(0xC3, 0xC9, 0xD3),
        TextPrimary = Color.FromArgb(0x16, 0x1A, 0x22),
        TextMuted = Color.FromArgb(0x48, 0x50, 0x5F),
        TextDisabled = Color.FromArgb(0x8E, 0x95, 0xA2),
        Accent = accent,
        AccentHover = Lighten(accent, 0.08f),
        AccentText = Color.White,
        Legal = Color.FromArgb(0x16, 0xA3, 0x4A),
        Warning = Color.FromArgb(0xE0, 0x8A, 0x00),
        Invalid = Color.FromArgb(0xDC, 0x3B, 0x3B),
        Info = Color.FromArgb(0x25, 0x6E, 0xE8),
    };

    public static ThemePalette CreateDark(Color accent) => new()
    {
        IsDark = true,
        Canvas = Color.FromArgb(0x0D, 0x0F, 0x14),
        Surface0 = Color.FromArgb(0x16, 0x19, 0x20),
        Surface1 = Color.FromArgb(0x21, 0x26, 0x30),
        Surface2 = Color.FromArgb(0x2B, 0x31, 0x3D),
        Border = Color.FromArgb(0x3A, 0x41, 0x4F),
        TextPrimary = Color.FromArgb(0xF1, 0xF3, 0xF7),
        TextMuted = Color.FromArgb(0xAF, 0xB6, 0xC6),
        TextDisabled = Color.FromArgb(0x70, 0x77, 0x87),
        Accent = accent,
        AccentHover = Lighten(accent, 0.10f),
        AccentText = Color.White,
        Legal = Color.FromArgb(0x22, 0xC5, 0x5E),
        Warning = Color.FromArgb(0xF5, 0x9E, 0x0B),
        Invalid = Color.FromArgb(0xEF, 0x44, 0x44),
        Info = Color.FromArgb(0x3B, 0x82, 0xF6),
    };

    public ThemePalette WithAccent(Color accent) => this with
    {
        Accent = accent,
        AccentHover = IsDark ? Lighten(accent, 0.10f) : Lighten(accent, 0.08f),
    };

    public Color Mix(Color a, Color b, float t)
    {
        t = Clamp01(t);
        var inv = 1f - t;
        return Color.FromArgb(
            (int)((a.A * inv) + (b.A * t)),
            (int)((a.R * inv) + (b.R * t)),
            (int)((a.G * inv) + (b.G * t)),
            (int)((a.B * inv) + (b.B * t)));
    }

    private static Color Lighten(Color c, float amount)
    {
        amount = Clamp01(amount);
        return Color.FromArgb(
            c.A,
            (int)(c.R + ((255 - c.R) * amount)),
            (int)(c.G + ((255 - c.G) * amount)),
            (int)(c.B + ((255 - c.B) * amount)));
    }

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
}
