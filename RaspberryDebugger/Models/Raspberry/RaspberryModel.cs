//-----------------------------------------------------------------------------
// FILE:	    RaspberryModel.cs
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

namespace RaspberryDebugger.Models.Raspberry
{
    /// <summary>
    /// Describes a specific Raspberry board as described <a href="https://www.raspberrypi.org/documentation/hardware/raspberrypi/revision-codes/README.md">here</a>.
    /// </summary>
    internal class RaspberryModel
    {
        /// <summary>
        /// The board revision code.
        /// </summary>
        [JsonProperty(PropertyName = "Code", Required = Required.Always)]
        public string Code { get; set; }

        /// <summary>
        /// The board model.
        /// </summary>
        [JsonProperty(PropertyName = "Model", Required = Required.Always)]
        public string Model { get; set; }

        /// <summary>
        /// The board revision.
        /// </summary>
        [JsonProperty(PropertyName = "Revision", Required = Required.Always)]
        public string Revision { get; set; }

        /// <summary>
        /// The board RAM.
        /// </summary>
        [JsonProperty(PropertyName = "Ram", Required = Required.Always)]
        public string Ram { get; set; }

        /// <summary>
        /// The board manufacturer.
        /// </summary>
        [JsonProperty(PropertyName = "Manufacturer", Required = Required.Always)]
        public string Manufacturer { get; set; }
    }
}
