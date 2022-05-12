using System.Collections.Generic;

namespace GingerMintSoft.VersionParser.Models
{
    public class SdkScrapingCatalog
    {
        public string? MicrosoftBaseUri { get; set; }

        public string? Culture { get; set; }

        public List<SdkScraper>? Sdks { get; set; } = new List<SdkScraper>();
    }
}
