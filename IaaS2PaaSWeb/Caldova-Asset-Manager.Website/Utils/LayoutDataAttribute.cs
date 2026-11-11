using System.Runtime.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.Utils;

public class LayoutDataAttribute(ICaldovaAssetManagerContext db) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ViewResult && context.Controller is Controller controller)
        {
            var cart = ShoppingCart.GetCart(db, context.HttpContext);
            controller.ViewBag.CartSummary = cart.GetCartItems().Select(a => a.Product.Title).OrderBy(a => a).ToList();
            controller.ViewBag.Categories = db.Categories.ToList();
            var latest = MemoryCache.Default["latestProduct"] as Product;
            if (latest == null)
            {
                latest = db.Products.OrderByDescending(a => a.Created).FirstOrDefault();
                if (latest != null) MemoryCache.Default.Add("latestProduct", latest, DateTimeOffset.Now.AddMinutes(10));
            }
            controller.ViewBag.Product = latest;
        }
        await next();
    }
}
