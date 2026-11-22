using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Drawing.PokeSprite;

namespace PKHeX.WinForms.Controls;

public partial class PokeGrid : UserControl
{
    public PokeGrid()
    {
        InitializeComponent();
    }

    public readonly List<PictureBox> Entries = [];
    public int Slots { get; private set; }

    private int sizeW = 68;
    private int sizeH = 56;

    public bool InitializeGrid(int width, int height, SpriteBuilder info)
    {
        var newCount = width * height;
        if (Slots == newCount)
        {
            if (info.Width == sizeW && info.Height == sizeH)
                return false;
        }

        sizeW = info.Width;
        sizeH = info.Height;
        (_cols, _rows) = (width, height);
        Generate(width, height);
        Slots = newCount;

        return true;
    }

    private int _cols, _rows;

    /// <summary>
    /// Grows the tiles so the grid fills <paramref name="area"/> (never below the normal tile size, at most 1.6x of it);
    /// sprites stay centered at their native size.
    /// </summary>
    public void FitTo(Size area)
    {
        if (_cols == 0 || _rows == 0 || Entries.Count != _cols * _rows)
            return;
        int gapPx = (int)Math.Round(gap * Theming.Theme.UiScale);
        int minW = Tile(sizeW), minH = Tile(sizeH);
        int tileW = Math.Clamp(((area.Width - (2 * padEdge) + gapPx) / _cols) - gapPx, minW, (int)(minW * 1.6));
        int tileH = Math.Clamp(((area.Height - (2 * padEdge) + gapPx) / _rows) - gapPx, minH, (int)(minH * 1.6));
        SuspendLayout();
        for (int i = 0; i < Entries.Count; i++)
        {
            int row = i / _cols, column = i % _cols;
            Entries[i].SetBounds(padEdge + (column * (tileW + gapPx)), padEdge + (row * (tileH + gapPx)), tileW, tileH);
        }
        Size = new Size((2 * padEdge) - gapPx + (_cols * (tileW + gapPx)), (2 * padEdge) - gapPx + (_rows * (tileH + gapPx)));
        ResumeLayout();
    }

    private const int padEdge = 1; // edges
    private const int border = 1; // tile inset around the sprite
    private const int gap = 4; // between tiles

    private void Generate(int width, int height)
    {
        SuspendLayout();
        Controls.Clear();
        foreach (var c in Entries)
            c.Dispose();
        Entries.Clear();

        int tileW = Tile(sizeW), tileH = Tile(sizeH);
        int gap = (int)Math.Round(PokeGrid.gap * Theming.Theme.UiScale);

        Location = Location with { X = 0 }; // prevent auto-expanding parent if position changes (centered)
        for (int row = 0; row < height; row++)
        {
            var y = padEdge + (row * (tileH + gap));
            for (int column = 0; column < width; column++)
            {
                var name = $"Pokémon Grid Row {row:00} Column {column:00}";
                var x = padEdge + (column * (tileW + gap));
                var pb = GetControl(sizeW, sizeH, name);
                pb.Location = new Point(x, y);
                Entries.Add(pb);
            }
        }

        int w = (2 * padEdge) - gap + (width * (tileW + gap));
        int h = (2 * padEdge) - gap + (height * (tileH + gap));
        Size = new Size(w, h);
        Controls.AddRange(Entries.Cast<Control>().ToArray());
        ResumeLayout();
    }

    // Wallpapers clash with the dark palette: dark mode shows plain slot tiles on the card instead.
    public void SetBackground(Image? img) => BackgroundImage = !DesignMode && Theming.Theme.Current.IsDark ? null : img;

    /// <summary> Slot tile size for a sprite dimension: sprite plus its 1px inset, scaled with the UI (sprite stays centered). </summary>
    public static int Tile(int sprite) => (int)Math.Round((sprite + (2 * border)) * Theming.Theme.UiScale);

    public static SelectablePictureBox GetControl(int width, int height, string name) => new()
    {
        AutoSize = false,
        SizeMode = PictureBoxSizeMode.CenterImage,
        BackColor = SlotUtil.GoodDataColor,
        Width = Tile(width),
        Height = Tile(height),
        Padding = Padding.Empty,
        Margin = Padding.Empty,
        BorderStyle = BorderStyle.None,
        DrawTile = true,
        AccessibleRole = AccessibleRole.Alert,
        Name = name,
        AccessibleName = name,
    };
}
