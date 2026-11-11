using System.Threading.Tasks;

namespace CaldovaAssetManager.Utils
{
    public interface IHttpClient
    {
        Task<string> GetStringAsync(string uri);
    }
}