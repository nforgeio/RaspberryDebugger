using GingerMintSoft.VersionParser.Architecture;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser.Models
{
    public class SdkScraper
    {
        public Version Version { get; set; }

        public Sdk Family { get; set; }
    }
}