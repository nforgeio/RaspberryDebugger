//-----------------------------------------------------------------------------
// FILE:	    DebugOptionsPage.cs
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
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements our custom debug options page.
    /// </summary>
    public class DebugOptionsPage : DialogPage
    {
        /// <summary>
        /// IP address or host name
        /// </summary>
        [Category("Target Raspberry")]
        [DisplayName("1: IP address or host name")]
        [Description("Target Raspberry IP address or host name")]
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// SSH port
        /// </summary>
        [Category("Target Raspberry")]
        [DisplayName("2: Port")]
        [Description("Target Raspberry Pi SSH port")]
        public int Port { get; set; } = 22;

        /// <summary>
        /// Username
        /// </summary>
        [Category("Target Raspberry")]
        [DisplayName("3: Username")]
        [Description("Raspberry Pi username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Password
        /// </summary>
        [Category("Target Raspberry")]
        [DisplayName("4: Password")]
        [Description("User password password")]
        public string Password { get; set; } = string.Empty;
    }
}
