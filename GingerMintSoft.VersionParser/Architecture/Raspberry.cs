using System.Runtime.Serialization;

namespace GingerMintSoft.VersionParser.Architecture
{
    /// <summary>
    /// Enumerates the supported ARM architectures.
    /// </summary>
    public enum Sdk
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
}

