using System.Runtime.Serialization;

namespace GingerMintSoft.VersionParser.Architecture
{
    public enum Version
    {
        /// <summary>
        /// 3.1 version
        /// </summary>
        [EnumMember(Value = "3.1")] 
        Core3,

        /// <summary>
        /// 5.0 version
        /// </summary>
        [EnumMember(Value = "5.0")] 
        Core5,

        /// <summary>
        /// 6.0 version
        /// </summary>
        [EnumMember(Value = "6.0")] 
        Core6,

        /// <summary>
        /// 7.0 version
        /// </summary>
        [EnumMember(Value = "7.0")] 
        Core7
    }
}