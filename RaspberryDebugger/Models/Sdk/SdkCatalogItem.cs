//-----------------------------------------------------------------------------
// FILE:	    SdkCatalogItem.cs
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
using System.ComponentModel;
using System.Runtime.InteropServices;

using Neon.Common;

using Newtonsoft.Json;

namespace RaspberryDebugger
{
    /// <summary>
    /// Describes an .NET Core SDK download.
    /// </summary>
    internal class SdkCatalogItem
    {
        private bool?   isStandAlone;

        /// <summary>
        /// The SDK name (like "3.1.402").
        /// </summary>
        [JsonProperty(PropertyName = "Name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// The SDK Version (like "3.1.8").
        /// </summary>
        [JsonProperty(PropertyName = "Version", Required = Required.Always)]
        public string Version { get; set; }

        /// <summary>
        /// Specifies the 32-bit or 64-bit version of the SDK.
        /// </summary>
        [JsonProperty(PropertyName = "Architecture", Required = Required.Always)]
        public SdkArchitecture Architecture { get; set; }

        /// <summary>
        /// The URL to the binary download.
        /// </summary>
        [JsonProperty(PropertyName = "Link", Required = Required.Always)]
        public string Link { get; set; }

        /// <summary>
        /// The SHA512 hash expected for the download.z
        /// </summary>
        [JsonProperty(PropertyName = "SHA512", Required = Required.Always)]
        public string SHA512 { get; set; }

        /// <summary>
        /// Indicates whether the SDK is actually usable or not.  This defaults
        /// to <c>false</c>.  This is set for some early .NET 5.0 releases
        /// that didn't work on ARM64.
        /// </summary>
        [JsonProperty(PropertyName = "Unusable", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool IsUnusable { get; set; }

        /// <summary>
        /// Indicates that the SDK is usable.
        /// </summary>
        [JsonIgnore]
        public bool IsUsable => !IsUnusable;

        /// <summary>
        /// Indicates that this is a standaloneg SDK vs. one integrated into Visual Studio;
        /// </summary>
        [JsonIgnore]
        public bool IsStandalone
        {
            get
            {
                if (isStandAlone.HasValue)
                {
                    return isStandAlone.Value;
                }

                // Standalone SDKs seem to have name patch versions < 200.

                isStandAlone = SemanticVersion.Parse(Name).Patch < 200;

                return isStandAlone.Value;
            }
        }
    }
}
