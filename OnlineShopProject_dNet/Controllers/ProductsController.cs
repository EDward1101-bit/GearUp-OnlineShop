using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using OnlineShopProject_dNet.Services;
using Microsoft.Extensions.Logging;

namespace OnlineShopProject_dNet.Controllers
{
    public class ProductsController(
        ApplicationDbContext context,
        IWebHostEnvironment env,
        UserManager<ApplicationUser> userManager,
        TextProcessingService textProcessor,
        ILogger<ProductsController> logger) : Controller
    {
        private readonly ApplicationDbContext db = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly TextProcessingService _text_processor = textProcessor;
        private readonly ILogger<ProductsController> _logger = logger;

        // 1. INDEX - Vizitatorii vad doar produsele APROBATE cu căutare, filtrare și sortare
        [HttpGet]
        public IActionResult Index(int? category, string search, string sortBy = "name", string sortOrder = "asc")
        {
            var query = db.Products
                         .Include(p => p.Category)
                         .Include(p => p.Wishlists)
                         .Where(p => p.Status == "Approved"); // Filtrare esentiala

            // Filtrare după categorie dacă este specificată
            if (category.HasValue)
            {
                query = query.Where(p => p.CategoryId == category.Value);
            }

            // Căutare după nume (parțial matching)
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower().Trim();
                query = query.Where(p => p.Title.ToLower().Contains(search));
            }

            // Sortare
            switch (sortBy.ToLower())
            {
                case "price":
                    query = sortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Price)
                        : query.OrderBy(p => p.Price);
                    break;
                case "rating":
                    query = sortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Rating ?? 0)
                        : query.OrderBy(p => p.Rating ?? 0);
                    break;
                case "name":
                default:
                    query = sortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Title)
                        : query.OrderBy(p => p.Title);
                    break;
            }

            var products = query.ToList();

            ViewBag.Products = products;
            ViewBag.SelectedCategory = category;
            ViewBag.Categories = db.Categories.OrderBy(c => c.Name).ToList();
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            // Pentru Admin: adăugăm produsele Pending într-o zonă separată
            if (User.IsInRole("Admin"))
            {
                var pendingProducts = db.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.User)
                                        .Where(p => p.Status == "Pending")
                                        .OrderByDescending(p => p.Id)
                                        .ToList();
                ViewBag.PendingProducts = pendingProducts;
            }

            // Pentru Proposer: adăugăm produsele proprii Pending
            if (User.IsInRole("Proposer"))
            {
                var currentUserId = _userManager.GetUserId(User);
                var myPendingProducts = db.Products
                    .Include(p => p.Category)
                    .Where(p => p.Status == "Pending" && p.UserId == currentUserId)
                    .OrderByDescending(p => p.Id)
                    .ToList();
                ViewBag.MyPendingProducts = myPendingProducts;
            }

            if (products.Count == 0)
            {
                TempData["message"] = "Nu există produse aprobate momentan.";
            }

            return View();
        }

        // 2. SHOW - Detalii produs
        [HttpGet]
        public IActionResult Show(int id)
        {
            var product = db.Products
                            .Include(p => p.Category)
                            .Include(p => p.Reviews)
                            .ThenInclude(r => r.User)
                            .Include(p => p.User)
                            .Include(p => p.Wishlists)
                            .FirstOrDefault(p => p.Id == id);

            if (product == null) return NotFound();

            // Securitate: Vezi produsul doar daca e Aprobat SAU e al tau SAU esti Admin
            bool isOwner = _userManager.GetUserId(User) == product.UserId;
            bool isAdmin = User.IsInRole("Admin");

            if (product.Status != "Approved" && !isOwner && !isAdmin)
            {
                return Forbid();
            }

            return View(product);
        }

        // 3. NEW - Adaugare (Doar Admin si Proposer)
        [Authorize(Roles = "Admin,Proposer")]
        [HttpGet]
        public IActionResult New()
        {
            ViewBag.Categories = db.Categories;
            return View();
        }

        [Authorize(Roles = "Admin,Proposer")]
        [HttpPost]
        public async Task<IActionResult> New(Product product, IFormFile? Image)
        {
            var userId = _userManager.GetUserId(User);
            
            // Handle null product or empty fields before validation
            if (product == null)
            {
                ModelState.AddModelError(string.Empty, "Datele produsului sunt invalide.");
                ViewBag.Categories = db.Categories;
                return View(new Product());
            }

            product.UserId = userId;

            _logger.LogInformation("User {UserId} is creating a new product: {ProductTitle}", userId, product.Title);

            // --- Logica Imagine ---
            if (Image != null && Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension) || Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Image", "Fișier invalid. Doar JPG, PNG, GIF, max 5MB.");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }

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
                product.Image = "/images/default-product.jpeg";
            }

            // Remove Image from validation since it's optional
            ModelState.Remove(nameof(product.Image));
            ModelState.Remove(nameof(product.UserId));
            ModelState.Remove(nameof(product.Status));
            ModelState.Remove(nameof(product.Rating));

            // Sanitize inputs for security AFTER validation check
            if (!string.IsNullOrWhiteSpace(product.Title))
            {
                product.Title = _text_processor.SanitizeText(product.Title);
            }
            
            if (!string.IsNullOrWhiteSpace(product.Description))
            {
                // Preserve formatting - sanitize HTML but keep structure
                product.Description = _text_processor.ProcessForStorage(_text_processor.SanitizeHtml(product.Description));
            }

            // LOGICA STATUS: Admin -> Approved direct / Colaborator -> Pending
            if (User.IsInRole("Admin"))
            {
                product.Status = "Approved";
            }
            else
            {
                product.Status = "Pending";
            }

            // Validate model
            if (ModelState.IsValid && TryValidateModel(product))
            {
                try
                {
                    db.Products.Add(product);
                    await db.SaveChangesAsync();

                    if (product.Status == "Pending")
                        TempData["message"] = "Produsul a fost trimis spre aprobare!";
                    else
                        TempData["message"] = "Produsul a fost adăugat!";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving product");
                    ModelState.AddModelError(string.Empty, "A apărut o eroare la salvarea produsului. Verifică datele introduse.");
                }
            }

            ViewBag.Categories = db.Categories;
            return View(product);
        }

        // 4. EDIT
        [Authorize(Roles = "Admin,Proposer")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return NotFound();

            if (product.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu ai dreptul să editezi acest produs!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = db.Categories;
            return View(product);
        }

        [Authorize(Roles = "Admin,Proposer")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product requestProduct, IFormFile? Image)
        {
            var product = await db.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (product.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Remove fields from validation that we handle manually
            ModelState.Remove(nameof(product.Image));
            ModelState.Remove(nameof(product.UserId));
            ModelState.Remove(nameof(product.Status));
            ModelState.Remove(nameof(product.Rating));

            // Sanitize inputs for security
            if (!string.IsNullOrWhiteSpace(requestProduct.Title))
            {
                product.Title = _text_processor.SanitizeText(requestProduct.Title);
            }
            else
            {
                product.Title = product.Title; // Keep existing if empty
            }
            
            if (!string.IsNullOrWhiteSpace(requestProduct.Description))
            {
                // Preserve formatting - sanitize HTML but keep structure
                product.Description = _text_processor.ProcessForStorage(_text_processor.SanitizeHtml(requestProduct.Description));
            }
            else
            {
                product.Description = product.Description; // Keep existing if empty
            }
            
            product.Price = requestProduct.Price;
            product.Stock = requestProduct.Stock;
            product.CategoryId = requestProduct.CategoryId;

            // RESETARE STATUS LA EDITARE
            // Daca esti Colaborator si modifici ceva, produsul reintra in verificare
            if (User.IsInRole("Proposer"))
            {
                product.Status = "Pending";
            }
            else if (User.IsInRole("Admin"))
            {
                product.Status = "Approved";
            }

            // --- Logica Imagine ---
            if (Image != null && Image.Length > 0)
            {
                if (!string.IsNullOrEmpty(product.Image) && product.Image != "/images/default-product.jpeg")
                {
                    var oldPath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }
                product.Image = "/images/" + Image.FileName;
            }

            if (TryValidateModel(product))
            {
                await db.SaveChangesAsync();

                if (product.Status == "Pending")
                    TempData["message"] = "Produsul modificat necesită o nouă aprobare!";
                else
                    TempData["message"] = "Produsul a fost actualizat!";

                return RedirectToAction("Index");
            }

            ViewBag.Categories = db.Categories;
            return View(requestProduct);
        }

        // 5. DELETE
        [HttpPost]
        [Authorize(Roles = "Admin,Proposer")]
        public ActionResult Delete(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return NotFound();

            if (product.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu ai dreptul să ștergi acest produs!";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(product.Image) && product.Image != "/images/default-product.jpeg")
            {
                var imagePath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));
                if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
            }

            db.Products.Remove(product);
            db.SaveChanges();
            TempData["message"] = "Produsul a fost șters.";
            return RedirectToAction("Index");
        }

        // 6. APPROVE - Doar Admin poate aproba produse
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Approve(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return NotFound();

            product.Status = "Approved";
            db.SaveChanges();

            TempData["message"] = "Produsul a fost aprobat cu succes!";
            return RedirectToAction("Index");
        }

        // 7. REJECT - Doar Admin poate respinge produse
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Reject(int id)
        {
            var product = db.Products.Find(id);
            if (product == null) return NotFound();

            product.Status = "Rejected";
            db.SaveChanges();

            TempData["message"] = "Produsul a fost respins.";
            return RedirectToAction("Index");
        }

        // 8. GETPENDINGCOUNT - Returnează numărul de produse Pending (pentru badge în navbar)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetPendingCount()
        {
            var count = db.Products.Count(p => p.Status == "Pending");
            return Json(new { count });
        }
    }
}