using Microsoft.AspNetCore.Mvc;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using Microsoft.AspNetCore.Hosting; 
using System.IO; 
using Microsoft.EntityFrameworkCore; 

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

        public IActionResult Index()
        {
            var categories = from category in db.Categories
                             orderby category.Name
                             select category;
            ViewBag.Categories = categories;
            return View();
        }

        public IActionResult Show(int id)
        {
            Category category = db.Categories.Find(id);
            return View(category);
        }

        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        public IActionResult New(Category cat)
        {
            if (ModelState.IsValid)
            {
                db.Categories.Add(cat);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(cat);
        }

        public IActionResult Edit(int id)
        {
            Category category = db.Categories.Find(id);
            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(int id, Category requestCategory)
        {
            Category category = db.Categories.Find(id);
            if (ModelState.IsValid)
            {
                category.Name = requestCategory.Name;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(requestCategory);
        }

       
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Category? category = db.Categories.Find(id);

            if (category == null)
            {
                return NotFound();
            }

            
            // Putem interoga direct tabelul Products
            var associatedProducts = db.Products.Where(p => p.CategoryId == id).ToList();

            // 2. Iteram prin produse si stergem imaginile fizice
            foreach (var product in associatedProducts)
            {
                if (!string.IsNullOrEmpty(product.Image))
                {
                    // Construim calea completa
                    var imagePath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));

                    // Stergem fisierul daca exista
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
            }

            db.Categories.Remove(category);
            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}