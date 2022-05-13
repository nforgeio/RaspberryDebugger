using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class DownloadLinkAndChecksum
    {
        [TestMethod]
        public void ReadDownloadLink()
        {
            var page = new HtmlPage("https://dotnet.microsoft.com");
            Assert.IsNotNull(page);

            var sdkUri = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.202-linux-arm64-binaries";
            Console.WriteLine($"Download Uri: {sdkUri}\r\n");

            var (downLoadLink, checkSum) = page.ReadDownloadUriAndChecksum($"{sdkUri}");
            Assert.IsNotNull(downLoadLink);
            Assert.IsNotNull(checkSum);

            Console.WriteLine($"Download Link: {downLoadLink} \r\n" +
                              $"Checksum: {checkSum} \r\n");

            sdkUri = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-3.1.418-linux-arm32-binaries";
            Console.WriteLine($"Download Uri: {sdkUri}\r\n");

            (downLoadLink, checkSum) = page.ReadDownloadUriAndChecksum($"{sdkUri}");
            Assert.IsNotNull(downLoadLink);
            Assert.IsNotNull(checkSum);

            Console.WriteLine($"Download Link: {downLoadLink} \r\n" +
                              $"Checksum: {checkSum} \r\n");
        }
    }
}
