using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GingerMintSoft.VersionParser.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class ReadVersionFeedService
    {
        [TestMethod]
        public async Task ReadVersionFeedServiceAsync()
        {
            const string uri = @"https://dotnetverionfeed.azurewebsites.net/version";

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var ret = await new Request().ReadVersionFeedService(uri);

            stopwatch.Stop();
            Assert.IsNotNull(ret);
            Debug.Print($"Milliseconds: {stopwatch.ElapsedMilliseconds}");
            Debug.Print(ret);
        }

        [TestMethod]
        public async Task ReadVersionFeedServiceWithoutUriAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var ret = await new Request().ReadVersionFeedService("");

            stopwatch.Stop();
            Assert.IsNotNull(ret);
            Debug.Print($"Milliseconds: {stopwatch.ElapsedMilliseconds}");
            Debug.Print(ret);

            stopwatch.Start();

            ret = await new Request().ReadVersionFeedService();

            stopwatch.Stop();
            Assert.IsNotNull(ret);
            Debug.Print($"Milliseconds: {stopwatch.ElapsedMilliseconds}");
            Debug.Print(ret);
        }
    }
}
