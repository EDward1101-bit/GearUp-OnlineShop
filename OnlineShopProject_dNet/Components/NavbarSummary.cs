using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using Microsoft.AspNetCore.Identity;

namespace OnlineShopProject_dNet.Components
{
    // Clasa care moștenește ViewComponent
    public class NavbarSummary(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : ViewComponent
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new NavbarSummaryViewModel();

            // Verificăm dacă userul e logat
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(UserClaimsPrincipal);

                // 1. Numărăm produsele din Wishlist
                model.WishlistCount = await _context.Wishlists.CountAsync(w => w.UserId == userId);

                // 2. Numărăm produsele din Coș (suma cantităților)
                // Folosim logica identică cu OrdersController [cite: 3, 47]
                var cart = await _context.Orders
                                         .Include(o => o.OrderDetails)
                                         .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

                if (cart != null)
                {
                    model.CartCount = cart.OrderDetails.Sum(od => od.Quantity);
                }
            }

            // Trimitem datele către View-ul HTML
            return View(model);
        }
    }

    // Clasa mică pentru transportul datelor (ViewModel)
    public class NavbarSummaryViewModel
    {
        public int CartCount { get; set; } = 0;
        public int WishlistCount { get; set; } = 0;
    }
}