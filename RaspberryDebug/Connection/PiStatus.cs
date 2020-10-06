//-----------------------------------------------------------------------------
// FILE:	    PiStatus.cs
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using Neon.Common;
using Neon.Net;
using Neon.SSH;

using Newtonsoft.Json;

namespace RaspberryDebug
{
    /// <summary>
    /// Describes the current status of a remote Raspberry Pi.
    /// </summary>
    internal class PiStatus
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sudoAllowed">Indicates whether <b>sudo</b> is allowed for the user.</param>
        /// <param name="architecture">The chip architecture.</param>
        /// <param name="hasUnzip">Indicates whether <b>unzip</b> is installed.</param>
        /// <param name="debugger">The debugger status.</param>
        /// <param name="installedSdks">The installed .NET Core SDKs.</param>
        public PiStatus(bool sudoAllowed, string architecture, bool hasUnzip, PiDebuggerStatus debugger, IEnumerable<PiSdk> installedSdks)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(architecture), nameof(architecture));
            Covenant.Requires<ArgumentNullException>(installedSdks != null, nameof(installedSdks));

            this.Success       = true;
            this.SudoAllowed   = sudoAllowed;
            this.Architecture  = architecture;
            this.HasUnzip      = hasUnzip;
            this.Debugger      = debugger;
            this.InstalledSdks = installedSdks.ToList().AsReadOnly();
        }

        /// <summary>
        /// Use is constructor when the status was not retrieved successfully.
        /// </summary>
        public PiStatus()
        {
            this.Success = false;
        }

        /// <summary>
        /// Indicates whether the status was retrieved successfully.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Returns <c>true</c> if the Raspberry user can sudo.
        /// </summary>
        public bool SudoAllowed { get; private set; }

        /// <summary>
        /// Returns the chip architecture (like <b>armv71</b>).
        /// </summary>
        public string Architecture { get; private set; }

        /// <summary>
        /// Returns <c>true</c> if <b>unzip</b> is installed on the Raspberry Pi.
        /// This is required and will be installed automatically.
        /// </summary>
        public bool HasUnzip { get; private set; }

        /// <summary>
        /// Indicates the status of the <b>vsdbg</b> debugger.
        /// </summary>
        public PiDebuggerStatus Debugger { get; private set; }

        /// <summary>
        /// Returns information about the .NET Core SDKs installed.
        /// </summary>
        public IList<PiSdk> InstalledSdks { get; private set; }
    }
}
