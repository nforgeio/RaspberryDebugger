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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements the debug connections options panel.
    /// </summary>
    public partial class PiDebugConnectionsPanel : UserControl
    {
        private const int spacing = 8;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PiDebugConnectionsPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when the panel is first loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void PiDebugOptionsPanel_Load(object sender, EventArgs args)
        {
            // Ensure that the controls are layed out correctly for the initial size.

            PiDebugOptionsPanel_SizeChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Calleds when the panel is resized.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void PiDebugOptionsPanel_SizeChanged(object sender, EventArgs args)
        {
            var buttonLeft = this.Width - addButton.Width - spacing;

            addButton.Left    = buttonLeft;
            editButton.Left   = buttonLeft;
            testButton.Left   = buttonLeft;
            removeButton.Left = buttonLeft;

            titleLabel.Left = spacing;

            connectionsList.Left   = spacing;
            connectionsList.Height = this.Height - connectionsList.Top;
            connectionsList.Width  = this.Width - addButton.Width - 4 * spacing;
        }
    }
}
