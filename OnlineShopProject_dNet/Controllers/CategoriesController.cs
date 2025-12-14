using Microsoft.AspNetCore.Mvc;
using OnlineShopProject_dNet.Data;

namespace OnlineShopProject_dNet.Controllers
{
    public class CategoriesController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext db = context;
        public IActionResult Index()
        {
            var categories = from category in db.Categories
                             orderby category.Name
                             select category;

            ViewBag.Categories = categories;
            return View();
        }
    }
}
