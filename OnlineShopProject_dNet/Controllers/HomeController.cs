using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly OnlineShopProject_dNet.Data.ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, OnlineShopProject_dNet.Data.ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }
        public IActionResult Index()
        {
            var products = _db.Products
                              .Where(p => p.Status == "Approved")
                              .Include(p => p.Wishlists)
                              .Include(p => p.Category)
                              .OrderByDescending(p => p.Id)
                              .Take(12)
                              .ToList();
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
