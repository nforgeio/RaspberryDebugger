//-----------------------------------------------------------------------------
// FILE:	    ConnectionsPanel.cs
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualStudio.Threading;

using BrightIdeasSoftware;
using Neon.Common;

namespace RaspberryDebugger
{
    /// <summary>
    /// Implements the debug connections options panel.
    /// </summary>
    internal partial class ConnectionsPanel : UserControl
    {
        private const int spacing       = 8;

        private const int defaultColumn = 1;
        private const int hostColumn    = 2;
        private const int portColumn    = 3;
        private const int userColumn    = 4;
        private const int authColumn    = 5;
        private const int blankColumn   = 6;

        private bool                    isInitialized = false;
        private List<ConnectionInfo>    connections;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ConnectionsPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The related Visual Studio connections options page.
        /// </summary>
        internal ConnectionsPage ConnectionsPage { get; set; }

        /// <summary>
        /// Returns the parent window to be used when displaying message boxes
        /// and dialogs.
        /// </summary>
        private IWin32Window dialogParent => ConnectionsPage.PanelWindow;

        /// <summary>
        /// Returns the selected connection or <c>null</c>.
        /// </summary>
        private ConnectionInfo SelectedConnection => (ConnectionInfo)connectionsView.SelectedObject;

        /// <summary>
        /// Called when the panel is first loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void PiDebugOptionsPanel_Load(object sender, EventArgs args)
        {
            // Ensure that the controls are laid out correctly for the initial panel size.

            OptionsPanel_SizeChanged(this, EventArgs.Empty);

            // We need to update the button enable/disable state when the connections
            // view selection changes.

            connectionsView.SelectedIndexChanged += (s, a) => EnableButtons();

            // Initialize the list view columns.

            if (!isInitialized)
            {
                // This crashes is called when the options panel is displayed for a second
                // time in Visual Studio.  VS must cache the instance or something and then
                // reload it.  We'll use a static variable to avoid this.
                //
                //      https://github.com/nforgeio/RaspberryDebugger/issues/4

                connectionsView.CheckBoxes = true;
                isInitialized              = true;

                connectionsView.CheckedAspectName = nameof(ConnectionInfo.IsDefault);

                connectionsView.Columns.Add(
                    new OLVColumn()
                    {
                        Name            = "Default",
                        Text            = "Default",
                        DisplayIndex    = defaultColumn,
                        Width           = 60,
                        HeaderTextAlign = HorizontalAlignment.Center,
                        TextAlign       = HorizontalAlignment.Center,
                        Sortable        = false
                    });

                connectionsView.Columns.Add(
                    new OLVColumn()
                    {
                        Name            = "Host",
                        Text            = "Host",
                        AspectName      = nameof(ConnectionInfo.Name),
                        DisplayIndex    = hostColumn,
                        Width           = 200,
                        HeaderTextAlign = HorizontalAlignment.Left,
                        TextAlign       = HorizontalAlignment.Left,
                        Sortable        = false
                    });

                connectionsView.Columns.Add(
                    new OLVColumn()
                    {
                        Name            = "Port",
                        Text            = "Port",
                        AspectName      = nameof(ConnectionInfo.Port),
                        DisplayIndex    = portColumn,
                        Width           = 60,
                        HeaderTextAlign = HorizontalAlignment.Center,
                        TextAlign       = HorizontalAlignment.Center,
                        Sortable        = false
                    });

                connectionsView.Columns.Add(
                    new OLVColumn()
                    {
                        Name            = "User",
                        Text            = "User",
                        AspectName      = nameof(ConnectionInfo.User),
                        DisplayIndex    = userColumn,
                        FillsFreeSpace  = true,
                        MaximumWidth    = 500,
                        HeaderTextAlign = HorizontalAlignment.Left,
                        TextAlign       = HorizontalAlignment.Left,
                        Sortable        = false
                    });

                connectionsView.Columns.Add(
                    new OLVColumn()
                    {
                        Name            = "Authentication",
                        Text            = "Authentication",
                        AspectName      = nameof(ConnectionInfo.Authentication),
                        DisplayIndex    = authColumn,
                        Width           = 100,
                        HeaderTextAlign = HorizontalAlignment.Center,
                        TextAlign       = HorizontalAlignment.Center,
                        Sortable        = false
                    });

                connectionsView.Columns.Add(
                    new OLVColumn()
                    {
                        Name            = "",
                        Text            = "",
                        DisplayIndex    = blankColumn,
                        Width           = 0,
                        IsVisible       = false,
                        HeaderTextAlign = HorizontalAlignment.Left,
                        TextAlign       = HorizontalAlignment.Left,
                        Sortable        = false
                    });
            }

            // Load the connections from the state persisted by Visual Studio.

            LoadConnections();
        }

        /// <summary>
        /// Reads the Raspberry Pi connections persisted to Visual Studio
        /// and loads them into the list view.
        /// </summary>
        private void LoadConnections()
        {
            connections = PackageHelper.ReadConnections(disableLogging: true);

            // All connections must reference this panel so they can notify
            // when their [IsDefault] check state changes.

            foreach (var connection in connections)
            {
                connection.ConnectionsPanel = this;
            }

            // Load the connections into the view and enable/disable the buttons.

            connectionsView.SetObjects(connections);
            EnableButtons();
        }

        /// <summary>
        /// Reloads the connections into the connection view maintaining the selection
        /// if the current connection still exists.
        /// </summary>
        private void ReloadConnections()
        {
            var orgName = ((ConnectionInfo)connectionsView.SelectedObject)?.Name;

            connections = PackageHelper.ReadConnections(disableLogging: true);

            connectionsView.SetObjects(connections);
            connectionsView.SelectedObject = connections.SingleOrDefault(connection => connection.Name == orgName);

            EnableButtons();
        }

        /// <summary>
        /// Persists the connections back to Visual Studio.
        /// </summary>
        private void SaveConnections()
        {
            PackageHelper.WriteConnections(connections, disableLogging: true);
        }

        /// <summary>
        /// Called when the panel is resized.  We need to relocate and resize some of
        /// the controls to fit the new size.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void OptionsPanel_SizeChanged(object sender, EventArgs args)
        {
            var buttonLeft = this.Width - addButton.Width - spacing;

            addButton.Left         = buttonLeft;
            editButton.Left        = buttonLeft;
            verifyButton.Left        = buttonLeft;
            removeButton.Left      = buttonLeft;

            titleLabel.Left        = spacing;

            connectionsView.Left   = spacing;
            connectionsView.Height = this.Height - connectionsView.Top;
            connectionsView.Width  = this.Width - addButton.Width - 4 * spacing;
        }

        /// <summary>
        /// Disables user column width resizing. 
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void connectionsView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs args)
        {
            // Don't allow the user to resize view columns.

            args.Cancel   = true;
            args.NewWidth = connectionsView.Columns[args.ColumnIndex].Width;
        }

        /// <summary>
        /// Handles check box changes by ensuring that only one connection is checked as default at a time.
        /// </summary>
        /// <param name="changedConnection">The changed connection.</param>
        /// <remarks>
        /// This is a bit of hack because the <see cref="ObjectListView"/> control doesn't
        /// appear to have a check box changed event.
        /// </remarks>
        internal void ConnectionIsDefaultChanged(ConnectionInfo changedConnection)
        {
            if (changedConnection.IsDefault)
            {
                var uncheckThese = new List<ConnectionInfo>();

                foreach (var connection in connections.Where(connection => connection != changedConnection))
                {
                    if (connectionsView.IsChecked(connection))
                    {
                        uncheckThese.Add(connection);
                    }
                }

                if (uncheckThese.Count > 0)
                {
                    connectionsView.UncheckObjects(uncheckThese);
                }

                changedConnection.IsDefault = true;

                foreach (var connection in uncheckThese)
                {
                    connection.IsDefault = false;
                }
            }
            else
            {
                changedConnection.IsDefault = false;
            }

            SaveConnections();
        }

        /// <summary>
        /// Enables or disables the buttons as necessary based on whether there's a selected connection.
        /// </summary>
        private void EnableButtons()
        {
            var connectionSelected = SelectedConnection != null;

            addButton.Enabled    = true;
            editButton.Enabled   = 
            verifyButton.Enabled = 
            removeButton.Enabled = connectionSelected;
        }

        /// <summary>
        /// Handles <b>Add</b> button clicks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void addButton_Click(object sender, EventArgs args)
        {
            var newConnection =
                new ConnectionInfo()
                {
                    ConnectionsPanel = this
                };

            var connectionDialog = new ConnectionDialog(newConnection, edit: false, connections);

            if (connectionDialog.ShowDialog(dialogParent) == DialogResult.OK)
            {
                connections.Add(newConnection);
                SaveConnections();
                ReloadConnections();
            }
        }

        /// <summary>
        /// Handles <b>Edit</b> button clicks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void editButton_Click(object sender, EventArgs args)
        {
            if (SelectedConnection == null)
            {
                return;
            }

            var connectionDialog = new ConnectionDialog(SelectedConnection, edit: true, connections);

            if (connectionDialog.ShowDialog(dialogParent) == DialogResult.OK)
            {
                SaveConnections();
                ReloadConnections();
            }
        }

        /// <summary>
        /// Verifies the connection settings by establishing a connection to the Raspberry Pi.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
#pragma warning disable VSTHRD100
        private async void verifyButton_Click(object sender, EventArgs args)
#pragma warning restore VSTHRD100
        {
            if (SelectedConnection == null)
            {
                return;
            }

            Log.Info($"[{SelectedConnection.Name}]: Verify Connection");

            var currentConnection = SelectedConnection;
            var exception         = (Exception)null;

            await PackageHelper.ExecuteWithProgressAsync("Verify connection", async () =>
            {
                try
                {
                    using (var connection = await Connection.ConnectAsync(currentConnection))
                    {
                        exception = null;
                    }
                }
                catch (Exception e)
                {
                    exception = e;

                    Log.Exception(e);
                }
            });

            if (exception == null)
            {
                // Reload the connections to pick any changes (like adding the SSH key).

                ReloadConnections();

                MessageBox.Show(this,
                                $"[{SelectedConnection.Name}] Connection is OK!",
                                $"Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this,
                                $"Connection Failed:\r\n\r\n{exception.GetType().FullName}\r\n{exception.Message}\r\n\r\nView the Debug Output for more details.",
                                $"Connection Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles <b>Remove</b> button clicks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void removeButton_Click(object sender, EventArgs args)
        {
            if (SelectedConnection == null)
            {
                return;
            }

            if (MessageBoxEx.Show(this,
                                  $"Delete the debug connection for [{SelectedConnection.Name}]?",
                                  $"Delete Connection",
                                  MessageBoxButtons.YesNo,
                                  MessageBoxIcon.Question,
                                  MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                connections.Remove(SelectedConnection);
                SaveConnections();
                ReloadConnections();
            }
        }

        /// <summary>
        /// We'll display the edit dialog when an item is double-clicked. 
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void connectionsView_MouseDoubleClick(object sender, MouseEventArgs args)
        {
            editButton_Click(sender, EventArgs.Empty);
        }
    }
}
