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
        public IActionResult New(Product product)
        {
            if (ModelState.IsValid)
            {
                product.Status = product.Stock > 0;

                try
                {
                    db.Products.Add(product);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

                catch (Exception)
                {
                    // In caz de eroare la salvare (e.g. eroare DB), se reincarca categoriile.
                    var categories = from categ in db.Categories
                                     select categ;
                    ViewBag.Categories = categories;

                    // return View(product) este esential pentru a afisa datele introduse de utilizator
                    // si pentru a reține CategoryId in formular dupa eroare.
                    return View(product);
                }
            }
            // Daca modelul NU este valid (ModelState.IsValid == false), se reincarca categoriile
            // si se returneaza View(product) pentru a afisa erorile de validare (Title required, etc.).
            var categoriesList = from categ in db.Categories
                                 select categ;
            ViewBag.Categories = categoriesList;

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
