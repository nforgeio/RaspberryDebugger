//-----------------------------------------------------------------------------
// FILE:	    PiConnectionSettings.cs
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
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;

namespace RaspberryDebug
{
    /// <summary>
    /// Holds the connection settings for a remote Raspberry Pi.
    /// </summary>
    public class PiConnectionSettings
    {
        /// <summary>
        /// The host IP address or DNS name.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The target SSH port;
        /// </summary>
        public int Port { get; set; } = 22;

        /// <summary>
        /// The SSH user name.
        /// </summary>
        public string Username { get; set; } = "pi";

        /// <summary>
        /// The SSH password.
        /// </summary>
        public string Password { get; set; } = "raspberry";
    }
}
