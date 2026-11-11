using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using CaldovaAssetManager.ProductSearch;

namespace CaldovaAssetManager.Controllers
{
    public class SearchController : Controller
    {
        private readonly IProductSearch search;

        public SearchController(IProductSearch search)
        {
            this.search = search;
        }

        [HttpGet]
        public async Task<ActionResult> Index(string q)
        {
            var result = await search.Search(q);

            return View(result);
        }
    }
}
