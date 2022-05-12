using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HtmlAgilityPack;
using System.Runtime.Serialization;
using GingerMintSoft.VersionParser.Architecture;
using GingerMintSoft.VersionParser.Extensions;
using Version = GingerMintSoft.VersionParser.Architecture.Version;

namespace GingerMintSoft.VersionParser
{
    public class HtmlPage
    {
        private HtmlWeb Web { get; }

        private HtmlDocument? Document { get; set; }

        public string BaseUri { get; set; } = "https://dotnet.microsoft.com";

        public string DownloadUri { get; set; } = "download/dotnet";

        public string DotNetUri { get; }

        public CultureInfo CultureInfo { get; set; } = CultureInfo.CreateSpecificCulture("en-us");

        public HtmlPage()
        {
            Web = new HtmlWeb();

            DotNetUri= $"{BaseUri}/{CultureInfo.Name}/{DownloadUri}";
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        public HtmlPage(string baseUri)
        {
            Web = new HtmlWeb();

            BaseUri = baseUri;
            DotNetUri= $"{BaseUri}/{CultureInfo.Name}/download/dotnet";
        }

        /// <summary>
        /// Load Html page as content
        /// </summary>
        /// <param name="htmlPage">Load this page</param>
        /// <returns>Html document</returns>
        private HtmlDocument? Load(string htmlPage)
        {
            Document = Web.Load(htmlPage);

            return Document;
        }

        /// <summary>
        /// Read download .NET versions at given page
        /// </summary>
        /// <param name="version">Search for this .NET version</param>
        /// <param name="architecture">Search for this architecture/bitness</param>
        /// <returns>List of partially version download uris</returns>
        public List<string> ReadDownloadPages(Version version, Sdk architecture)
        {
            // Get ARM bitness architecture for SDK
            var sdk = architecture == Sdk.Arm32 
                ? Sdk.Arm32.GetAttributeOfType<EnumMemberAttribute>()?.Value 
                : Sdk.Arm64.GetAttributeOfType<EnumMemberAttribute>()?.Value;

            // Get .NET main version: 3.1/5.0/6.0/etc.
            var actual = version.GetAttributeOfType<EnumMemberAttribute>()?.Value;
            var htmlPage = new HtmlPage(BaseUri).Load($"{DotNetUri}/{actual}");

            // Filter for Linux .NET SDK
            var downLoads = htmlPage?.DocumentNode
                .SelectNodes($"//a[contains(text(), '{sdk}')]")
                .Select(x => x.GetAttributeValue("href", string.Empty))
                .ToList();

            // filter unwanted: only ARM32 and ARM64 Bit is welcome for Raspberry Pi
            for (var i = 0; i < downLoads?.Count; i++)
            {
                if (downLoads != null && 
                    !downLoads[i].Contains("alpine") && 
                    !downLoads[i].Contains("x32") && 
                    !downLoads[i].Contains("x64") && 
                    !downLoads[i].Contains("macos") && 
                    !downLoads[i].Contains("windows") && 
                    !downLoads[i].Contains("runtime") && 
                    !downLoads[i].Contains("rc") && 
                    !downLoads[i].Contains("preview")) 
                    continue;

                downLoads?.RemoveAt(i--);
            }

            // reverse version number ordering -> the actual is on top
            downLoads?.Sort();
            downLoads?.Reverse();

            return downLoads ?? new List<string>();
        }

        /// <summary>
        /// Read actual download partial uri for .NET version and bitness
        /// </summary>
        /// <param name="version">Search for this .NET version</param>
        /// <param name="architecture">Search for this architecture/bitness</param>
        /// <returns>Partial version download uri</returns>
        public string ReadActualDownloadPage(Version version, Sdk architecture)
        {
            return ReadDownloadPages(version, architecture).First();
        }

        /// <summary>
        /// Read download partial uri for .NET version, bitness and a specific SDK version
        /// </summary>
        /// <param name="version">Search for this .NET version</param>
        /// <param name="specificVersion">Search for this .NET SDK version</param>
        /// <param name="architecture">Search for this architecture/bitness</param>
        /// <returns>Partial version download uri</returns>
        public string ReadDownloadPageForVersion(Version version, string specificVersion, Sdk architecture)
        {
            return ReadDownloadPages(version, architecture).First(x => x.Contains(specificVersion));
        }

        /// <summary>
        /// Read .NET download uri with related checksum
        /// </summary>
        /// <param name="uri">Uri to download SDK page</param>
        /// <returns>Download SDK uri and checksum</returns>
        public (string? downLoadLink, string? checkSum) ReadDownloadUriAndChecksum(string uri)
        {
            // load page content from uri
            var htmlPage = new HtmlPage(BaseUri).Load($"{uri}");

            // .NET SDK download link and checksum
            return 
                (htmlPage?.DocumentNode
                .SelectNodes("//a[@id='directLink']")
                .Select(x => x.GetAttributeValue("href", string.Empty))
                .First(), 
                htmlPage?.DocumentNode
                .SelectNodes("//input[@id='checksum']")
                .Select(x => x.GetAttributeValue("value", string.Empty))
                .First());
        }
    }
}