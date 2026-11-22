using System;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHeX.WinForms.Controls;

public partial class TrainerID : UserControl
{
    public TrainerID() => InitializeComponent();
    public event EventHandler? UpdatedID;

    private bool LoadingFields;

    private int XorFormat;
    private TrainerIDFormat DisplayType { get; set; }
    private ITrainerID32 Trainer = null!;

    /// <summary> Lines the control up with a form grid: the first label spans the label column (left-aligned), so the
    /// first ID box starts where the other fields start. </summary>
    public void AlignToGrid(int labelWidth)
    {
        (_gridLabel, _defaultLabel) = (labelWidth, Label_SID.Width);
        ApplyGridAlignment();
    }

    private int _gridLabel, _defaultLabel;

    // The label order depends on the ID format (Gen 7+ shows SID first), so this re-runs after every reorder.
    private void ApplyGridAlignment()
    {
        if (_gridLabel == 0)
            return;
        bool first = true;
        foreach (var l in FLP.Controls.OfType<Label>())
        {
            l.Width = first ? _gridLabel : _defaultLabel;
            l.TextAlign = first ? System.Drawing.ContentAlignment.MiddleLeft : System.Drawing.ContentAlignment.MiddleRight;
            first = false;
        }
    }

    protected override void ScaleControl(System.Drawing.SizeF factor, BoundsSpecified specified)
    {
        base.ScaleControl(factor, specified);
        _gridLabel = (int)Math.Round(_gridLabel * factor.Width);
        _defaultLabel = (int)Math.Round(_defaultLabel * factor.Width);
    }

    public void UpdateTSV()
    {
        var tsv = GetTSV();
        if (tsv > ushort.MaxValue)
            return;

        string IDstr = $"TSV: {tsv:d4}{Environment.NewLine}{GetAlternateRepresentation(Trainer, DisplayType)}";
        TSVTooltip.SetToolTip(TB_TID, IDstr);
        TSVTooltip.SetToolTip(TB_SID, IDstr);
        TSVTooltip.SetToolTip(TB_TID7, IDstr);
        TSVTooltip.SetToolTip(TB_SID7, IDstr);
    }

    private static string GetAlternateRepresentation(ITrainerID32 tr, TrainerIDFormat format)
    {
        if (format is not TrainerIDFormat.SixteenBit)
            return $"ID: {tr.TID16:D5}/{tr.SID16:D5}";
        var id = tr.ID32;
        return $"G7ID: ({id / 1_000_000:D4}){id % 1_000_000:D6}";
    }

    private uint GetTSV()
    {
        if (DisplayType is TrainerIDFormat.None)
            return uint.MaxValue;
        var xor = (uint)(Trainer.SID16 ^ Trainer.TID16);
        if (XorFormat <= 5)
            return xor >> 3;
        return xor >> 4;
    }

    public void LoadIDValues(ITrainerID32 tr, byte format)
    {
        Trainer = tr;
        var display = tr.GetTrainerIDFormat();
        SetFormat(display, format);
        LoadValues();
    }

    public void UpdateSID() => LoadValues();

    public void LoadInfo(ITrainerInfo info)
    {
        Trainer.TID16 = info.TID16;
        Trainer.SID16 = info.SID16;
        LoadValues();
    }

    private void LoadValues()
    {
        LoadingFields = true;
        if (XorFormat <= 2)
            TB_TID.Text = Trainer.TID16.ToString();
        else if (DisplayType == TrainerIDFormat.SixteenBit)
            LoadTID(Trainer);
        else
            LoadTID7(Trainer);
        LoadingFields = false;
    }

    private void LoadTID(ITrainerID32 tr)
    {
        TB_TID.Text = tr.TID16.ToString(TrainerIDExtensions.TID16);
        TB_SID.Text = tr.SID16.ToString(TrainerIDExtensions.SID16);
    }

    private void LoadTID7(ITrainerID32 tr)
    {
        TB_TID7.Text = tr.GetTrainerTID7().ToString(TrainerIDExtensions.TID7);
        TB_SID7.Text = tr.GetTrainerSID7().ToString(TrainerIDExtensions.SID7);
    }

    private void SetFormat(TrainerIDFormat display, byte format)
    {
        if ((display, format) == (DisplayType, XorFormat))
            return;

        var controls = GetControlsForFormat(display);
        FLP.Controls.Clear(); int i = 0;
        foreach (var c in controls)
        {
            FLP.Controls.Add(c);
            FLP.Controls.SetChildIndex(c, i++); // because you don't listen the first time
        }

        (DisplayType, XorFormat) = (display, format);
        ApplyGridAlignment();
    }

    private Control[] GetControlsForFormat(TrainerIDFormat format) => format switch
    {
        TrainerIDFormat.SixDigit => [Label_SID, TB_SID7, Label_TID, TB_TID7],
        TrainerIDFormat.SixteenBitSingle => [Label_TID, TB_TID], // Gen1/2
        _ => [Label_TID, TB_TID, Label_SID, TB_SID],
    };

    private void UpdateTSV(object sender, EventArgs e) => UpdateTSV();

    private void Update_ID(object sender, EventArgs e)
    {
        if (sender is not MaskedTextBox mt)
            return;

        if (!uint.TryParse(mt.Text, out var value))
            value = 0;
        if (mt == TB_TID7)
        {
            if (value > 999_999)
            {
                mt.Text = "999999";
                return;
            }
            if (!uint.TryParse(TB_SID7.Text, out var sid))
                sid = 0;
            SanityCheckSID7(value, sid);
        }
        else if (mt == TB_SID7)
        {
            if (value > 4294) // max 4 digits of 32bit int
            {
                mt.Text = "4294";
                return;
            }
            if (!uint.TryParse(TB_TID7.Text, out var tid))
                tid = 0;
            SanityCheckSID7(tid, value);
        }
        else
        {
            if (value > ushort.MaxValue) // prior to Gen7
                mt.Text = (value = ushort.MaxValue).ToString();

            if (mt == TB_TID)
                Trainer.TID16 = (ushort)value;
            else
                Trainer.SID16 = (ushort)value;
        }

        UpdatedID?.Invoke(sender, e);
    }

    private void SanityCheckSID7(uint tid, uint sid)
    {
        if (LoadingFields)
            return;

        var repack = ((ulong)sid * 1_000_000) + tid;
        if (repack > uint.MaxValue)
        {
            TB_SID7.Text = (sid - 1).ToString();
            return; // GUI triggers change event, so we'll eventually reach below.
        }
        Trainer.SetTrainerID7(sid, tid);
    }
}
