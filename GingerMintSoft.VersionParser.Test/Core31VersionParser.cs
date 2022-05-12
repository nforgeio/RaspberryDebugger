using System;
using GingerMintSoft.VersionParser.Architecture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class Core31VersionParser
    {
        [TestMethod]
        public void FindCore31Arm64TestMethod()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = page.ReadDownloadPages(Version.Core3, Sdk.Arm64);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public void FindCore31Arm32TestMethod()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = page.ReadDownloadPages(Version.Core3, Sdk.Arm32);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public void ReadActualCore3()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = page.ReadActualDownloadPage(Version.Core3, Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = page.ReadActualDownloadPage(Version.Core3, Sdk.Arm32);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }

        [TestMethod]
        public void ReadCore3Version()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = page.ReadDownloadPageForVersion(Version.Core3, "3.1.302", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = page.ReadDownloadPageForVersion(Version.Core3, "3.1.300", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }
    }
}
