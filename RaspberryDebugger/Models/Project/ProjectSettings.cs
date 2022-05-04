//-----------------------------------------------------------------------------
// FILE:	    ProjectSettings.cs
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
using Newtonsoft.Json;

namespace RaspberryDebugger.Models.Project
{
    /// <summary>
    /// Holds the Raspberry related settings for a project.
    /// </summary>
    internal class ProjectSettings
    {
        /// <summary>
        /// Connection combo box item indicating that Raspberry debugging is disabled.
        /// </summary>
        public const string DisabledConnectionName = "[DISABLED]";

        /// <summary>
        /// Connection combo box item indicating that the default Raspberry connection should be used.
        /// </summary>
        public const string DefaultConnectionName  = "[DEFAULT]";

        /// <summary>
        /// Default constructor that returns settings with remote debugging disabled.
        /// </summary>
        public ProjectSettings()
        {
            EnableRemoteDebugging = false;
            RemoteDebugTarget     = null;
        }

        /// <summary>
        /// Indicates whether remote debugging is disabled.
        /// </summary>
        [JsonProperty(PropertyName = "EnableRemoteDebugging", Required = Required.Always)]
        public bool EnableRemoteDebugging { get; set; }

        /// <summary>
        /// Specifies the name of the specific remote Raspberry connection to use or
        /// <c>null</c> for the default connection.
        /// </summary>
        [JsonProperty(PropertyName = "RemoteDebugTarget", Required = Required.AllowNull)]
        public string RemoteDebugTarget { get; set; }

        /// <summary>
        /// Specifies the Linux group the program should execute within.  
        /// This defaults to <c>gpio</c>.
        /// </summary>
        [JsonProperty(PropertyName = "TargetGroup", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue("gpio")]
        public string TargetGroup { get; set; } = "gpio";
    }
}
