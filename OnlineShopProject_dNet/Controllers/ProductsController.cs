using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using Microsoft.AspNetCore.Hosting; // Necesar pentru IWebHostEnvironment
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
        // Se afiseaza lista tuturor produselor impreuna cu categoria din care fac parte
        // HttpGet implicit
        public IActionResult Index()
        {
            var products = db.Products
                             .Include(a => a.Category);
            // .OrderByDescending(a => a.Date); => trebuie alt order


            ViewBag.Products = products;
            return View();
        }

        // Se afiseaza un singur articol in functie de id-ul sau impreuna cu categoria din care face parte
        // In plus sunt preluate si toate review urile asociate unui produs
        // HttpGet implicit
        public IActionResult Show(int id)
        {
            Product product = db.Products
                            .Include(p => p.Category)
                            .Include(p => p.Reviews)
                            .Where(p => p.Id == id)
                            .First();

            ViewBag.Product = product;
            ViewBag.Category = product.Category;

            return View();
        }


        // Se afiseaza formularul in care se vor completa datele unui produs impreuna cu selectarea categoriei din care face parte
        // HttpGet implicit

        public IActionResult New()
        {
            var categories = from categ in db.Categories
                             select categ;

            ViewBag.Categories = categories;

            return View();
        }

        
        // Se adauga produsul in baza de date
        [HttpPost]
        public async Task<IActionResult> New(Product product, IFormFile? Image)
        {
            // Calculam statusul in functie de stoc
            product.Status = product.Stock > 0;

            // Logica de incarcare imagine 
            if (Image != null && Image.Length > 0)
            {
                // 1. Verificam extensia (Tipul fisierului)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Image", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif).");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }

                //  Verificam dimensiunea (Maxim 5MB - Cerinta proiect) 
                if (Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Image", "Imaginea nu poate fi mai mare de 5MB.");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }

                //  Construim calea de stocare
                // Se creeaza un folder in wwwroot, numit images 
                
                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                var databaseFileName = "/images/" + Image.FileName;

                //  Salvarea fizica a fisierului 
                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }

                //  Setam calea in model
                product.Image = databaseFileName;

                // Eliminam eroarea de validare pentru Image din ModelState
                // Deoarece campul a fost null la binding, dar acum are valoare
                ModelState.Remove(nameof(product.Image));
            }

            // Verificam validitatea modelului (TryValidateModel revalideaza cu noua valoare Image) 
            if (TryValidateModel(product))
            {
                db.Products.Add(product);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // Daca validarea esueaza, reincarcam categoriile
            var categories = from categ in db.Categories
                             select categ;
            ViewBag.Categories = categories;

            return View(product);
        }


        // Se editeaza un produs existent in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // HttpGet implicit
        // Se afiseaza formularul impreuna cu datele aferente produsului din baza de date
        public IActionResult Edit(int id)
        {
            Product product = db.Products
                                .Include(p => p.Category)
                                .First(prod => prod.Id == id);

            ViewBag.Product = product;
            ViewBag.Category = product.Category;

            var categories = from categ in db.Categories
                             select categ;

            ViewBag.Categories = categories;

            return View();
        }

        // Se adauga produsul modificat in baza de date
        [HttpPost]
        public IActionResult Edit(int id, Product requestProduct)
        {
            Product? product = db.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    product.Title = requestProduct.Title;
                    product.Description = requestProduct.Description;

                    product.Price = requestProduct.Price;
                    product.Stock = requestProduct.Stock;
                    product.Status = product.Stock > 0;

                    product.CategoryId = requestProduct.CategoryId;

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    var categories = from categ in db.Categories
                                     select categ;
                    ViewBag.Categories = categories;

                    // In caz de eroare, reincarcam View-ul cu datele trimise (requestProduct)
                    // pentru a afisa erorile in formular.
                    return View(requestProduct);
                }
            }

            // Daca validarea esueaza, reincarcam View-ul cu datele trimise (requestProduct)
            // pentru a afisa erorile.
            var categoriesList = from categ in db.Categories
                                 select categ;
            ViewBag.Categories = categoriesList;

            return View(requestProduct);
        }

        // Se sterge un produs din baza de date 
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Product? product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
