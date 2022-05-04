//-----------------------------------------------------------------------------
// FILE:	    ConnectionInfo.cs
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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Neon.Net;
using Newtonsoft.Json;
using RaspberryDebugger.OptionsPages;

namespace RaspberryDebugger.Models.Connection
{
    /// <summary>
    /// Describes a Raspberry Pi connection's network details and credentials.
    /// </summary>
    internal class ConnectionInfo
    {
        private bool        isDefault;
        private string      host;
        private string      user = "pi";
        private string      cachedName;

        /// <summary>
        /// Returns the value to be used for sorting the connection.
        /// </summary>
        [JsonIgnore]
        public string SortKey => Name.ToLowerInvariant();

        /// <summary>
        /// Returns the connection name like: user@host
        /// </summary>
        [JsonIgnore]
        public string Name
        {
            get
            {
                if (cachedName != null)
                {
                    return cachedName;
                }

                return cachedName = $"{User}@{Host}";
            }
        }

        /// <summary>
        /// Indicates that this is the default connection.
        /// </summary>
        [JsonProperty(PropertyName = "IsDefault", Required = Required.Always)]
        public bool IsDefault
        {
            get => isDefault;

            set
            {
                if (isDefault == value) return;

                isDefault = value;
                ConnectionsPanel?.ConnectionIsDefaultChanged(this);
            }
        }

        /// <summary>
        /// The Raspberry Pi host name or IP address.
        /// </summary>
        [JsonProperty(PropertyName = "Host", Required = Required.Always)]
        public string Host
        {
            get => host;
            
            set
            {
                host      = value;
                cachedName = null;
            }
        }

        /// <summary>
        /// The SSH port.
        /// </summary>
        [JsonProperty(PropertyName = "Port", Required = Required.Always)]
        public int Port { get; set; } = NetworkPorts.SSH;

        /// <summary>
        /// The user name.
        /// </summary>
        [JsonProperty(PropertyName = "User", Required = Required.Always)]
        public string User
        {
            get => user;

            set
            {
                user       = value;
                cachedName = null;
            }
        }

        /// <summary>
        /// The password.
        /// </summary>
        [JsonProperty(PropertyName = "Password", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Password { get; set; } = "raspberry";

        /// <summary>
        /// Path to the private key for this connection or <c>null</c> when one
        /// hasn't been initialized yet.
        /// </summary>
        [JsonProperty(PropertyName = "PrivateKeyPath", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string PrivateKeyPath { get; set; }

        /// <summary>
        /// Path to the public key for this connection or <c>null</c> when one
        /// hasn't been initialized yet.
        /// </summary>
        [JsonProperty(PropertyName = "PublicKeyPath", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string PublicKeyPath { get; set; }

        /// <summary>
        /// Describes the authentication method.
        /// </summary>
        [JsonIgnore]
        public string Authentication => string.IsNullOrEmpty(PrivateKeyPath) ? "PASSWORD" : "SSH KEY";

        /// <summary>
        /// <para>
        /// This is a bit of a hack to call the <see cref="ConnectionsPanel.ConnectionIsDefaultChanged"/>
        /// when the user changes the state of the <see cref="IsDefault"/>  property.  The sender will be
        /// the changed <see cref="ConnectionInfo"/> and the arguments will be empty.
        /// </para>
        /// <para>
        /// This is a bit of hack because the <see cref="ObjectListView"/> control doesn't
        /// appear to have a check box changed event.
        /// </para>
        /// </summary>
        [SuppressMessage("ReSharper", "InvalidXmlDocComment")]
        internal ConnectionsPanel ConnectionsPanel { get; set; }
    }
}
