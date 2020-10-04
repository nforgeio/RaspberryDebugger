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
    partial class PiDebugConnectionsPanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// The related Visual Studio options page.
        /// </summary>
        public PiDebugConnectionsPage OptionsPage { get; set; }

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
            this.connectionsList = new System.Windows.Forms.ListView();
            this.addButton = new System.Windows.Forms.Button();
            this.editButton = new System.Windows.Forms.Button();
            this.testButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Location = new System.Drawing.Point(3, 2);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(298, 13);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Add or Remove SSH connections for Raspberry Pi debugging";
            // 
            // connectionsList
            // 
            this.connectionsList.HideSelection = false;
            this.connectionsList.Location = new System.Drawing.Point(3, 21);
            this.connectionsList.Name = "connectionsList";
            this.connectionsList.Size = new System.Drawing.Size(408, 329);
            this.connectionsList.TabIndex = 1;
            this.connectionsList.UseCompatibleStateImageBehavior = false;
            // 
            // addButton
            // 
            this.addButton.Location = new System.Drawing.Point(417, 21);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(75, 23);
            this.addButton.TabIndex = 2;
            this.addButton.Text = "Add";
            this.addButton.UseVisualStyleBackColor = true;
            // 
            // editButton
            // 
            this.editButton.Location = new System.Drawing.Point(417, 50);
            this.editButton.Name = "editButton";
            this.editButton.Size = new System.Drawing.Size(75, 23);
            this.editButton.TabIndex = 3;
            this.editButton.Text = "Edit";
            this.editButton.UseVisualStyleBackColor = true;
            // 
            // testButton
            // 
            this.testButton.Location = new System.Drawing.Point(417, 79);
            this.testButton.Name = "testButton";
            this.testButton.Size = new System.Drawing.Size(75, 23);
            this.testButton.TabIndex = 4;
            this.testButton.Text = "Test";
            this.testButton.UseVisualStyleBackColor = true;
            // 
            // removeButton
            // 
            this.removeButton.Location = new System.Drawing.Point(417, 108);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(75, 23);
            this.removeButton.TabIndex = 5;
            this.removeButton.Text = "Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            // 
            // PiDebugConnectionsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.testButton);
            this.Controls.Add(this.editButton);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.connectionsList);
            this.Controls.Add(this.titleLabel);
            this.Name = "PiDebugConnectionsPanel";
            this.Size = new System.Drawing.Size(505, 404);
            this.Load += new System.EventHandler(this.PiDebugOptionsPanel_Load);
            this.SizeChanged += new System.EventHandler(this.PiDebugOptionsPanel_SizeChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.ListView connectionsList;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button editButton;
        private System.Windows.Forms.Button testButton;
        private System.Windows.Forms.Button removeButton;
    }
}
