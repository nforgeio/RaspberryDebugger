using System;
using GingerMintSoft.VersionParser.Architecture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class Core6VersionParser
    {
        [TestMethod]
        public void FindCore6Arm64TestMethod()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = page.ReadDownloadPages(Version.Core6, Sdk.Arm64);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public void FindCore6Arm32TestMethod()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoads = page.ReadDownloadPages(Version.Core6, Sdk.Arm32);
            Assert.IsNotNull(downLoads);

            foreach (var downLoad in downLoads)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }

        [TestMethod]
        public void ReadActualCore6()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = page.ReadActualDownloadPage(Version.Core6, Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = page.ReadActualDownloadPage(Version.Core6, Sdk.Arm32);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }

        [TestMethod]
        public void ReadCore6Version()
        {
            var page = new HtmlPage();
            Assert.IsNotNull(page);

            var downLoad = page.ReadDownloadPageForVersion(Version.Core6, "6.0.103", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");

            downLoad = page.ReadDownloadPageForVersion(Version.Core6, "6.0.100", Sdk.Arm64);
            Assert.IsNotNull(downLoad);

            Console.WriteLine($"{downLoad} \r\n");
        }
    }
}