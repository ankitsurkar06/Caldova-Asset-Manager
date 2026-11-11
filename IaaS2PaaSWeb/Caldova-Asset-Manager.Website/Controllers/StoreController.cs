using CaldovaAssetManager.Models;
using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Runtime.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using CaldovaAssetManager.Utils;
using CaldovaAssetManager.ViewModels;
using System.Threading;

namespace CaldovaAssetManager.Controllers
{
    public class StoreController : Controller
    {
        private readonly ICaldovaAssetManagerContext db;

        public StoreController(ICaldovaAssetManagerContext context)
        {
            db = context;
            int delay = ConfigurationHelpers.GetInt32("SiteDelayMilliseconds");
            if (delay < 0)
                throw new InvalidOperationException("SiteDelayMilliseconds must not be negative.");

            Thread.Sleep(delay);
        }

        //
        // GET: /Store/
        public ActionResult Index()
        {
            var genres = db.Categories.ToList();

            return View(genres);
        }

        //
        // GET: /Store/Browse?genre=Disco
        public ActionResult Browse(int categoryId)
        {
            // Retrieve Category genre and its Associated associated Products products from database
            var genreModel = db.Categories.Include("Products").Single(g => g.CategoryId == categoryId);

            return View(genreModel);
        }

        public ActionResult Details(int id)
        {

            var productCacheKey = string.Format("product_{0}", id);
            var product = MemoryCache.Default[productCacheKey] as Product;
            if (product == null)
            {
                product = db.Products.Single(a => a.ProductId == id);
                //Remove it from cache if not retrieved in last 10 minutes
                MemoryCache.Default.Add(productCacheKey, product, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(10) });
            }
            var viewModel = new ProductViewModel
            {
                Product = product,
                ShowRecommendations = ConfigurationHelpers.GetBool("ShowRecommendations")
            };

            return View(viewModel);
        }

    }
}
