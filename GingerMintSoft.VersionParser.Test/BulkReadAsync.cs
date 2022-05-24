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
        public void BulkReadTest()
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
                    new StreamReader(catalogStream).ReadToEnd(),
                    jsonSerializerSettings);

            if (_cachedSdkScrapingCatalog?.Sdks == null) return;

            var downloadPageLinks = Task.WhenAll(_cachedSdkScrapingCatalog.Sdks.Select(sdk => Task.Run(() =>
                scrapeHtml.ReadDownloadPagesAsync(sdk.Version, sdk.Family)))).Result;

            //var downloadPageLinks = ThreadHelper.JoinableTaskFactory.Run(delegate
            //{
            //    return Task.WhenAll(_cachedSdkScrapingCatalog.Sdks.Select(sdk => Task.Run(() =>
            //        scrapeHtml.ReadDownloadPagesAsync(sdk.Version, sdk.Family))));
            //});

            var rawLinkCatalog = scrapeHtml.ReadDownloadUriAndChecksumBulkAsync(downloadPageLinks).Result;

            //var rawLinkCatalog = ThreadHelper.JoinableTaskFactory
            //    .Run<IEnumerable<(string, string)>>(async () =>
            //        await scrapeHtml.ReadDownloadUriAndChecksumBulkAsync(downloadPageLinks));

            foreach (var catalogItem in rawLinkCatalog)
            {
                Debug.Print($"{catalogItem.Item1}, {catalogItem.Item2}");
            }
        }
    }
}
