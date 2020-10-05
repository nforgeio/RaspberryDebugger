//-----------------------------------------------------------------------------
// FILE:	    PiDebugConnectionsPage.cs
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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using YamlDotNet.Serialization.NamingConventions;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements our custom debug connections options page.
    /// </summary>
    [Guid("00000000-0000-0000-0000-000000000000")]
    internal class PiDebugConnectionsPage : DialogPage
    {
        /// <summary>
        /// Constructs and returns the custom control used to implement this options page.
        /// </summary>
        protected override IWin32Window Window
        {
            get
            {
                var panel = new PiDebugConnectionsPanel();

                panel.ConnectionsPage = this;
                panel.Initialize();

                return panel;
            }
        }

        /// <summary>
        /// Returns the panel window that can be used for ensuring that
        /// dialogs and message boxes are properly located over Visual
        /// Studio.
        /// </summary>
        public IWin32Window PanelWindow => Window;
    }
}
