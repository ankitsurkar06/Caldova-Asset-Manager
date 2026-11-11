using System.Net.Http.Headers;
using System.Text;

namespace CaldovaAssetManager.Utils;

public class HttpClientWrapper : IHttpClient
{
    private readonly HttpClient client;

    public HttpClientWrapper(HttpClient client, IConfiguration configuration)
    {
        this.client = client;
        if (!configuration.GetValue<bool>("ShowRecommendations")) return;
        var key = configuration["MachineLearning:AccountKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("MachineLearning:AccountKey must be supplied externally when recommendations are enabled.");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(key)));
    }

    public Task<string> GetStringAsync(string uri) => client.GetStringAsync(uri);
}
