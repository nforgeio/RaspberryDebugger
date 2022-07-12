namespace RaspberryDebugger.Dialogs
{
    partial class SettingsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsDialog));
            this.targetLabel = new System.Windows.Forms.Label();
            this.targetComboBox = new System.Windows.Forms.ComboBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.instructionsTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.targetGroup = new System.Windows.Forms.TextBox();
            this.internalProxyLabel = new System.Windows.Forms.Label();
            this.internalProxyCheck = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // targetLabel
            // 
            this.targetLabel.AutoSize = true;
            this.targetLabel.Location = new System.Drawing.Point(16, 21);
            this.targetLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.targetLabel.Name = "targetLabel";
            this.targetLabel.Size = new System.Drawing.Size(117, 16);
            this.targetLabel.TabIndex = 1;
            this.targetLabel.Text = "Target Raspberry:";
            // 
            // targetComboBox
            // 
            this.targetComboBox.FormattingEnabled = true;
            this.targetComboBox.Location = new System.Drawing.Point(141, 17);
            this.targetComboBox.Margin = new System.Windows.Forms.Padding(4);
            this.targetComboBox.Name = "targetComboBox";
            this.targetComboBox.Size = new System.Drawing.Size(317, 24);
            this.targetComboBox.TabIndex = 2;
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(484, 16);
            this.okButton.Margin = new System.Windows.Forms.Padding(4);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(100, 28);
            this.okButton.TabIndex = 6;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(484, 54);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 28);
            this.cancelButton.TabIndex = 7;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // instructionsTextBox
            // 
            this.instructionsTextBox.Location = new System.Drawing.Point(19, 149);
            this.instructionsTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.instructionsTextBox.Multiline = true;
            this.instructionsTextBox.Name = "instructionsTextBox";
            this.instructionsTextBox.ReadOnly = true;
            this.instructionsTextBox.Size = new System.Drawing.Size(439, 104);
            this.instructionsTextBox.TabIndex = 5;
            this.instructionsTextBox.Text = resources.GetString("instructionsTextBox.Text");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(43, 57);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "Target Group:";
            // 
            // targetGroup
            // 
            this.targetGroup.Location = new System.Drawing.Point(141, 57);
            this.targetGroup.Margin = new System.Windows.Forms.Padding(4);
            this.targetGroup.Name = "targetGroup";
            this.targetGroup.Size = new System.Drawing.Size(132, 22);
            this.targetGroup.TabIndex = 4;
            // 
            // internalProxyLabel
            // 
            this.internalProxyLabel.AutoSize = true;
            this.internalProxyLabel.Location = new System.Drawing.Point(15, 90);
            this.internalProxyLabel.Name = "internalProxyLabel";
            this.internalProxyLabel.Size = new System.Drawing.Size(118, 16);
            this.internalProxyLabel.TabIndex = 8;
            this.internalProxyLabel.Text = "Use Internal Proxy:";
            // 
            // internalProxyCheck
            // 
            this.internalProxyCheck.AutoSize = true;
            this.internalProxyCheck.Location = new System.Drawing.Point(141, 89);
            this.internalProxyCheck.Name = "internalProxyCheck";
            this.internalProxyCheck.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.internalProxyCheck.Size = new System.Drawing.Size(18, 17);
            this.internalProxyCheck.TabIndex = 9;
            this.internalProxyCheck.UseVisualStyleBackColor = true;
            // 
            // SettingsDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(604, 289);
            this.Controls.Add(this.internalProxyCheck);
            this.Controls.Add(this.internalProxyLabel);
            this.Controls.Add(this.targetGroup);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.instructionsTextBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.targetComboBox);
            this.Controls.Add(this.targetLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Raspberry Debug Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label targetLabel;
        private System.Windows.Forms.ComboBox targetComboBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox instructionsTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox targetGroup;
        private System.Windows.Forms.Label internalProxyLabel;
        private System.Windows.Forms.CheckBox internalProxyCheck;
    }
}