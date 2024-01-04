//-----------------------------------------------------------------------------
// FILE:	    KeysDialog.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2024 by neonFORGE, LLC.  All rights reserved.
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
using System.Windows.Forms;

namespace RaspberryDebugger.Dialogs
{
    /// <summary>
    /// Edits a connection's SSH key pair.
    /// </summary>
    public partial class KeysDialog : Form
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Converts a string with Linux-style line endings to Windows endings.
        /// </summary>
        /// <param name="value">The inpput value.</param>
        /// <returns>The converted output.</returns>
        private static string ToWindowsLineEndings(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (!value.Contains("\r\n"))
            {
                return value.Replace("\n", "\r\n");
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Converts a string with Windows-style line endings to Linux endings.
        /// </summary>
        /// <param name="value">The inpput value.</param>
        /// <returns>The converted output.</returns>
        private static string ToLinuxLineEndings(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Contains("\r\n"))
            {
                return value.Replace("\t\n", "\n");
            }
            else
            {
                return value;
            }
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeysDialog()
        {
            InitializeComponent();

            this.Load += (s, a) =>
            {
                privateKeyTextBox.Text = ToWindowsLineEndings(PrivateKey.Trim());
                publicKeyTextBox.Text  = ToWindowsLineEndings(PublicKey.Trim());
            };
        }
        
        /// <summary>
        /// The SSK private key.
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// The SSH publick key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Handles OK button clicks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The event arguments.</param>
        private void okButton_Click(object sender, EventArgs args)
        {
            // We're not going to try to validate the keys.

            PrivateKey = ToLinuxLineEndings(privateKeyTextBox.Text.Trim());
            PublicKey  = ToLinuxLineEndings(publicKeyTextBox.Text.Trim());

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Handles Cancel button clicks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The event arguments.</param>
        private void cancelButton_Click(object sender, EventArgs args)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
