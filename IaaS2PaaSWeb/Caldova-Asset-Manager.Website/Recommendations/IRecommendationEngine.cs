using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaldovaAssetManager.Recommendations
{
    public interface IRecommendationEngine
    {
        Task<IEnumerable<string>> GetRecommendationsAsync(string productId);
    }
}