using System.Web.Mvc;
using CaldovaAssetManager.Utils;

namespace CaldovaAssetManager
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new LayoutDataAttribute());
        }
    }
}