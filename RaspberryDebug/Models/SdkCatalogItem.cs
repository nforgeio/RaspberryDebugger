//-----------------------------------------------------------------------------
// FILE:	    SdkCatalogItem.cs
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

using Newtonsoft.Json;

namespace RaspberryDebug
{
    /// <summary>
    /// Describes an .NET Core SDK download.
    /// </summary>
    public class SdkCatalogItem
    {
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
    }
}
