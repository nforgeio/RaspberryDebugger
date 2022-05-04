//-----------------------------------------------------------------------------
// FILE:	    Status.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2021 by neonFORGE, LLC.  All rights reserved.
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
using System.Diagnostics.Contracts;
using System.Linq;
using RaspberryDebugger.Models.Sdk;

namespace RaspberryDebugger.Connection
{
    /// <summary>
    /// Describes the current status of a remote Raspberry Pi.
    /// </summary>
    internal class Status
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="processor">The chip architecture.</param>
        /// <param name="hasUnzip">Indicates whether <b>unzip</b> is installed.</param>
        /// <param name="hasDebugger">Indicates whether the debugger is installed.</param>
        /// <param name="installedSdks">The installed .NET Core SDKs.</param>
        /// <param name="path">The current value of the PATH environment variable.</param>
        /// <param name="model">The Raspberry board model.</param>
        /// <param name="revision">The Raspberry board revision.</param>
        public Status(
            string              processor, 
            string              path, 
            bool                hasUnzip, 
            bool                hasDebugger, 
            IEnumerable<Sdk>    installedSdks,
            string              model,
            string              revision,
            SdkArchitecture     architecture)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(processor), nameof(processor));
            Covenant.Requires<ArgumentNullException>(path != null, nameof(path));
            Covenant.Requires<ArgumentNullException>(installedSdks != null, nameof(installedSdks));

            this.Processor         = processor;
            this.PATH              = path;
            this.HasUnzip          = hasUnzip;
            this.HasDebugger       = hasDebugger;
            this.InstalledSdks     = installedSdks.ToList();
            this.RaspberryModel    = model;
            this.RaspberryRevision = revision;
            this.Architecture      = architecture;
        }

        /// <summary>
        /// <summary>
        /// Returns the chip architecture (like <b>armv71</b>).
        /// </summary>
        public string Processor { get; private set; }

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

        /// <summary>
        /// Returns the Raspberry board model.
        /// </summary>
        public string RaspberryModel { get; private set; }

        /// <summary>
        /// Returns the Raspberry board revision.
        /// </summary>
        public string RaspberryRevision { get; private set; }

        /// <summary>
        /// Returns the Raspberry architecture.
        /// </summary>
        public SdkArchitecture Architecture { get; private set; }
    }
}
