//-----------------------------------------------------------------------------
// FILE:	    Sdk.cs
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
using System.Diagnostics.Contracts;
using RaspberryDebugger.Models.Sdk;

namespace RaspberryDebugger.Connection
{
    /// <summary>
    /// Holds information about a .NET Core SDK installed on a Raspberry Pi.
    /// </summary>
    internal class Sdk
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The SDK name.</param>
        /// <param name="version">The SDK version.</param>
        /// <param name="architecture">The SDK bitness architecture.</param>
        public Sdk(string name, string version, SdkArchitecture architecture = SdkArchitecture.ARM32)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));

            this.Name    = name;
            this.Version = version;
            this.Architecture = architecture;
        }

        /// <summary>
        /// Returns the name of the SDK (like <b>3.1.402</b>).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns thge version of the SDK (like <b>3.1.8</b>).
        /// </summary>
        public string Version { get; private set; }

        public SdkArchitecture Architecture { get; private set; }
    }
}
