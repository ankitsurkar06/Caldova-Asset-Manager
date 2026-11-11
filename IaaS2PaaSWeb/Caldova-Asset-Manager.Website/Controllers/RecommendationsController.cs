using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using CaldovaAssetManager.Models;
using CaldovaAssetManager.Recommendations;
using CaldovaAssetManager.Utils;

namespace CaldovaAssetManager.Controllers
{
    public class RecommendationsController : Controller
    {
        private readonly ICaldovaAssetManagerContext db;
        private readonly IRecommendationEngine recommendation;

        public RecommendationsController(ICaldovaAssetManagerContext context, IRecommendationEngine recommendationEngine)
        {
            db = context;
            recommendation = recommendationEngine;
        }

        public async Task<ActionResult> GetRecommendations(string productId)
        {
            if (!ConfigurationHelpers.GetBool("ShowRecommendations"))
            {
                return new EmptyResult();
            }

            var recommendedProductIds = await recommendation.GetRecommendationsAsync(productId);

            var recommendedProducts = await db.Products.Where(x => recommendedProductIds.Contains(x.ProductId.ToString())).ToListAsync();

            return PartialView("_Recommendations", recommendedProducts);
        }
    }
}
