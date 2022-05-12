using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GingerMintSoft.VersionParser.Architecture;
using GingerMintSoft.VersionParser.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class SerializeDeserializeScrapingContent
    {
        [TestMethod]
        public void SerializeDeserializeScrapingContentTest()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var baseUri = page.BaseUri;
            Assert.IsNotNull(baseUri);
            Console.WriteLine($"Empty ctor base uri: {baseUri}");

            page.BaseUri = "https://dotnet.microsoft.com";
            page.CultureInfo = CultureInfo.CreateSpecificCulture("en-us");
            var uriDotNet = page.DotNetUri;
            Assert.IsNotNull(uriDotNet);

            baseUri = page.BaseUri;
            Assert.IsNotNull(baseUri);
            Console.WriteLine($"Base uri set via property: {baseUri}");

            var netUri = page.DotNetUri;
            Assert.IsNotNull(netUri);
            Console.WriteLine($".NET uri: {netUri}");

            var downloadUri = page.DownloadUri;
            Assert.AreEqual("download/dotnet", downloadUri);

            Console.WriteLine();
            Console.WriteLine("Sdk scraping part:");

            var catalog = new SdkScrapingCatalog
            {
                Culture = "en-Us",
                MicrosoftBaseUri = "https://dotnet.microsoft.com",
                Sdks = new List<SdkScraper>()
                {
                   new()
                   {
                       Family = Sdk.Arm32,
                       Version = Version.Core3
                   },
                   new()
                   {
                       Family = Sdk.Arm64,
                       Version = Version.Core3
                   },
                   new()
                   {
                       Family = Sdk.Arm32,
                       Version = Version.Core6
                   },
                   new()
                   {
                       Family = Sdk.Arm64,
                       Version = Version.Core6
                   }
                }
            };

            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            var jsonCatalog = JsonConvert.SerializeObject(catalog, Formatting.Indented, jsonSerializerSettings);
            Assert.IsNotNull(uriDotNet);

            var readCatalog = JsonConvert.DeserializeObject<SdkScrapingCatalog>(jsonCatalog, jsonSerializerSettings);
            Assert.IsNotNull(readCatalog);

            Console.WriteLine($"Culture: {readCatalog.Culture}");
            Console.WriteLine($"BaseUri: {readCatalog.MicrosoftBaseUri}\r\n");

            var sdks = readCatalog.Sdks ?? new List<SdkScraper>();

            foreach (var sdk in sdks)
            {
                Console.WriteLine($"Sdk: {sdk.Family}, Version: {sdk.Version}");
            }

            var path = AppDomain.CurrentDomain.BaseDirectory;
            var directory = Path.GetDirectoryName(path);
            Assert.IsNotNull(directory);

            Console.WriteLine();
            Console.WriteLine("Catalog:");
            Console.WriteLine($"{jsonCatalog}");
        }
    }
}
