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
using Newtonsoft.Json;

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
        [JsonProperty(PropertyName = "Host", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue("")]
        public string Host { get; set; }

        /// <summary>
        /// The target SSH port;
        /// </summary>
        [JsonProperty(PropertyName = "Port", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(22)]
        public int Port { get; set; } = 22;

        /// <summary>
        /// The SSH user name.
        /// </summary>
        [JsonProperty(PropertyName = "Username", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue("")]
        public string Username { get; set; } = "pi";

        /// <summary>
        /// Specifies the authentication type.
        /// </summary>
        [JsonProperty(PropertyName = "AuthenticationType", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(PiAuthenticationType.Password)]
        public PiAuthenticationType AuthenticationType { get; set; } = PiAuthenticationType.Password;

        /// <summary>
        /// The SSH password or an empty string.
        /// </summary>
        [JsonProperty(PropertyName = "Password", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue("")]
        public string Password { get; set; } = "";

        /// <summary>
        /// The path to the SSH public key file or an empty string.
        /// </summary>
        [JsonProperty(PropertyName = "KeyPath", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue("")]
        public string KeyPath { get; set; } = "";
    }
}
