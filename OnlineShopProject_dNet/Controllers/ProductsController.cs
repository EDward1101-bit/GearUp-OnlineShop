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
            product.Status = product.Stock > 0;

            //  Daca utilizatorul a incarcat o imagine
            if (Image != null && Image.Length > 0)
            {
                // Verificari extensie si dimensiune
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Image", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif).");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }

                if (Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Image", "Imaginea nu poate fi mai mare de 5MB.");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }

                // Salvare fizica
                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                var databaseFileName = "/images/" + Image.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }

                // Setam calea imaginii incarcate
                product.Image = databaseFileName;
            }
            else
            {
                // Daca NU a incarcat imagine, folosim placeholder-ul
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
        public async Task<IActionResult> Edit(int id, Product requestProduct, IFormFile? Image)
        {
            // Gasim produsul existent in baza de date
            Product? product = await db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Actualizam datele text
            product.Title = requestProduct.Title;
            product.Description = requestProduct.Description;
            product.Price = requestProduct.Price;
            product.Stock = requestProduct.Stock;
            product.Status = product.Stock > 0;
            product.CategoryId = requestProduct.CategoryId;

            // Logica pentru imagine la editare
            if (Image != null && Image.Length > 0)
            {
                //  Validari (Tip si Dimensiune)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Image", "Fișierul trebuie să fie o imagine.");
                    ViewBag.Categories = db.Categories;
                    return View(requestProduct);
                }
                if (Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Image", "Imaginea nu poate fi mai mare de 5MB.");
                    ViewBag.Categories = db.Categories;
                    return View(requestProduct);
                }

                //  Stergerea imaginii vechi (Daca exista)
                if (!string.IsNullOrEmpty(product.Image))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                //  Salvarea imaginii noi
                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                var databaseFileName = "/images/" + Image.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }

                product.Image = databaseFileName;
            }

            // Eliminam validarea pentru Image deoarece:
            // a) Fie am pus una noua si e ok
            // b) Fie am pastrat-o pe cea veche (deci e deja in 'product', dar 'requestProduct.Image' e null)
            ModelState.Remove("Image");
            ModelState.Remove("requestProduct.Image"); // Pentru siguranta

            if (TryValidateModel(product))
            {
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = db.Categories;
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

            // Stergem fisierul fizic asociat
            if (!string.IsNullOrEmpty(product.Image))
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
