//-----------------------------------------------------------------------------
// FILE:	    SettingsDialog.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Neon.Common;
using Neon.Net;
using System.Diagnostics.Contracts;
using EnvDTE;

namespace RaspberryDebugger
{
    /// <summary>
    /// Allows the user to edit the Rasparry related settings for a project.
    /// </summary>
    public partial class SettingsDialog : Form
    {
        private const string disabledItem = "[debugging disabled]";
        private const string defaultItem  = "[default connection]";

        private ProjectSettings     projectSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="projectSettings">The project settings.</param>
        public SettingsDialog(ProjectSettings projectSettings)
        {
            Covenant.Requires<ArgumentNullException>(projectSettings != null);

            InitializeComponent();

            this.projectSettings = projectSettings;

            // Initialize the combo box with the available connections.

            var connections = PackageHelper.ReadConnections(disableLogging: true);

            targetComboBox.Items.Clear();
            targetComboBox.Items.Add(disabledItem);
            targetComboBox.Items.Add(defaultItem);

            foreach (var connection in connections.OrderBy(connection => connection.SortKey))
            {
                targetComboBox.Items.Add(connection.Name);
            }
        }

        /// <summary>
        /// Handles OK button clicks.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args"The arguments</param>
        private void okButton_Click(object sender, EventArgs args)
        {
            var selectedItem = (string)(targetComboBox.SelectedItem);

            switch (selectedItem)
            {
                case disabledItem:

                    projectSettings.EnableRemoteDebugging = false;
                    break;

                case defaultItem:

                    projectSettings.EnableRemoteDebugging = true;
                    projectSettings.RemoteDebugTarget     = null;   // NULL means default
                    break;

                default:

                    projectSettings.EnableRemoteDebugging = true;
                    projectSettings.RemoteDebugTarget     = selectedItem;
                    break;
            }

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Handles Cancel button clicks.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args"The arguments</param>
        private void cancelButton_Click(object sender, EventArgs args)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
