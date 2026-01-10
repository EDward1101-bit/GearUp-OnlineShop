using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // 1. GETALL - Returneaza categorii ca JSON (pentru dropdown - public)
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

        // GETALLFORADMIN - Returneaza categorii cu detalii pentru admin (pentru gestionare)
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
            // Handle AJAX requests from modal
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                if (cat == null || string.IsNullOrWhiteSpace(cat.Name))
                {
                    return Json(new { success = false, message = "Numele categoriei este obligatoriu." });
                }

                // Sanitize category name
                cat.Name = _textProcessor.SanitizeText(cat.Name);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    
                    return Json(new { success = false, message = string.Join(" ", errors) });
                }

                try
                {
                    db.Categories.Add(cat);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Categoria a fost adaugata!" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving new category");
                    return Json(new { success = false, message = "A aparut o eroare la salvarea categoriei." });
                }
            }

            // Regular form submission
            if (cat == null || string.IsNullOrWhiteSpace(cat.Name))
            {
                ModelState.AddModelError("Name", "Numele categoriei este obligatoriu.");
                return View(cat);
            }

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
                TempData["message"] = "Categoria a fost adaugata!";
                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving new category");
                ModelState.AddModelError(string.Empty, "A aparut o eroare la salvarea categoriei.");
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
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Categoria nu a fost gasita." });
                return NotFound();
            }

            // Handle AJAX requests from modal
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                if (requestCategory == null || string.IsNullOrWhiteSpace(requestCategory.Name))
                {
                    return Json(new { success = false, message = "Numele categoriei este obligatoriu." });
                }

                // Sanitize category name
                requestCategory.Name = _textProcessor.SanitizeText(requestCategory.Name);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    
                    return Json(new { success = false, message = string.Join(" ", errors) });
                }

                try
                {
                    category.Name = requestCategory.Name;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Categoria a fost modificata!" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating category {CategoryId}", id);
                    return Json(new { success = false, message = "A aparut o eroare la actualizarea categoriei." });
                }
            }

            // Regular form submission
            if (requestCategory == null || string.IsNullOrWhiteSpace(requestCategory.Name))
            {
                ModelState.AddModelError("Name", "Numele categoriei este obligatoriu.");
                return View(requestCategory);
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
                TempData["message"] = "Categoria a fost modificata!";
                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", id);
                ModelState.AddModelError(string.Empty, "A aparut o eroare la actualizarea categoriei.");
                return View(requestCategory);
            }
        }

        // 5. DELETE - RESTRICTIONAT: Doar Admin + Logica de stergere poze
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null) 
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Categoria nu a fost gasita." });
                return NotFound();
            }

            // Logica de stergere imagini
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

            try
            {
                db.Categories.Remove(category);
                db.SaveChanges();
                
                // Return JSON for AJAX requests (modal stays open)
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Categoria si produsele aferente au fost sterse!" });
                }
                
                TempData["message"] = "Categoria si produsele aferente au fost sterse!";
                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "A aparut o eroare la stergerea categoriei." });
                }
                
                TempData["message"] = "A aparut o eroare la stergerea categoriei.";
                return RedirectToAction("Index", "Products");
            }
        }
    }
}