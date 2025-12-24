using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace OnlineShopProject_dNet.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            db = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Show(int id)
        {
            // INCLUDE: Category, Reviews, si User-ul care a scris Review-ul
            var product = db.Products
                            .Include(p => p.Category)
                            .Include(p => p.Reviews)
                            .ThenInclude(r => r.User) // Aducem si datele userului care a scris (Nume, etc.)
                            .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var products = db.Products.Include(p => p.Category).ToList();
            ViewBag.Products = products;
            return View();
        }

        [HttpGet]
        public IActionResult New()
        {
            ViewBag.Categories = db.Categories;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> New(Product product, IFormFile? Image)
        {
            product.Status = product.Stock > 0;

            // LOGICA IMAGINE
            if (Image != null && Image.Length > 0)
            {
                // Verificari 
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Image", "Extensie nepermisă.");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }
                if (Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Image", "Fișierul este prea mare (Max 5MB).");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }

                // Salvare fizica imagine noua
                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                var databaseFileName = "/images/" + Image.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }
                product.Image = databaseFileName;
            }
            else
            {
                // Daca nu se incarca nimic, setam PLACEHOLDER-ul
                product.Image = "/images/default-product.jpeg";
            }

            
            ModelState.Remove(nameof(product.Image));

            if (TryValidateModel(product))
            {
                db.Products.Add(product);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = db.Categories;
            return View(product);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return NotFound();

            ViewBag.Categories = db.Categories;
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product requestProduct, IFormFile? Image)
        {
            var product = await db.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Actualizam datele
            product.Title = requestProduct.Title;
            product.Description = requestProduct.Description;
            product.Price = requestProduct.Price;
            product.Stock = requestProduct.Stock;
            product.Status = product.Stock > 0;
            product.CategoryId = requestProduct.CategoryId;

            // Logica Imagine la Editare
            if (Image != null && Image.Length > 0)
            {
                
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension) || Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Image", "Eroare la fișier (extensie sau mărime).");
                    ViewBag.Categories = db.Categories;
                    return View(requestProduct);
                }

                // PROTECTIE PLACEHOLDER: Stergem imaginea veche DOAR daca NU este placeholder-ul
                if (!string.IsNullOrEmpty(product.Image) && product.Image != "/images/default-product.jpeg")
                {
                    var oldPath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                // Salvam noua imagine
                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                var databaseFileName = "/images/" + Image.FileName;
                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }
                product.Image = databaseFileName;
            }
            // Nota: Daca Image e null, product.Image ramane neschimbat 

            ModelState.Remove("Image");
            ModelState.Remove("requestProduct.Image");

            if (TryValidateModel(product))
            {
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = db.Categories;
            return View(requestProduct);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return NotFound();

            //  Nu stergem fisierul daca este cel default
            if (!string.IsNullOrEmpty(product.Image) && product.Image != "/images/default-product.jpeg")
            {
                var imagePath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}