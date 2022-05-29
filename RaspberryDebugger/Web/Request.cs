using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RaspberryDebugger.Web;

public class Request
{
    private string VersionsFeedUri { get; } = "https://dotnetversionfeed.azurewebsites.net/versions";

    public async Task<string> ReadVersionFeedServiceAsync(string uri = null)
    {
        string responseBody;

        if (string.IsNullOrEmpty(uri)) uri = VersionsFeedUri;

        try
        {
            var response = await new HttpClient().GetAsync(uri);
            response.EnsureSuccessStatusCode();

            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            responseBody = string.Empty;
        }

        return responseBody;
    }
}