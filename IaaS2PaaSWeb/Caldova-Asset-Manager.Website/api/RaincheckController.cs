using Microsoft.AspNetCore.Mvc;
using CaldovaAssetManager.Models;
using CaldovaAssetManager.Utils;

namespace CaldovaAssetManager.Api;

[ApiController, Route("api/raincheck")]
public class RaincheckController(IRaincheckQuery query) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<Raincheck>> Get() => query.GetAllAsync();

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            return Ok(await query.FindAsync(id));
        }
        catch (ArgumentOutOfRangeException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public Task<int> Post([FromBody] Raincheck raincheck) => query.AddAsync(raincheck);
}
