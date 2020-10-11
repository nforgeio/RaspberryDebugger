//-----------------------------------------------------------------------------
// FILE:	    ConnectionDialog.cs
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
using BrightIdeasSoftware;
using Neon.Net;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements the Add/Remove connection dialogs.
    /// </summary>
    internal partial class ConnectionDialog : Form
    {
        private const char passwordChar = '•';

        private List<ConnectionInfo>    existingConnections;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionInfo">The information for the connection being created or edited.</param>
        /// <param name="edit">Pass <c>true</c> when editing, <c>false</c> for creating a new connection.</param>
        /// <param name="existingConnections">The existing connection.</param>
        public ConnectionDialog(ConnectionInfo connectionInfo, bool edit, List<ConnectionInfo> existingConnections)
        {
            InitializeComponent();

            this.ConnectionInfo      = connectionInfo;
            this.Text                = edit ? "Edit Connection" : "New Connection";
            this.existingConnections = existingConnections;

            // Initialize the controls on load.

            this.Load += (s, a) =>
            {
                hostTextBox.Text             = connectionInfo.Host;
                portTextBox.Text             = connectionInfo.Port.ToString();
                userTextBox.Text             = connectionInfo.User;
                passwordTextBox.Text         = connectionInfo.Password;
                passwordTextBox.PasswordChar = passwordChar;
                showPasswordCheckBox.Checked = false;
                instructionsTextBox.Visible  = string.IsNullOrEmpty(connectionInfo.PrivateKeyPath);
            };
        }

        /// <summary>
        /// Returns the connection being created or edited.
        /// </summary>
        internal ConnectionInfo ConnectionInfo { get; private set; }

        /// <summary>
        /// Handles the OK button.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
#pragma warning disable VSTHRD100
        private async void okButton_Click(object sender, EventArgs args)
#pragma warning restore VSTHRD100
        {
            //-----------------------------------------------------------------
            // Validate the host

            var hostText = hostTextBox.Text.Trim();

            if (hostText == string.Empty)
            {
                hostTextBox.Focus();
                hostTextBox.SelectAll();
                MessageBoxEx.Show(this, "You must specify a host name or IP address.", "Error", MessageBoxButtons.OK);
                return;
            }

            if (!IPAddress.TryParse(hostText, out var address) && !NetHelper.IsValidHost(hostText))
            {
                portTextBox.Focus();
                portTextBox.SelectAll();
                MessageBoxEx.Show(this, $"[{hostText}] is not a valid IPv4 address or host name.", "Error", MessageBoxButtons.OK);
                return;
            }

            if (existingConnections.Any(connection => connection != this.ConnectionInfo && connection.Host == hostText))
            {
                portTextBox.Focus();
                portTextBox.SelectAll();
                MessageBoxEx.Show(this, $"[{hostText}] is already referenced by another connection.", "Error", MessageBoxButtons.OK);
                return;
            }

            //-----------------------------------------------------------------
            // Validate the SSH port.

            var portText = portTextBox.Text.Trim();

            if (portText == string.Empty)
            {
                portTextBox.Focus();
                portTextBox.SelectAll();
                MessageBoxEx.Show(this, "You must specify the SSH port.", "Error", MessageBoxButtons.OK);
                return;
            }

            if (!int.TryParse(portText, out var port) || !NetHelper.IsValidPort(port))
            {
                portTextBox.Focus();
                portTextBox.SelectAll();
                MessageBoxEx.Show(this, $"[{portText}] is not a valid SSH port.", "Error", MessageBoxButtons.OK);
                return;
            }

            //-----------------------------------------------------------------
            // Validate the username.

            var userText = userTextBox.Text.Trim();

            if (userText == string.Empty)
            {
                userTextBox.Focus();
                userTextBox.SelectAll();
                MessageBoxEx.Show(this, "You must specify a username.", "Error", MessageBoxButtons.OK);
                return;
            }

            //-----------------------------------------------------------------
            // Validate the password.

            var passwordText = passwordTextBox.Text.Trim();

            if (passwordText == string.Empty)
            {
                passwordTextBox.Focus();
                passwordTextBox.SelectAll();
                MessageBoxEx.Show(this, "You must specify a password.", "Error", MessageBoxButtons.OK);
                return;
            }

            //-----------------------------------------------------------------
            // The properties look OK, so establish a connection to verify.

            var testConnectionInfo = new ConnectionInfo()
            {
                Host           = hostText,
                Port           = port,
                User           = userText,
                Password       = passwordText,
                PrivateKeyPath = ConnectionInfo.PrivateKeyPath,
                PublicKeyPath  = ConnectionInfo.PublicKeyPath
            };

            try
            {
                using (await Connection.ConnectAsync(testConnectionInfo))
                {
                }
            }
            catch
            {
                MessageBoxEx.Show(
                    this,
                    $"Unable to connect to: {hostText}\r\n\r\nMake sure your Raspberry is turned on and verify your username and password.",
                    "Connection Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            //-----------------------------------------------------------------
            // Everything looks good, so update the connection and return.

            ConnectionInfo.Host           = hostText;
            ConnectionInfo.Port           = port;
            ConnectionInfo.User           = userText;
            ConnectionInfo.Password       = passwordText;
            ConnectionInfo.PrivateKeyPath = testConnectionInfo.PrivateKeyPath;
            ConnectionInfo.PublicKeyPath  = testConnectionInfo.PublicKeyPath;

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Handles the CANCEL button.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void cancelButton_Click(object sender, EventArgs args)
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Shows/hides the password.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void showPasswordCheckBox_CheckedChanged(object sender, EventArgs args)
        {
            passwordTextBox.PasswordChar = showPasswordCheckBox.Checked ? (char)0 : passwordChar;
        }

        /// <summary>
        /// Handles Keys button clicks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void keysButton_Click(object sender, EventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
