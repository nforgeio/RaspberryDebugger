using System;
using GingerMintSoft.VersionParser.Architecture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class Core5VersionParser
    {
        [TestMethod]
        public void FindCore5Arm64TestMethod()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = page.ReadDownloadPages(Version.Core5, Sdk.Arm64);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public void FindCore5Arm32TestMethod()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = page.ReadDownloadPages(Version.Core5, Sdk.Arm32);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public void ReadActualCore5()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = page.ReadActualDownloadPage(Version.Core5, Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = page.ReadActualDownloadPage(Version.Core5, Sdk.Arm32);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }

        [TestMethod]
        public void ReadCore5Version()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = page.ReadDownloadPageForVersion(Version.Core5, "5.0.203", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = page.ReadDownloadPageForVersion(Version.Core5, "5.0.200", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }
    }
}
