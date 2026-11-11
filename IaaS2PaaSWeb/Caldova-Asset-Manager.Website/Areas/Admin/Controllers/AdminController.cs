using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace CaldovaAssetManager.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminConstants.Role)]
    public abstract class AdminController : Controller
    {
    }
}