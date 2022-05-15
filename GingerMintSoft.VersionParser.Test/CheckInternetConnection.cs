using System.Threading.Tasks;
using GingerMintSoft.VersionParser.Connection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GingerMintSoft.VersionParser.Test
{
    [TestClass]
    public class CheckInternetConnection
    {
        [TestMethod]
        public async Task CheckTest()
        {
            var ok = await Internet.CheckAsync();
            Assert.IsTrue(ok);
        }
    }
}
