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
using Newtonsoft.Json;

namespace RaspberryDebugger.Models.Sdk
{
    /// <summary>
    /// Describes an .NET Core SDK download.
    /// </summary>
    internal class SdkCatalogItem
    {
        /// <summary>
        /// The SDK name (like "3.1.402").
        /// </summary>
        [JsonProperty(PropertyName = "Name", Required = Required.Always)]
        public string Name { get; set; }

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
        public string Sha512 { get; set; }

        /// <summary>
        /// SdkCatalog Item Constructor
        /// </summary>
        /// <param name="name">SDK Name </param>
        /// <param name="sdk">SDK type</param>
        /// <param name="link">Link for download</param>
        /// <param name="sha512">Checksum for download</param>
        public SdkCatalogItem(string name, SdkArchitecture sdk, string link, string sha512)
        {
            Name = name;
            Architecture = sdk;
            Link = link;
            Sha512 = sha512;
        }
    }
}
