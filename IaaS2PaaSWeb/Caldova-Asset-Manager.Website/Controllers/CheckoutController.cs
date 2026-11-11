using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ICaldovaAssetManagerContext db;

        public CheckoutController(ICaldovaAssetManagerContext context)
        {
            db = context;
        }

        const string PromoCode = "FREE";

        //
        // GET: /Checkout/

        public async Task<ActionResult> AddressAndPayment()
        {
            var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await db.Users.FirstOrDefaultAsync(o => o.Id == id);

            var order = new Order
            {
                Name = user.Name,
                Email = user.Email,
                Username = user.UserName
            };

            return View(order);
        }

        //
        // POST: /Checkout/AddressAndPayment

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddressAndPayment(Order order)
        {
            order.Username = User.Identity.Name;
            order.OrderDate = DateTime.Now;
            ModelState.Remove(nameof(Order.Username));
            if (!ModelState.IsValid)
                return View(order);
            if (!string.Equals(Request.Form["PromoCode"].FirstOrDefault(), PromoCode,
                StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Invalid promotional code.");
                return View(order);
            }
            db.Orders.Add(order);
            var cart = ShoppingCart.GetCart(db, HttpContext);
            cart.CreateOrder(order);
            await db.SaveChangesAsync(CancellationToken.None);
            return RedirectToAction("Complete", new { id = order.OrderId });
        }

        //
        // GET: /Checkout/Complete

        public ActionResult Complete(int id)
        {
            // Validate customer owns this order
            var username = User.Identity.Name;

            Order order = db.Orders.First(
                o => o.OrderId == id &&
                o.Username == username);

            if (order != null)
            {
                return View(order);
            }
            else
            {
                return View("Error");
            }
        }
    }
}
