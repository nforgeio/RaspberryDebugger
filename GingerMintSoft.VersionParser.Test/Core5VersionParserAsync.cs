using System;
using System.Threading.Tasks;
using GingerMintSoft.VersionParser.Architecture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class Core5VersionParserAsync
    {
        [TestMethod]
        public async Task FindCore5Arm64TestMethodAsync()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = await page.ReadDownloadPagesAsync(Version.Core5, Sdk.Arm64);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public async Task FindCore5Arm32TestMethodAsync()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = await page.ReadDownloadPagesAsync(Version.Core5, Sdk.Arm32);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public async Task ReadActualCore5Async()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = await page.ReadActualDownloadPageAsync(Version.Core5, Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = await page.ReadActualDownloadPageAsync(Version.Core5, Sdk.Arm32);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }

        [TestMethod]
        public async Task ReadCore5VersionAsync()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = await page.ReadDownloadPageForVersionAsync(Version.Core5, "5.0.203", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = await page.ReadDownloadPageForVersionAsync(Version.Core5, "5.0.200", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }
    }
}

