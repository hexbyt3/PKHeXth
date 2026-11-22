using System;
using System.Drawing;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Drawing.PokeSprite;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Utility logic for drawing individual Slot views that represent underlying <see cref="PKM"/> data.
/// </summary>
public static class SlotUtil
{
    /// <summary>
    /// Gets the background image for a slot based on the provided <see cref="type"/>.
    /// </summary>
    public static Bitmap GetTouchTypeBackground(SlotTouchType type) => type switch
    {
        SlotTouchType.None => SpriteUtil.Spriter.Transparent,
        SlotTouchType.Get => SpriteUtil.Spriter.View,
        SlotTouchType.Set => SpriteUtil.Spriter.Set,
        SlotTouchType.Delete => SpriteUtil.Spriter.Delete,
        SlotTouchType.Swap => SpriteUtil.Spriter.Set,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    /// <summary>
    /// Gets the type of action that should be performed for a Drag &amp; Drop request.
    /// </summary>
    public static DropModifier GetDropModifier() => Control.ModifierKeys switch
    {
        Keys.Shift => DropModifier.Clone,
        Keys.Alt => DropModifier.Overwrite,
        _ => DropModifier.None,
    };

    public static Color GoodDataColor => Color.Transparent;
    public static Color BadDataColor => Color.FromArgb(170, Theme.Current.Invalid);

    /// <summary>
    /// Refreshes a <see cref="PictureBox"/> with the appropriate display content.
    /// </summary>
    /// <summary> Gives a slot tile the plain Pokémon of the sprite it is about to show, so it can zoom the Pokémon on its own. </summary>
    public static void SetSubject(PictureBox pb, PKM? pk)
    {
        if (pb is Controls.SelectablePictureBox tile)
            tile.Subject = pk?.SubjectSprite();
    }

    public static void UpdateSlot(PictureBox pb, ISlotInfo c, PKM p, SaveFile s, bool flagIllegal, SlotTouchType t = SlotTouchType.None)
    {
        pb.BackgroundImage = GetTouchTypeBackground(t);
        if (p.Species == 0) // Nothing in slot
        {
            pb.Image = null;
            pb.BackColor = GoodDataColor;
            return;
        }
        if (!p.Valid) // Invalid
        {
            // Bad Egg present in slot.
            pb.Image = null;
            pb.BackColor = BadDataColor;
            return;
        }

        var img = c switch
        {
            SlotInfoBox b => p.Sprite(s, b.Box, b.Slot, flagIllegal, b.Type),
            SlotInfoParty ps => p.Sprite(s, -1, ps.Slot, flagIllegal, ps.Type),
            _ => p.Sprite(s, -1, -1, flagIllegal, c.Type),
        };

        pb.BackColor = Color.Transparent;
        SetSubject(pb, p);
        pb.Image = img;

        var x = Main.Settings;
        var programLanguage = Language.GetLanguageValue(x.Startup.Language);
        var cfg = x.BattleTemplate;
        var settings = cfg.Hover.GetSettings(programLanguage, p.Context);
        pb.AccessibleDescription = ShowdownParsing.GetLocalizedPreviewText(p, settings);
    }
}
