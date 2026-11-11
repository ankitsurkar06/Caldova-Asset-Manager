using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.Api;

[ApiController, Route("api/products")]
public class ProductsController(ICaldovaAssetManagerContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IList<Product>> Get(bool sale = false) =>
        await (sale ? db.Products.Where(p => p.Price != p.SalePrice) : db.Products).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        return product == null ? NotFound() : Ok(product);
    }
}
