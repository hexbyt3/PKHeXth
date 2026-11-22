using System;
using System.Collections.Generic;
using System.Drawing;

namespace PKHeX.WinForms.Theming;

/// <summary>
/// Glyph icons from Segoe Fluent Icons (Windows 11) or Segoe MDL2 Assets (Windows 10).
/// <see cref="Get"/> returns null when neither font is installed so callers can fall back.
/// </summary>
internal static class Icons
{
    private static readonly FontFamily? Family = Array.Find(FontFamily.Families, f => f.Name == "Segoe Fluent Icons")
                                                ?? Array.Find(FontFamily.Families, f => f.Name == "Segoe MDL2 Assets");
    private static readonly Dictionary<float, Font> Cache = [];

    public static Font? Get(float size)
    {
        if (Family is null)
            return null;
        if (!Cache.TryGetValue(size, out var font))
            Cache[size] = font = new Font(Family, size);
        return font;
    }

    public const string Calendar = "";
    public const string ChevronDown = "";
    public const string Search = "";
    public const string Settings = "";
    public const string OpenFile = "";
}
