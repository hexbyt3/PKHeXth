using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms.Controls;

/// <summary>
/// Reflection-driven settings page: category sidebar with search (when there is more than one category) and
/// plain-language rows (title, description, control). Edits are written straight to the settings object.
/// Used by the Settings window, the database windows, and plugin settings windows.
/// </summary>
public sealed class SettingsView : UserControl
{
    private readonly object _root;
    private readonly List<Category> _categories = [];
    private readonly List<(object Owner, Row Row)> _custom = [];
    private readonly EditorSidebar _nav = new() { ItemHeight = 36, Dock = DockStyle.Top };
    private readonly TextBox _search = new() { PlaceholderText = WinFormsTranslator.Tr("SettingsEditor.TB_Search", "Search settings  (Ctrl+F)"), Dock = DockStyle.Top, Name = "TB_Search" };
    private readonly Panel _navPanel;
    private readonly Panel _scroller = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly TableLayoutPanel _page = new() { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2 };
    private readonly Caption _title = new(false) { Dock = DockStyle.Top, AutoSize = true };
    private readonly Caption _subtitle = new(true) { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 2, 0, 8) };
    private readonly HashSet<int> _headerRows = [];
    private readonly List<Label> _wrapped = [];
    private bool _loaded;

    /// <summary> Category last shown, keyed by settings type, so reopening returns to the same place. </summary>
    private static readonly Dictionary<Type, string> LastCategory = [];

    private sealed record Category(string Name, string Title, object Value, string? OwnCategory);
    public sealed record Row(string Title, string Description, Func<Control>? Editor);

    public SettingsView(object root)
    {
        _root = root;
        Dock = DockStyle.Fill;

        _navPanel = new Panel { Dock = DockStyle.Left, Width = LogicalToDeviceUnits(232), Padding = new Padding(12, 18, 12, 8) };
        var navScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 12, 0, 0) };
        navScroll.Controls.Add(_nav);
        _navPanel.Controls.Add(navScroll);
        _navPanel.Controls.Add(_search);
        _navPanel.Paint += (_, e) => DrawLine(e.Graphics, _navPanel.Width - 1, 0, _navPanel.Width - 1, _navPanel.Height);

        _title.Font = new Font(Font.FontFamily, 15f, FontStyle.Bold);
        var header = new Panel { Dock = DockStyle.Top, AutoSize = true };
        header.Controls.Add(_subtitle);
        header.Controls.Add(_title);

        _page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _page.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _page.Padding = new Padding(0, 0, 20, 24);
        _page.CellPaint += (_, e) =>
        {
            if (!_headerRows.Contains(e.Row) && !_headerRows.Contains(e.Row + 1) && e.Row + 1 < _page.RowCount)
                DrawLine(e.Graphics, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
        };
        _scroller.Controls.Add(_page);
        _scroller.Resize += (_, _) => UpdateWrap();

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 20, 4, 0) };
        content.Controls.Add(_scroller);
        content.Controls.Add(header);

        Controls.Add(content);
        Controls.Add(_navPanel);

        LoadCategories();
        _nav.SelectedIndexChanged += (_, _) =>
        {
            if (_nav.SelectedIndex < 0)
                return;
            LastCategory[_root.GetType()] = _categories[_nav.SelectedIndex].Name;
            if (_search.TextLength != 0)
                _search.Clear(); // re-renders through TextChanged
            else
                ShowCurrent();
        };
        _search.TextChanged += (_, _) => ShowCurrent();
    }

    /// <summary> Adds a hand-made row at the top of the category that edits <paramref name="owner"/>. </summary>
    public void AddCustomRow(object owner, string title, string description, Func<Control> editor)
        => _custom.Add((owner, new Row(title, description, editor)));

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (LastCategory.TryGetValue(_root.GetType(), out var last))
            _nav.SelectByName(last);
        _loaded = true;
        ShowCurrent();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F) && _navPanel.Visible)
        {
            _search.Focus();
            _search.SelectAll();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static void DrawLine(Graphics g, int x1, int y1, int x2, int y2)
    {
        using var pen = new Pen(Theme.Current.Border);
        g.DrawLine(pen, x1, y1, x2, y2);
    }

    private void LoadCategories()
    {
        var own = new List<string>();
        foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(_root, [BrowsableAttribute.Yes]))
        {
            if (!IsGroup(pd.PropertyType))
            {
                var cat = pd.Category ?? "Misc";
                if (!own.Contains(cat))
                    own.Add(cat);
                continue;
            }
            if (pd.GetValue(_root) is not { } value)
                continue;
            _categories.Add(new Category(pd.Name, Title(pd.Name, CategoryTitle(pd.Name)), value, null));
        }

        // Loose properties on the root: one category per PropertyGrid category, or a single "General".
        if (own.Count == 1)
            _categories.Insert(0, new Category("General", _categories.Count == 0 ? WinFormsTranslator.Tr("SettingsEditor.Settings", "Settings") : WinFormsTranslator.Tr("SettingsEditor.General", "General"), _root, own[0]));
        else
            _categories.InsertRange(0, own.Select(c => new Category(c, Friendly(c), _root, c)));

        foreach (var c in _categories)
            _nav.AddItem(c.Name, c.Title, Theme.Current.Accent, CategoryGlyph(c.Name));
        _nav.Height = _categories.Count * _nav.ItemHeight;
        _navPanel.Visible = _categories.Count > 1;
    }

    private void ShowCurrent()
    {
        if (!_loaded || _categories.Count == 0)
            return;
        var query = _search.Text.Trim();
        if (query.Length == 0)
        {
            var c = _categories[Math.Max(0, _nav.SelectedIndex)];
            var rows = GetRows(c.Value, null, c.OwnCategory).ToList();
            Render(c.Title, Count(rows.Count(r => r.Editor is not null), "SettingsEditor.CountSetting", "{0} setting", "SettingsEditor.CountSettings", "{0} settings"), rows);
            return;
        }

        var results = new List<Row>();
        foreach (var c in _categories)
        {
            var matches = GetRows(c.Value, query, c.OwnCategory).ToList();
            if (matches.Count == 0)
                continue;
            results.Add(new Row(c.Title, "", null));
            results.AddRange(matches);
        }
        int count = results.Count(r => r.Editor is not null);
        Render(string.Format(WinFormsTranslator.Tr("SettingsEditor.Results", "Results for “{0}”"), query), count == 0 ? WinFormsTranslator.Tr("SettingsEditor.NoResults", "No settings match. Try a different word.") : Count(count, "SettingsEditor.CountMatch", "{0} match", "SettingsEditor.CountMatches", "{0} matches"), results);
    }

    private static string Count(int n, string oneKey, string one, string manyKey, string many)
        => string.Format(n == 1 ? WinFormsTranslator.Tr(oneKey, one) : WinFormsTranslator.Tr(manyKey, many), n);

    /// <summary> Setting / category title from the language file ("SettingsTitle.{name}"), else the English <paramref name="fallback"/>. </summary>
    private static string Title(string name, string fallback) => WinFormsTranslator.Tr($"SettingsTitle.{name}", fallback);
    private static string Title(PropertyDescriptor pd) => Title(pd.Name, Friendly(pd.DisplayName));

    /// <param name="ownCategory">When set, only loose (non-group) properties of that PropertyGrid category are listed.</param>
    private IEnumerable<Row> GetRows(object owner, string? query, string? ownCategory)
    {
        foreach (var (o, row) in _custom)
        {
            if (ReferenceEquals(o, owner) && Matches(query, row.Title, row.Description))
                yield return row;
        }

        foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(owner, [BrowsableAttribute.Yes]))
        {
            if (IsGroup(pd.PropertyType))
            {
                if (ownCategory is not null || pd.GetValue(owner) is not { } child)
                    continue;
                var sub = GetRows(child, query, null).ToList();
                if (sub.Count == 0)
                    continue;
                yield return new Row(Title(pd), "", null);
                foreach (var r in sub)
                    yield return r;
                continue;
            }
            if (ownCategory is not null && (pd.Category ?? "Misc") != ownCategory)
                continue;

            var title = Title(pd);
            var desc = pd.Description ?? string.Empty;
            if (!Matches(query, title, desc) && !Matches(query, pd.Name, string.Empty))
                continue;
            var prop = pd;
            yield return new Row(title, desc, () => CreateEditor(prop, owner));
        }
    }

    private static bool Matches(string? query, string title, string desc) => query is null
        || title.Contains(query, StringComparison.OrdinalIgnoreCase)
        || desc.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool IsGroup(Type t) => t.IsClass && t != typeof(string)
        && !typeof(IEnumerable).IsAssignableFrom(t)
        && t.Namespace?.StartsWith("PKHeX", StringComparison.Ordinal) == true;

    private void Render(string title, string subtitle, List<Row> rows)
    {
        SuspendLayout();
        _page.SuspendLayout();
        var old = _page.Controls.Cast<Control>().ToArray();
        _page.Controls.Clear();
        foreach (var c in old)
            c.Dispose();
        _page.RowStyles.Clear();
        _headerRows.Clear();
        _wrapped.Clear();

        _title.Text = title;
        _subtitle.Text = subtitle;

        int row = 0;
        foreach (var r in rows)
        {
            _page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (r.Editor is null)
            {
                var h = new Caption(true)
                {
                    Text = r.Title.ToUpperInvariant(),
                    AutoSize = true,
                    Font = new Font(Font.FontFamily, 8.25f, FontStyle.Bold),
                    Margin = new Padding(0, row == 0 ? 4 : 26, 0, 4),
                };
                _page.Controls.Add(h, 0, row);
                _page.SetColumnSpan(h, 2);
                _headerRows.Add(row++);
                continue;
            }

            var text = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 11, 24, 11),
            };
            var name = new Caption(false) { Text = r.Title, AutoSize = true, Font = new Font(Font.FontFamily, 9.75f, FontStyle.Bold), Margin = Padding.Empty };
            text.Controls.Add(name);
            _wrapped.Add(name);
            if (r.Description.Length != 0)
            {
                var desc = new Caption(true) { Text = r.Description, AutoSize = true, Margin = new Padding(0, 3, 0, 0) };
                text.Controls.Add(desc);
                _wrapped.Add(desc);
            }

            var editor = r.Editor();
            editor.Anchor = AnchorStyles.Right;
            editor.Margin = new Padding(0, 10, 0, 10);
            _page.Controls.Add(text, 0, row);
            _page.Controls.Add(editor, 1, row);
            row++;
        }
        _page.RowCount = row;

        Theme.Apply(_page);
        UpdateWrap();
        _page.ResumeLayout();
        ResumeLayout();
        _scroller.AutoScrollPosition = Point.Empty;
        _page.Invalidate();
    }

    private void UpdateWrap()
    {
        // Text gets whatever the widest control on the page leaves over.
        int controls = _page.Controls.Cast<Control>().Where(c => _page.GetColumn(c) == 1).Select(c => c.Width).DefaultIfEmpty(0).Max();
        int width = Math.Max(LogicalToDeviceUnits(180), _scroller.ClientSize.Width - _page.Padding.Horizontal - controls - LogicalToDeviceUnits(48));
        foreach (var l in _wrapped)
            l.MaximumSize = new Size(width, 0);
    }

    private Control CreateEditor(PropertyDescriptor pd, object owner)
    {
        var type = Nullable.GetUnderlyingType(pd.PropertyType) ?? pd.PropertyType;
        var value = pd.GetValue(owner);
        Control editor;

        if (type == typeof(bool))
        {
            var toggle = new ToggleSwitch { Checked = value is true, AccessibleName = pd.DisplayName };
            toggle.CheckedChanged += (_, _) => Set(pd, owner, toggle.Checked);
            editor = toggle;
        }
        else if (type.IsEnum && !type.IsDefined(typeof(FlagsAttribute), false))
        {
            var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = LogicalToDeviceUnits(220), AccessibleName = pd.DisplayName };
            foreach (var v in Enum.GetValues(type))
                cb.Items.Add(new EnumChoice(v));
            cb.SelectedItem = cb.Items.Cast<EnumChoice>().FirstOrDefault(z => Equals(z.Value, value));
            cb.SelectedIndexChanged += (_, _) =>
            {
                if (cb.SelectedItem is EnumChoice choice)
                    Set(pd, owner, choice.Value);
            };
            editor = cb;
        }
        else if (GetNumericRange(type) is var (min, max, decimals))
        {
            var nud = new NumericUpDown
            {
                Minimum = min, Maximum = max, DecimalPlaces = decimals,
                Increment = decimals == 0 ? 1 : 0.1m,
                Width = LogicalToDeviceUnits(120), TextAlign = HorizontalAlignment.Right, AccessibleName = pd.DisplayName,
            };
            nud.Value = Math.Clamp(Convert.ToDecimal(value ?? 0), min, max);
            nud.ValueChanged += (_, _) => Set(pd, owner, Convert.ChangeType(nud.Value, type));
            editor = nud;
        }
        else if (type == typeof(string))
        {
            var tb = new TextBox { Text = value as string ?? string.Empty, Width = LogicalToDeviceUnits(260), AccessibleName = pd.DisplayName };
            tb.TextChanged += (_, _) => Set(pd, owner, tb.Text);
            editor = tb;
        }
        else if (type == typeof(Color))
        {
            var b = new Button { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 2, 8, 2), AccessibleName = pd.DisplayName };
            void Show(Color c)
            {
                b.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                var old = b.Image;
                b.Image = Swatch(c, LogicalToDeviceUnits(16));
                old?.Dispose();
            }
            Show(value is Color color ? color : Color.Empty);
            b.Click += (_, _) =>
            {
                using var dlg = new ColorDialog { Color = pd.GetValue(owner) is Color cur ? cur : Color.Empty, FullOpen = true };
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                Set(pd, owner, dlg.Color);
                Show(dlg.Color);
            };
            editor = b;
        }
        else
        {
            // Lists, brushes and other complex values keep the classic grid editor.
            var b = new Button { Text = WinFormsTranslator.Tr("SettingsEditor.B_Edit", "Edit…"), AutoSize = true, Padding = new Padding(10, 2, 10, 2), AccessibleName = pd.DisplayName };
            b.Click += (_, _) => ShowGrid(Title(pd), owner);
            editor = b;
        }

        editor.Enabled = !pd.IsReadOnly;
        return editor;
    }

    private static (decimal Min, decimal Max, int Decimals)? GetNumericRange(Type t) => Type.GetTypeCode(t) switch
    {
        TypeCode.Byte => (byte.MinValue, byte.MaxValue, 0),
        TypeCode.SByte => (sbyte.MinValue, sbyte.MaxValue, 0),
        TypeCode.Int16 => (short.MinValue, short.MaxValue, 0),
        TypeCode.UInt16 => (ushort.MinValue, ushort.MaxValue, 0),
        TypeCode.Int32 => (int.MinValue, int.MaxValue, 0),
        TypeCode.UInt32 => (uint.MinValue, uint.MaxValue, 0),
        TypeCode.Int64 => (long.MinValue, long.MaxValue, 0),
        TypeCode.UInt64 => (ulong.MinValue, ulong.MaxValue, 0),
        TypeCode.Single or TypeCode.Double => (-1_000_000_000m, 1_000_000_000m, 2),
        TypeCode.Decimal => (decimal.MinValue, decimal.MaxValue, 2),
        _ => null,
    };

    private static Bitmap Swatch(Color c, int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        Shapes.Fill(g, new Rectangle(0, 0, size - 1, size - 1), size / 4, c, Color.FromArgb(90, Color.Gray));
        return bmp;
    }

    private static void Set(PropertyDescriptor pd, object owner, object? value)
    {
        try
        {
            pd.SetValue(owner, value);
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Could not change {Title(pd)}.", ex.Message);
        }
    }

    private void ShowGrid(string title, object owner)
    {
        using var form = new Form
        {
            Text = title, Size = new Size(LogicalToDeviceUnits(560), LogicalToDeviceUnits(460)), Icon = FindForm()?.Icon,
            StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false, MinimizeBox = false,
        };
        // Tagged so UpgradePropertyGrids leaves this one alone: it exists for values the rows cannot edit.
        var pg = new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = owner, ToolbarVisible = false, Tag = KeepGridTag };
        form.Controls.Add(pg);
        pg.ExpandAllGridItems();
        form.ShowDialog(this);
        ShowCurrent(); // values may have changed
    }

    private const string KeepGridTag = "keepgrid";

    /// <summary>
    /// Swaps a form-filling <see cref="PropertyGrid"/> that edits a settings object (e.g. a plugin's settings window)
    /// for a <see cref="SettingsView"/> on the same object. The grid stays in place, hidden, so code that still
    /// references it keeps working; edits land on the shared object either way.
    /// </summary>
    public static void UpgradePropertyGrids(Form form)
    {
        if (form is SettingsEditor)
            return;
        foreach (var pg in FindGrids(form))
        {
            if (pg.Tag is KeepGridTag || pg.SelectedObject is not { } obj)
                continue;
            bool fill = pg.Dock == DockStyle.Fill;
            if (!fill && pg.Width * pg.Height < form.ClientSize.Width * form.ClientSize.Height / 2)
                continue; // only grids that are the window's main content
            if (!obj.GetType().Name.Contains("Settings", StringComparison.Ordinal) || pg.Parent is not { } parent)
                continue;
            var view = new SettingsView(obj);
            if (!fill)
            {
                view.Dock = DockStyle.None;
                view.Bounds = pg.Bounds;
                view.Anchor = pg.Anchor;
            }
            pg.Visible = false;
            parent.Controls.Add(view);
            view.BringToFront(); // fill-docked last, so siblings docked to edges (buttons, toolbars) keep their space
            if (form.Width < view.LogicalToDeviceUnits(940))
                form.Width = view.LogicalToDeviceUnits(940);
            if (form.Height < view.LogicalToDeviceUnits(600))
                form.Height = view.LogicalToDeviceUnits(600);
        }
    }

    private static IEnumerable<PropertyGrid> FindGrids(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is PropertyGrid pg)
            {
                yield return pg;
                continue;
            }
            foreach (var inner in FindGrids(c))
                yield return inner;
        }
    }

    private sealed record EnumChoice(object Value)
    {
        public override string ToString() => WinFormsTranslator.Tr($"{Value.GetType().Name}.{Value}", Friendly(Value.ToString() ?? string.Empty));
    }

    /// <summary> Label that keeps its own primary/muted color through theme passes. </summary>
    private sealed class Caption : Label, IThemedControl
    {
        private readonly bool _muted;

        public Caption(bool muted)
        {
            _muted = muted;
            ApplyTheme(Theme.Current);
        }

        public void ApplyTheme(ThemePalette palette)
        {
            BackColor = Color.Transparent;
            ForeColor = _muted ? palette.TextMuted : palette.TextPrimary;
        }
    }

    private static readonly Regex WordBoundary = new("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

    /// <summary> "ShowStatusCondition" -> "Show status condition"; acronyms (PKM, OT, HOME) keep their case. </summary>
    public static string Friendly(string name)
    {
        if (name.Contains(' ')) // already prose (e.g. a PropertyGrid category)
            return name;
        var words = WordBoundary.Split(name.Replace('_', ' '));
        for (int i = 1; i < words.Length; i++)
        {
            if (!(words[i].Length > 1 && words[i].Any(char.IsUpper) && words[i].All(char.IsLetterOrDigit) && !words[i].Any(char.IsLower)))
                words[i] = words[i].ToLowerInvariant();
        }
        return string.Join(' ', words).Replace("Ha x", "HaX").Replace("ha x", "HaX");
    }

    private static string CategoryTitle(string name) => name switch
    {
        "LocalResources" => "Local Resources",
        "Converter" => "Format Conversion",
        "Import" => "Set Import",
        "SlotWrite" => "Slot Writing",
        "SaveLanguage" => "Save Language",
        "Hover" => "Hover Previews",
        "BattleTemplate" => "Showdown Sets",
        "Draw" => "Colors & Highlights",
        "EntityEditor" => "Pokémon Editor",
        "EntityDb" => "Pokémon Database",
        "EncounterDb" => "Encounter Database",
        "MysteryDb" => "Mystery Gift Database",
        "Report" => "Report Grid",
        _ => Friendly(name),
    };

    private static string CategoryGlyph(string name) => name switch
    {
        "Startup" => "",
        "Backup" => "",
        "LocalResources" => "",
        "Legality" => "",
        "Converter" => "",
        "Import" => "",
        "SlotWrite" => "",
        "Privacy" => "",
        "SaveLanguage" => "",
        "Theme" => "",
        "Display" => "",
        "Sprite" => "",
        "Sounds" => "",
        "Hover" => "",
        "BattleTemplate" => "",
        "Draw" => "",
        "EntityEditor" => "",
        "EntityDb" => "",
        "EncounterDb" => "",
        "MysteryDb" => "",
        "Report" => "",
        _ when name.Contains("Legal", StringComparison.OrdinalIgnoreCase) => "",
        _ when name.Contains("Trainer", StringComparison.OrdinalIgnoreCase) => "",
        _ when name.Contains("Export", StringComparison.OrdinalIgnoreCase) || name.Contains("Save", StringComparison.OrdinalIgnoreCase) => "",
        _ when name.Contains("Import", StringComparison.OrdinalIgnoreCase) || name.Contains("Showdown", StringComparison.OrdinalIgnoreCase) => "",
        _ when name.Contains("Display", StringComparison.OrdinalIgnoreCase) || name.Contains("Mark", StringComparison.OrdinalIgnoreCase) => "",
        _ when name.Contains("Connect", StringComparison.OrdinalIgnoreCase) || name.Contains("Live", StringComparison.OrdinalIgnoreCase) => "",
        _ => Icons.Settings,
    };
}
