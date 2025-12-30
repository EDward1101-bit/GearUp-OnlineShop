using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using Microsoft.AspNetCore.Identity;
using OnlineShopProject_dNet.Services;

namespace OnlineShopProject_dNet.Controllers
{
    public class OrdersAjaxController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CartService _cartService;

        public OrdersAjaxController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, CartService cartService)
        {
            _db = db;
            _userManager = userManager;
            _cartService = cartService;
        }

        // Returns HTML partial for mini cart
        [HttpGet]
        public async Task<IActionResult> MiniCart()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Content("<div class=\"text-center py-3 text-muted\">Autentificare necesar?</div>", "text/html");

            var cart = await _db.Orders
                                .Include(o => o.OrderDetails)
                                .ThenInclude(od => od.Product)
                                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            if (cart == null)
            {
                return PartialView("~/Views/Orders/MiniCart.cshtml", null);
            }

            cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
            return PartialView("~/Views/Orders/MiniCart.cshtml", cart);
        }

        // Merge localStorage cart into user's server-side cart after login
        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests with [FromBody] don't need form tokens
        public async Task<IActionResult> MergeLocalCart([FromBody] List<LocalCartItem> items)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (items == null || items.Count == 0) return Json(new { success = true, merged = 0 });

            int merged = 0;
            foreach (var it in items)
            {
                if (it == null) continue;
                var added = await _cartService.AddItemToCart(userId, it.productId, it.quantity);
                if (added) merged += it.quantity;
            }

            return Json(new { success = true, merged });
        }

        public class LocalCartItem
        {
            public int productId { get; set; }
            public int quantity { get; set; }
        }

        // Returns JSON count for badge
        [HttpGet]
        public async Task<IActionResult> MiniCartCount()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { count = 0 });

            var cart = await _db.Orders
                                .Include(o => o.OrderDetails)
                                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            if (cart == null) return Json(new { count = 0 });

            var total = cart.OrderDetails.Sum(od => od.Quantity);
            return Json(new { count = total });
        }
    }
}
