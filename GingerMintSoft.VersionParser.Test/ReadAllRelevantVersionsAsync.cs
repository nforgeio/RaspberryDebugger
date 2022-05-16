using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GingerMintSoft.VersionParser.Architecture;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class AllRelevantVersionsAsync
    {
        [TestMethod]
        public async Task FindAllCoreVersionsAsyncTestMethod()
        {
            var downloadLinks = new List<string>();

            var page = new HtmlPage("https://dotnet.microsoft.com");
            Assert.IsNotNull(page);

            var downLoadLinks32 = await page.ReadDownloadPagesAsync(Version.Core3, Sdk.Arm32);
            Assert.IsNotNull(downLoadLinks32);

            var downLoadLinks64 = await page.ReadDownloadPagesAsync(Version.Core3, Sdk.Arm64);
            Assert.IsNotNull(downLoadLinks64);

            downloadLinks.AddRange(downLoadLinks32);
            downloadLinks.AddRange(downLoadLinks64);

            downLoadLinks32 = await page.ReadDownloadPagesAsync(Version.Core6, Sdk.Arm32);
            Assert.IsNotNull(downLoadLinks32);

            downLoadLinks64 = await page.ReadDownloadPagesAsync(Version.Core6, Sdk.Arm64);
            Assert.IsNotNull(downLoadLinks64);

            downloadLinks.AddRange(downLoadLinks32);
            downloadLinks.AddRange(downLoadLinks64);

            foreach (var downLoad in downloadLinks)
            {
                Console.WriteLine($"{downLoad} \r\n");
            }
        }
    }
}

