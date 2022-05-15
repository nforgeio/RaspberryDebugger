using System.Net.Http;
using System.Threading.Tasks;

namespace GingerMintSoft.VersionParser.Connection
{
    public static class Internet
    {
        
        /// <summary>
        /// Simple internet check
        /// </summary>
        /// <returns><c>true</c> on success.</returns>
        // ReSharper disable once UnusedMember.Global
        public static async Task<bool> CheckAsync()
        {
            const string google = "http://google.com/generate_204";

            try
            {
                using var client = new HttpClient();
                using (await client.GetAsync(google))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
