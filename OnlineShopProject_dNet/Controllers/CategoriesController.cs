using Microsoft.AspNetCore.Authorization; // NECESAR pentru securitate
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // NECESAR pentru Include
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using OnlineShopProject_dNet.Services;
using Microsoft.Extensions.Logging;

namespace OnlineShopProject_dNet.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;
        private readonly TextProcessingService _textProcessor;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, IWebHostEnvironment env, TextProcessingService textProcessor, ILogger<CategoriesController> logger)
        {
            db = context;
            _env = env;
            _textProcessor = textProcessor;
            _logger = logger;
        }

        // 1. GETALL - Returnează categorii ca JSON (pentru dropdown - public)
        [HttpGet]
        public IActionResult GetAll()
        {
            var categories = db.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    productCount = c.Products != null ? c.Products.Count(p => p.Status == "Approved") : 0
                })
                .ToList();

            return Json(categories);
        }

        // GETALLFORADMIN - Returnează categorii cu detalii pentru admin (pentru gestionare)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetAllForAdmin()
        {
            var categories = db.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    productCount = c.Products != null ? c.Products.Count : 0
                })
                .ToList();

            return Json(categories);
        }

        // INDEX - Eliminat (folosim modal acum)
        // [HttpGet]
        // public IActionResult Index() { ... }

        // SHOW - Eliminat (nu mai avem nevoie de view separat)
        // Produsele se filtrează direct din Products/Index

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
            // Sanitize category name
            cat.Name = _textProcessor.SanitizeText(cat.Name);

            if (!ModelState.IsValid)
            {
                foreach (var kv in ModelState)
                {
                    foreach (var err in kv.Value.Errors)
                    {
                        _logger.LogWarning("ModelState error for {Key}: {Error}", kv.Key, err.ErrorMessage);
                    }
                }
                return View(cat);
            }

            try
            {
                db.Categories.Add(cat);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost adăugată!";
                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving new category");
                ModelState.AddModelError(string.Empty, "A apărut o eroare la salvarea categoriei.");
                return View(cat);
            }
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

            // Sanitize category name
            requestCategory.Name = _textProcessor.SanitizeText(requestCategory.Name);

            if (!ModelState.IsValid)
            {
                foreach (var kv in ModelState)
                {
                    foreach (var err in kv.Value.Errors)
                    {
                        _logger.LogWarning("ModelState error for {Key}: {Error}", kv.Key, err.ErrorMessage);
                    }
                }
                return View(requestCategory);
            }

            try
            {
                category.Name = requestCategory.Name;
                db.SaveChanges();
                TempData["message"] = "Categoria a fost modificată!";
                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", id);
                ModelState.AddModelError(string.Empty, "A apărut o eroare la actualizarea categoriei.");
                return View(requestCategory);
            }
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

            try
            {
                db.Categories.Remove(category);
                db.SaveChanges();
                TempData["message"] = "Categoria și produsele aferente au fost șterse!";
                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                TempData["message"] = "A apărut o eroare la ștergerea categoriei.";
                return RedirectToAction("Index", "Products");
            }
        }
    }
}