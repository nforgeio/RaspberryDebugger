//-----------------------------------------------------------------------------
// FILE:	    PiConnection.cs
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
using System.Net;
using Neon.Common;
using Neon.SSH;

using Newtonsoft.Json;

namespace RaspberryDebug
{
    /// <summary>
    /// Implements a SSH connection to the remote Raspberry Pi.
    /// </summary>
    public class PiConnection : SshLinuxProxy
    {
        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="name">The server name.</param>
        /// <param name="address">The IP address.</param>
        /// <param name="credentials">The SSH credentials.</param>
        /// <param name="logWriter">Optional log writer.</param>
        private PiConnection(string name, IPAddress address, SshCredentials credentials, TextWriter logWriter = null) 
            : base(name, address, credentials, logWriter)
        {
        }
    }
}
