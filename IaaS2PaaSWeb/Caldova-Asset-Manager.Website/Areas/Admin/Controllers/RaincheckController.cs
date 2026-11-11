using CaldovaAssetManager.Utils;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace CaldovaAssetManager.Areas.Admin.Controllers
{
    public class RaincheckController : AdminController
    {
        private readonly IRaincheckQuery _query;

        public RaincheckController(IRaincheckQuery query)
        {
            _query = query;
        }

        public async Task<ActionResult> Index()
        {
            var rainchecks = await _query.GetAllAsync();

            return View(rainchecks);
        }
    }
}
