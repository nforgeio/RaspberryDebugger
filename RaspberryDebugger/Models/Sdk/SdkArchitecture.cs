//-----------------------------------------------------------------------------
// FILE:	    SdkArchitecture.cs
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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RaspberryDebugger.Models.Sdk
{
    /// <summary>
    /// Enumerates the supported ARM architectures.
    /// </summary>
    public enum SdkArchitecture
    {
        /// <summary>
        /// 32-bit ARM
        /// </summary>
        [EnumMember(Value = "Arm32")]
        Arm32,

        /// <summary>
        /// 64-bit ARM
        /// </summary>
        [EnumMember(Value = "Arm64")]
        Arm64,

        /// <summary>
        /// unknown 
        /// </summary>
        [EnumMember(Value = "Unknown")]
        Unknown
    }

    /// <summary>
    /// Enumerates the supported architecture bitness.
    /// </summary>
    public enum Platform
    {
        /// <summary>
        /// 32-bit ARM
        /// </summary>
        [EnumMember(Value = "32")]
        Bitness32,

        /// <summary>
        /// 64-bit ARM
        /// </summary>
        [EnumMember(Value = "64")]
        Bitness64
    }

    /// <summary>
    /// OperatingSystem bitness
    /// </summary>
    public static class OperatingSystem
    {
        public static List<string> Bitness32 { get; private set; } = 
            new List<string>(new []{"armv3", "armv7"});

        public static List<string> Bitness64 { get; private set; } = 
            new List<string>(new []{"armv8", "aarch64"});
    }
}
