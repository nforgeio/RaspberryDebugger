//-----------------------------------------------------------------------------
// FILE:	    SettingsDialog.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2020 by neonFORGE, LLC.  All rights reserved.
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
using System.Text.RegularExpressions;

namespace RaspberryDebugger
{
    /// <summary>
    /// Allows the user to edit the Rasparry related settings for a project.
    /// </summary>
    internal partial class SettingsDialog : Form
    {
        private ProjectSettings             projectSettings;
        private Dictionary<string, int>     connectionNameToIndex;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="projectSettings">The project settings.</param>
        public SettingsDialog(ProjectSettings projectSettings)
        {
            Covenant.Requires<ArgumentNullException>(projectSettings != null);

            this.projectSettings = projectSettings;

            InitializeComponent();

            // The instructions include "\r\n" sequences that need to be replaced with
            // actual CR/LF characters.

            instructionsTextBox.Text = Regex.Unescape(instructionsTextBox.Text);

            // Initialize the combo box with the available connections and select
            // the current one.

            connectionNameToIndex = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

            var connections = PackageHelper.ReadConnections(disableLogging: true);
            var index       = 0;

            targetComboBox.Items.Clear();

            targetComboBox.Items.Add(ProjectSettings.DisabledConnectionName);
            connectionNameToIndex.Add(ProjectSettings.DisabledConnectionName, index++);

            targetComboBox.Items.Add(ProjectSettings.DefaultConnectionName);
            connectionNameToIndex.Add(ProjectSettings.DefaultConnectionName, index++);

            foreach (var connection in connections.OrderBy(connection => connection.SortKey))
            {
                targetComboBox.Items.Add(connection.Name);
                connectionNameToIndex.Add(connection.Name, index++);
            }

            if (!projectSettings.EnableRemoteDebugging)
            {
                targetComboBox.SelectedIndex = connectionNameToIndex[ProjectSettings.DisabledConnectionName];
            }
            else
            {
                // If the connection named in the settings exists select it,
                // otherwise select the default.

                var selectedConnection = connections.FirstOrDefault(connection => connection.Name.Equals(projectSettings.RemoteDebugTarget, StringComparison.InvariantCultureIgnoreCase));

                if (projectSettings.RemoteDebugTarget == null || selectedConnection == null)
                {
                    targetComboBox.SelectedIndex = connectionNameToIndex[ProjectSettings.DefaultConnectionName];
                }
                else
                {
                    targetComboBox.SelectedIndex = connectionNameToIndex[selectedConnection.Name];
                }
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
                case ProjectSettings.DisabledConnectionName:

                    projectSettings.EnableRemoteDebugging = false;
                    break;

                case ProjectSettings.DefaultConnectionName:

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
