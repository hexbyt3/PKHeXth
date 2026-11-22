using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using PKHeX.Core;
using PKHeX.WinForms.Controls;
using PKHeX.WinForms.Theming;

namespace PKHeX.WinForms;

/// <summary>
/// Settings window: a <see cref="SettingsView"/> over the settings object plus a footer (Reset all, Done).
/// </summary>
public partial class SettingsEditor : Form
{
    public bool BlankChanged { get; private set; }

    public SettingsEditor(object obj)
    {
        InitializeComponent();

        var view = new SettingsView(obj);
        if (obj is PKHeXSettings s)
        {
            view.AddCustomRow(s.Startup, WinFormsTranslator.Tr("SettingsTitle.BlankSaveVersion", "Blank save version"),
                WinFormsTranslator.Tr("LocalizedDescription.BlankSaveVersion", "Game used for the empty save created when PKHeX starts without a save file."), () => CreateBlankEditor(s));
            B_Reset.Click += (_, _) => DeleteSettings();
        }
        else
        {
            B_Reset.Visible = false;
        }

        CancelButton = B_Done;
        B_Done.Tag = ThemeWalker.PrimaryTag;
        var footer = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = LogicalToDeviceUnits(60), ColumnCount = 3, RowCount = 1, Padding = new Padding(16, 12, 16, 12) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        B_Reset.Anchor = AnchorStyles.Left;
        B_Done.Anchor = AnchorStyles.Right;
        footer.Controls.Add(B_Reset, 0, 0);
        footer.Controls.Add(B_Done, 2, 0);
        footer.Paint += (_, e) =>
        {
            using var pen = new System.Drawing.Pen(Theme.Current.Border);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        Controls.Add(view);
        Controls.Add(footer);
        WinFormsUtil.TranslateInterface(this, Main.CurrentLanguage);
        this.CenterToForm(FindForm());
    }

    private ComboBox CreateBlankEditor(PKHeXSettings s)
    {
        static bool IsInvalid(GameVersion value) => value is 0 or GameVersion.GO or GameVersion.CP;
        var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = LogicalToDeviceUnits(220), Name = "CB_Blank", BindingContext = BindingContext };
        cb.InitializeBinding();
        cb.DataSource = GameInfo.Sources.VersionDataSource.Where(z => !IsInvalid((GameVersion)z.Value)).ToList();
        cb.SelectedValue = (int)s.Startup.DefaultSaveVersion;
        cb.HandleCreated += (_, _) => cb.SelectedValue = (int)s.Startup.DefaultSaveVersion; // binding resets the pick when it attaches
        // Only user picks count: data binding can raise value changes on its own, and BlankChanged reloads the save.
        cb.SelectionChangeCommitted += (_, _) =>
        {
            var version = (GameVersion)WinFormsUtil.GetIndex(cb);
            if (IsInvalid(version) || version == s.Startup.DefaultSaveVersion)
                return;
            s.Startup.DefaultSaveVersion = version;
            BlankChanged = true;
        };
        return cb;
    }

    private void SettingsEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
            Close();
    }

    private static void DeleteSettings()
    {
        try
        {
            var dr = WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Resetting settings requires the program to exit.", MessageStrings.MsgContinue);
            if (dr != DialogResult.Yes)
                return;
            var path = Program.PathConfig;
            if (File.Exists(path))
                File.Delete(path);
            System.Diagnostics.Process.Start(Application.ExecutablePath);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error("Failed to delete settings.", ex.Message);
        }
    }
}
