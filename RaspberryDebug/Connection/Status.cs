//-----------------------------------------------------------------------------
// FILE:	    Status.cs
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
    internal class Status
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="architecture">The chip architecture.</param>
        /// <param name="hasUnzip">Indicates whether <b>unzip</b> is installed.</param>
        /// <param name="hasDebuffer">Indicates whether the debugger is installed.</param>
        /// <param name="installedSdks">The installed .NET Core SDKs.</param>
        /// <param name="path">The current value of the PATH environment variable.</param>
        public Status(string architecture, string path, bool hasUnzip, bool hasDebugger, IEnumerable<Sdk> installedSdks)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(architecture), nameof(architecture));
            Covenant.Requires<ArgumentNullException>(path != null, nameof(path));
            Covenant.Requires<ArgumentNullException>(installedSdks != null, nameof(installedSdks));

            this.Architecture  = architecture;
            this.PATH          = path;
            this.HasUnzip      = hasUnzip;
            this.HasDebugger   = hasDebugger;
            this.InstalledSdks = installedSdks.ToList();
        }

        /// <summary>
        /// <summary>
        /// Returns the chip architecture (like <b>armv71</b>).
        /// </summary>
        public string Architecture { get; private set; }

        /// <summary>
        /// Returns the current value of the <b>PATH</b> environment variable.
        /// </summary>
        public string PATH { get; private set; }

        /// <summary>
        /// Returns <c>true</c> if <b>unzip</b> is installed on the Raspberry Pi.
        /// This is required and will be installed automatically.
        /// </summary>
        public bool HasUnzip { get; private set; }

        /// <summary>
        /// Indicates whether the <b>vsdbg</b> debugger is installed.
        /// </summary>
        public bool HasDebugger { get; set; }

        /// <summary>
        /// Returns information about the .NET Core SDKs installed.
        /// </summary>
        public List<Sdk> InstalledSdks { get; private set; }
    }
}
