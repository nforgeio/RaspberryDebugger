using System;
using System.Threading.Tasks;
using GingerMintSoft.VersionParser.Architecture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class Core6VersionParserAsync
    {
        [TestMethod]
        public async Task FindCore6Arm64TestMethodAsync()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = await page.ReadDownloadPagesAsync(Version.Core6, Sdk.Arm64);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public async Task FindCore6Arm32TestMethodAsync()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = await page.ReadDownloadPagesAsync(Version.Core6, Sdk.Arm32);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public async Task ReadActualCore6Async()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = await page.ReadActualDownloadPageAsync(Version.Core6, Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = await page.ReadActualDownloadPageAsync(Version.Core6, Sdk.Arm32);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }

        [TestMethod]
        public async Task ReadCore6VersionAsync()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = await page.ReadDownloadPageForVersionAsync(Version.Core6, "6.0.103", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = await page.ReadDownloadPageForVersionAsync(Version.Core6, "6.0.100", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }
    }
}
