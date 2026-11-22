namespace PKHeX.WinForms
{
    partial class SettingsEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            B_Reset = new System.Windows.Forms.Button();
            B_Done = new System.Windows.Forms.Button();
            SuspendLayout();
            //
            // B_Reset
            //
            B_Reset.AutoSize = true;
            B_Reset.Name = "B_Reset";
            B_Reset.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            B_Reset.TabIndex = 2;
            B_Reset.Text = "Reset All";
            //
            // B_Done
            //
            B_Done.AutoSize = true;
            B_Done.DialogResult = System.Windows.Forms.DialogResult.OK;
            B_Done.Name = "B_Done";
            B_Done.Padding = new System.Windows.Forms.Padding(16, 2, 16, 2);
            B_Done.TabIndex = 3;
            B_Done.Text = "Done";
            //
            // SettingsEditor
            //
            AcceptButton = B_Done;
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            ClientSize = new System.Drawing.Size(900, 620);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            Icon = Properties.Resources.Icon;
            KeyPreview = true;
            MaximizeBox = true;
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(720, 480);
            Name = "SettingsEditor";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Settings";
            KeyDown += SettingsEditor_KeyDown;
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button B_Reset;
        private System.Windows.Forms.Button B_Done;
    }
}
