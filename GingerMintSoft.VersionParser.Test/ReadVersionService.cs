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
    public class ReadVersionService
    {
        [TestMethod]
        public async Task ReadVersionServiceAsync()
        {
            const string uri = @"https://dotnetverionfeed.azurewebsites.net/version";
            var ret = await new Request().ReadVersionFeedService(uri);

            Assert.IsNotNull(ret);
            Debug.Print(ret);
        }
    }
}
