using System;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHeX.WinForms.Controls;

public partial class FormArgument : UserControl
{
    private bool IsRawMode;
    private ushort CurrentSpecies;
    private byte CurrentForm;
    private EntityContext CurrentContext;
    private bool FieldsLoaded;
    /// <summary> True when the species has a form argument to edit. With nothing to show the (auto-sized) control
    /// drops its minimum size and shrinks away, so the editor row it sits in takes no space. </summary>
    public bool IsControlVisible
    {
        get;
        private set
        {
            field = value;
            base.MinimumSize = value ? _minimum : System.Drawing.Size.Empty;
        }
    }

    // The designer's minimum size only applies while an argument is shown.
    private System.Drawing.Size _minimum;

    public override System.Drawing.Size MinimumSize
    {
        get => base.MinimumSize;
        set
        {
            if (value != System.Drawing.Size.Empty)
                _minimum = value;
            base.MinimumSize = IsControlVisible ? value : System.Drawing.Size.Empty;
        }
    }

    protected override void ScaleControl(System.Drawing.SizeF factor, BoundsSpecified specified)
    {
        base.ScaleControl(factor, specified);
        if (!IsControlVisible) // base only scaled the (empty) active minimum; keep the stored one in step with DPI
            _minimum = new System.Drawing.Size((int)Math.Round(_minimum.Width * factor.Width), (int)Math.Round(_minimum.Height * factor.Height));
    }

    public FormArgument() => InitializeComponent(); // starts collapsed: IsControlVisible is false until an argument loads

    public bool LoadArgument(IFormArgument f, ushort species, byte form, EntityContext context)
    {
        FieldsLoaded = false;
        var max = FormArgumentUtil.GetFormArgumentMax(species, form, context);
        if (max == 0)
        {
            CurrentSpecies = species;
            CurrentForm = form;
            CurrentContext = context;
            NUD_FormArg.Value = CB_FormArg.SelectedIndex = 0;
            CB_FormArg.Visible = false;
            NUD_FormArg.Visible = false;
            FieldsLoaded = true;
            IsControlVisible = false;
            return IsControlVisible;
        }

        bool named = FormConverter.GetFormArgumentIsNamedIndex(species);
        if (named)
        {
            if (CurrentSpecies == species && CurrentForm == form && CurrentContext == context)
            {
                CurrentValue = f.FormArgument;
                FieldsLoaded = true;
                return IsControlVisible;
            }
            IsRawMode = false;

            NUD_FormArg.Visible = false;
            CB_FormArg.Items.Clear();
            var args = FormConverter.GetFormArgumentStrings(species);
            CB_FormArg.Items.AddRange(args);
            CB_FormArg.SelectedIndex = 0;
            CB_FormArg.Visible = true;
            IsControlVisible = true;
        }
        else
        {
            IsRawMode = true;

            CB_FormArg.Visible = false;
            NUD_FormArg.Maximum = max;
            NUD_FormArg.Visible = true;
            IsControlVisible = true;
        }
        CurrentSpecies = species;
        CurrentForm = form;
        CurrentContext = context;

        bool isPair = FormArgumentUtil.IsFormArgumentTypeDateTriple(species, form);
        CurrentValue = isPair ? f.FormArgumentRemain : f.FormArgument;

        FieldsLoaded = true;
        return IsControlVisible;
    }

    public void SaveArgument(IFormArgument f)
    {
        f.ChangeFormArgument(CurrentSpecies, CurrentForm, CurrentContext, CurrentValue);
    }

    private uint CurrentValue
    {
        get => IsRawMode ? (uint)NUD_FormArg.Value : (uint)CB_FormArg.SelectedIndex;
        set
        {
            if (IsRawMode)
                NUD_FormArg.Value = Math.Min(NUD_FormArg.Maximum, value);
            else
                CB_FormArg.SelectedIndex = Math.Min(CB_FormArg.Items.Count - 1, (int)value);
        }
    }

    public event EventHandler? ValueChanged;

    private void CB_FormArg_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (FieldsLoaded)
            ValueChanged?.Invoke(sender, e);
    }

    private void NUD_FormArg_ValueChanged(object sender, EventArgs e)
    {
        if (FieldsLoaded)
            ValueChanged?.Invoke(sender, e);
    }
}
