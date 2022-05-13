using System.Collections.Generic;

namespace GingerMintSoft.VersionParser.Models
{
    public class SdkScrapingCatalog
    {
        public string MicrosoftBaseUri { get; set; } = string.Empty;

        public string Culture { get; set; } = "en-US";

        public List<SdkScraper> Sdks { get; set; } = new List<SdkScraper>();
    }
}
