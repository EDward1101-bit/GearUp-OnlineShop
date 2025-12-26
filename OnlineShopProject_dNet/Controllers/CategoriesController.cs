using Microsoft.AspNetCore.Authorization; // NECESAR pentru securitate
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // NECESAR pentru Include
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;

        public CategoriesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            db = context;
            _env = env;
        }

        // 1. INDEX - Public: Oricine vede lista
        public IActionResult Index()
        {
            var categories = from category in db.Categories
                             orderby category.Name
                             select category;
            ViewBag.Categories = categories;
            return View();
        }

        // 2. SHOW - Public: Oricine vede produsele din categorie
        [HttpGet]
        public IActionResult Show(int id)
        {
            // MODIFICARE: Folosim Include pentru a avea acces si la Produse in View
            Category? category = db.Categories
                                   .Include(c => c.Products)
                                   .FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // 3. NEW - RESTRICTIONAT: Doar Admin
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult New()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult New(Category cat)
        {
            if (ModelState.IsValid)
            {
                db.Categories.Add(cat);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost adăugată!";
                return RedirectToAction("Index");
            }
            return View(cat);
        }

        // 4. EDIT - RESTRICTIONAT: Doar Admin
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            Category? category = db.Categories.Find(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Edit(int id, Category requestCategory)
        {
            Category? category = db.Categories.Find(id);
            if (category == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                category.Name = requestCategory.Name;
                db.SaveChanges();
                TempData["message"] = "Categoria a fost modificată!";
                return RedirectToAction("Index");
            }
            return View(requestCategory);
        }

        // 5. DELETE - RESTRICTIONAT: Doar Admin + Logica ta de stergere poze
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null) return NotFound();

            // --- LOGICA TA DE STERGERE IMAGINI (PASTRATA) ---
            var associatedProducts = db.Products.Where(p => p.CategoryId == id).ToList();

            foreach (var product in associatedProducts)
            {
                // Verificam sa nu fie null si sa NU fie imaginea default
                if (!string.IsNullOrEmpty(product.Image) && product.Image != "/images/default-product.jpeg")
                {
                    var imagePath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
            }
            // ------------------------------------------------

            db.Categories.Remove(category);
            db.SaveChanges();
            TempData["message"] = "Categoria și produsele aferente au fost șterse!";

            return RedirectToAction("Index");
        }
    }
}