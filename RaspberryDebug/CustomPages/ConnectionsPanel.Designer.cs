//-----------------------------------------------------------------------------
// FILE:	    PiDebugConnectionsPanel.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Open Source
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel;
using System.Net.NetworkInformation;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements the custom debug connections options panel.
    /// </summary>
    partial class ConnectionsPanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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

        /// <summary>
        /// Called by the related options ppage to load the current settings
        /// into this control.
        /// </summary>
        public void Initialize()
        {
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.addButton = new System.Windows.Forms.Button();
            this.editButton = new System.Windows.Forms.Button();
            this.verifyButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.connectionsView = new BrightIdeasSoftware.ObjectListView();
            ((System.ComponentModel.ISupportInitialize)(this.connectionsView)).BeginInit();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Location = new System.Drawing.Point(3, 2);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(263, 13);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Manage SSH connections for Raspberry Pi debugging";
            // 
            // addButton
            // 
            this.addButton.Location = new System.Drawing.Point(417, 21);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(75, 23);
            this.addButton.TabIndex = 2;
            this.addButton.Text = "Add";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // editButton
            // 
            this.editButton.Location = new System.Drawing.Point(417, 50);
            this.editButton.Name = "editButton";
            this.editButton.Size = new System.Drawing.Size(75, 23);
            this.editButton.TabIndex = 3;
            this.editButton.Text = "Edit";
            this.editButton.UseVisualStyleBackColor = true;
            this.editButton.Click += new System.EventHandler(this.editButton_Click);
            // 
            // verifyButton
            // 
            this.verifyButton.Location = new System.Drawing.Point(417, 79);
            this.verifyButton.Name = "verifyButton";
            this.verifyButton.Size = new System.Drawing.Size(75, 23);
            this.verifyButton.TabIndex = 4;
            this.verifyButton.Text = "Verify";
            this.verifyButton.UseVisualStyleBackColor = true;
            this.verifyButton.Click += new System.EventHandler(this.verifyButton_Click);
            // 
            // removeButton
            // 
            this.removeButton.Location = new System.Drawing.Point(417, 108);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(75, 23);
            this.removeButton.TabIndex = 5;
            this.removeButton.Text = "Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // connectionsView
            // 
            this.connectionsView.CellEditUseWholeCell = false;
            this.connectionsView.CheckBoxes = true;
            this.connectionsView.CopySelectionOnControlC = false;
            this.connectionsView.CopySelectionOnControlCUsesDragSource = false;
            this.connectionsView.Cursor = System.Windows.Forms.Cursors.Default;
            this.connectionsView.EmptyListMsg = "Click ADD to specify a target Raspberry Pi for debugging.";
            this.connectionsView.EmptyListMsgFont = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.connectionsView.FullRowSelect = true;
            this.connectionsView.GridLines = true;
            this.connectionsView.HasCollapsibleGroups = false;
            this.connectionsView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.connectionsView.HideSelection = false;
            this.connectionsView.Location = new System.Drawing.Point(6, 18);
            this.connectionsView.MultiSelect = false;
            this.connectionsView.Name = "connectionsView";
            this.connectionsView.ShowSortIndicators = false;
            this.connectionsView.Size = new System.Drawing.Size(405, 337);
            this.connectionsView.TabIndex = 1;
            this.connectionsView.UseCompatibleStateImageBehavior = false;
            this.connectionsView.View = System.Windows.Forms.View.Details;
            this.connectionsView.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.connectionsView_ColumnWidthChanging);
            this.connectionsView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.connectionsView_MouseDoubleClick);
            // 
            // ConnectionsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.connectionsView);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.verifyButton);
            this.Controls.Add(this.editButton);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.titleLabel);
            this.Name = "ConnectionsPanel";
            this.Size = new System.Drawing.Size(505, 367);
            this.Load += new System.EventHandler(this.PiDebugOptionsPanel_Load);
            this.SizeChanged += new System.EventHandler(this.OptionsPanel_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.connectionsView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button editButton;
        private System.Windows.Forms.Button verifyButton;
        private System.Windows.Forms.Button removeButton;
        private BrightIdeasSoftware.ObjectListView connectionsView;
    }
}
