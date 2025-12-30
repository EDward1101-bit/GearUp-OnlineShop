using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Components
{
    [ViewComponent(Name = "UserSidebar")]
    public class UserSidebarViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserSidebarViewComponent(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.User == null)
            {
                return View("LoggedOut"); // Render unauthenticated sidebar
            }

            var user = await _userManager.GetUserAsync(httpContext.User);

            if (user == null)
            {
                return View("LoggedOut"); // Render unauthenticated sidebar
            }

            // Get user data
            var userData = new UserSidebarViewModel
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
                IsProposer = await _userManager.IsInRoleAsync(user, "Proposer")
            };

            // Get admin stats if admin
            if (userData.IsAdmin)
            {
                userData.PendingProductsCount = await _db.Products
                    .CountAsync(p => p.Status == "Pending");

                userData.TotalUsersCount = await _userManager.Users.CountAsync();

                userData.TotalProductsCount = await _db.Products
                    .Where(p => p.Status == "Approved")
                    .CountAsync();

                userData.TotalOrdersCount = await _db.Orders.CountAsync();

                userData.TotalRevenueSum = await _db.Orders
                    .Where(o => o.Status != "Cancelled")
                    .SelectMany(o => o.OrderDetails)
                    .SumAsync(od => od.Quantity * od.UnitPrice);
            }

            return View("Default", userData);
        }
    }

    public class UserSidebarViewModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsProposer { get; set; }

        // Admin Stats
        public int PendingProductsCount { get; set; }
        public int TotalUsersCount { get; set; }
        public int TotalProductsCount { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal TotalRevenueSum { get; set; }
    }
}
