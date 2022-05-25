using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GingerMintSoft.VersionParser.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class BulkReadAsync
    {
        private static SdkScrapingCatalog _cachedSdkScrapingCatalog;

        [TestMethod]
        public async Task BulkReadTestAsync()
        {
            var scrapeHtml = new HtmlPage();

            using var catalogStream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("GingerMintSoft.VersionParser.Test.sdk-parser-catalog.json");

            if (catalogStream == null) return;

            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

            _cachedSdkScrapingCatalog =
                JsonConvert.DeserializeObject<SdkScrapingCatalog>(
                    await new StreamReader(catalogStream).ReadToEndAsync(),
                    jsonSerializerSettings);

            if (_cachedSdkScrapingCatalog?.Sdks == null) return;

            var downloadPageLinks = await Task.WhenAll(_cachedSdkScrapingCatalog.Sdks.Select(sdk => Task.Run(() => 
                scrapeHtml.ReadDownloadPagesAsync(sdk.Version, sdk.Family))));

            var rawLinkCatalog = await scrapeHtml.ReadDownloadUriAndChecksumBulkAsync(downloadPageLinks);

            foreach (var catalogItem in rawLinkCatalog)
            {
                Debug.Print($"{catalogItem.Item1}, {catalogItem.Item2}");
            }
        }
    }
}
