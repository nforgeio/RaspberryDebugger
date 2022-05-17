using System;
using System.Threading.Tasks;
using GingerMintSoft.VersionParser.Architecture;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class DownloadLinkAndChecksumAsync
    {
        [TestMethod]
        public async Task ReadDownloadLinkAsync()
        {
            var page = new HtmlPage("https://dotnet.microsoft.com");
            Assert.IsNotNull(page);

            var sdkUri = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.202-linux-arm64-binaries";
            Console.WriteLine($"Download Uri: {sdkUri}\r\n");

            var (downLoadLink, checkSum) = await page.ReadDownloadUriAndChecksumAsync($"{sdkUri}");
            Assert.IsNotNull(downLoadLink);
            Assert.IsNotNull(checkSum);

            Console.WriteLine($"Download Link: {downLoadLink} \r\n" +
                              $"Checksum: {checkSum} \r\n");

            sdkUri = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-3.1.418-linux-arm32-binaries";

            (downLoadLink, checkSum) = await page.ReadDownloadUriAndChecksumAsync($"{sdkUri}");
            Assert.IsNotNull(downLoadLink);
            Assert.IsNotNull(checkSum);

            Console.WriteLine($"Download Link: {downLoadLink} \r\n" +
                              $"Checksum: {checkSum} \r\n");
        }

        [TestMethod]
        public async Task ReadDownloadLinkExtendedAsync()
        {
            var page = new HtmlPage("https://dotnet.microsoft.com");
            Assert.IsNotNull(page);

            const string sdkUri = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.202-linux-arm64-binaries";
            Console.WriteLine($"Download Uri: {sdkUri}\r\n");

            ShowDownloadItems(await page.ReadDownloadUriAndChecksumExtendedAsync<SdkCatalogItem>($"{sdkUri}"));
        }

        private void ShowDownloadItems(SdkCatalogItem sdkInfo)
        {
            Assert.IsNotNull(sdkInfo.Name);
            Assert.IsNotNull(sdkInfo.Architecture);
            Assert.IsNotNull(sdkInfo.Link);
            Assert.IsNotNull(sdkInfo.Sha512);

            Console.WriteLine($"Download Name: {sdkInfo.Name} \r\n" +
                              $"Download SDK: {sdkInfo.Architecture} \r\n" +
                              $"Download Link: {sdkInfo.Link} \r\n" +
                              $"Checksum: {sdkInfo.Sha512} \r\n");
        }
    }

    public class SdkCatalogItem
    {
        /// <summary>
        /// The SDK name (like "3.1.402").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Specifies the 32-bit or 64-bit version of the SDK.
        /// </summary>
        public Sdk Architecture { get; set; }

        /// <summary>
        /// The URL to the binary download.
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The SHA512 hash expected for the download.z
        /// </summary>
        public string Sha512 { get; set; }

        public SdkCatalogItem(string sdkName, Sdk architecture, string sdkDownloadLink, string sha512)
        {
            Name = sdkName;
            Architecture = architecture;
            Link = sdkDownloadLink;
            Sha512 = sha512;
        }
    }
}