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
        ILogger<ProductsController> logger,
        IProductAiService productAiService,
        NotificationService notificationService,
        IImageValidationService imageValidationService) : Controller
    {
        private const string AiFallbackAnswer = "Momentan nu avem detalii despre acest aspect.";
        private readonly ApplicationDbContext db = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly TextProcessingService _text_processor = textProcessor;
        private readonly ILogger<ProductsController> _logger = logger;
        private readonly IProductAiService _productAiService = productAiService;
        private readonly NotificationService _notificationService = notificationService;
        private readonly IImageValidationService _imageValidationService = imageValidationService;

        // 1. INDEX - Vizitatorii vad doar produsele APROBATE cu cautare, filtrare si sortare
        [HttpGet]
        public IActionResult Index(int? category, string search, string sortBy = "name", string sortOrder = "asc", int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 12;

            var query = db.Products
                         .Include(p => p.Category)
                         .Include(p => p.Wishlists)
                         .Where(p => p.Status == "Approved"); // Filtrare esentiala

            // Filtrare dupa categorie daca este specificata
            if (category.HasValue)
            {
                query = query.Where(p => p.CategoryId == category.Value);
            }

            // Cautare dupa nume (partial matching)
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

            var totalCount = query.Count();
            var products = query
                // .Skip((page - 1) * pageSize)
                // .Take(pageSize)
                .ToList();

            ViewBag.Products = products;
            ViewBag.SelectedCategory = category;
            ViewBag.Categories = db.Categories.OrderBy(c => c.Name).ToList();
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Pentru Admin: adaugam produsele Pending intr-o zona separata
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

            // Pentru Proposer: adaugam produsele proprii Pending
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
                TempData["message"] = "Nu exista produse aprobate momentan.";
            }

            return View();
        }

        // 2. SHOW - Detalii produs
        [HttpGet]
        public IActionResult Show(int id, int reviewPage = 1, int reviewPageSize = 5)
        {
            if (reviewPage < 1) reviewPage = 1;
            if (reviewPageSize < 1 || reviewPageSize > 20) reviewPageSize = 5;

            var product = db.Products
                            .Include(p => p.Category)
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

            var currentUserId = _userManager.GetUserId(User);

            // Paginare review-uri
            var reviewsQuery = db.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.Date);

            var reviewsTotal = reviewsQuery.Count();
            var pagedReviews = reviewsQuery
                .Skip((reviewPage - 1) * reviewPageSize)
                .Take(reviewPageSize)
                .ToList();

            ViewBag.ReviewsPaged = pagedReviews;
            ViewBag.ReviewsPage = reviewPage;
            ViewBag.ReviewsTotalPages = (int)Math.Ceiling(reviewsTotal / (double)reviewPageSize);
            ViewBag.ReviewsTotal = reviewsTotal;
            ViewBag.UserHasReview = currentUserId != null && db.Reviews.Any(r => r.ProductId == id && r.UserId == currentUserId);

            // Pentru compatibilitate cu partialul
            product.Reviews = pagedReviews;

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> AskProductAssistant(int productId, string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Json(new { answer = AiFallbackAnswer });
            }

            var product = await db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return Json(new { answer = AiFallbackAnswer });
            }

            var currentUserId = _userManager.GetUserId(User);
            bool isOwner = currentUserId == product.UserId;
            bool isAdmin = User.IsInRole("Admin");

            if (product.Status != "Approved" && !isOwner && !isAdmin)
            {
                return Json(new { answer = AiFallbackAnswer });
            }

            var faqs = await db.FAQs
                .Where(f => f.ProductId == productId || f.ProductId == null)
                .ToListAsync();

            try
            {
                var answer = await _productAiService.AskProductAssistantAsync(product, faqs, question);
                
                // Salvare FAQ daca intrebarea este noua, are sens si raspunsul nu e fallback
                if (!IsFallbackAnswer(answer))
                {
                    await SaveQuestionToFaqIfNewAsync(productId, question, answer, faqs);
                }
                
                return Json(new { answer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI assistant failed for product {ProductId}", productId);
                return Json(new { answer = AiFallbackAnswer });
            }
        }

        /// <summary>
        /// Verifica daca raspunsul este un fallback (nu contine informatii utile)
        /// </summary>
        private bool IsFallbackAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return true;

            var lowerAnswer = answer.ToLowerInvariant();

            // Verificam daca raspunsul contine fraze de fallback
            var fallbackPhrases = new[]
            {
                "nu avem detalii",
                "nu am informatii",
                "nu pot raspunde",
                "contacteaza",
                AiFallbackAnswer.ToLowerInvariant()
            };

            return fallbackPhrases.Any(phrase => lowerAnswer.Contains(phrase));
        }

        /// <summary>
        /// Salveaza intrebarea in FAQ daca nu exista deja una similara semantic
        /// </summary>
        private async Task SaveQuestionToFaqIfNewAsync(int productId, string question, string answer, List<FAQ> existingFaqs)
        {
            try
            {
                // Validare: intrebarea trebuie sa aiba minim 10 caractere
                if (string.IsNullOrWhiteSpace(question) || question.Trim().Length < 10)
                {
                    _logger.LogDebug("Question rejected - too short (min 10 chars): {Question}", question);
                    return;
                }

                // Validare: raspunsul trebuie sa aiba minim 20 caractere
                if (string.IsNullOrWhiteSpace(answer) || answer.Trim().Length < 20)
                {
                    _logger.LogDebug("Answer rejected - too short (min 20 chars): {Answer}", answer);
                    return;
                }

                // Normalizam intrebarea pentru comparatie
                var normalizedQuestion = NormalizeQuestion(question);
                
                // Verificam daca intrebarea are sens (minim 1 cuvant, nu e spam)
                if (!IsValidQuestion(normalizedQuestion))
                {
                    _logger.LogDebug("Question rejected as invalid: {Question}", question);
                    return;
                }

                // Verificam daca exista deja o intrebare similara semantic pentru acelasi produs
                var productFaqs = existingFaqs.Where(f => f.ProductId == productId).ToList();
                foreach (var faq in productFaqs)
                {
                    if (AreQuestionsSemanticallySimlar(normalizedQuestion, NormalizeQuestion(faq.Question)))
                    {
                        _logger.LogDebug("Similar FAQ already exists for question: {Question}", question);
                        return; // Nu salvam duplicat
                    }
                }

                // Salvam noua intrebare
                var newFaq = new FAQ
                {
                    ProductId = productId,
                    Question = question.Trim(),
                    Answer = answer,
                    HelpfulCount = 0
                };

                db.FAQs.Add(newFaq);
                await db.SaveChangesAsync();
                _logger.LogInformation("New FAQ saved for product {ProductId}: {Question}", productId, question);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save FAQ for product {ProductId}", productId);
                // Nu aruncam exceptia - salvarea FAQ nu e critica
            }
        }

        /// <summary>
        /// Normalizeaza intrebarea pentru comparatie (lowercase, fara punctuatie, cuvinte sortate)
        /// </summary>
        private static string NormalizeQuestion(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return string.Empty;

            // Lowercase si eliminare punctuatie
            var normalized = new string(question.ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

            // Eliminare cuvinte comune (stop words) romanesti si englezesti
            var stopWords = new HashSet<string> 
            { 
                "este", "sunt", "care", "pentru", "acest", "aceasta", "cum", "ce", "de", "la", "in", "pe", "cu", "si", "sau", "nu", "da",
                "is", "are", "the", "a", "an", "this", "that", "how", "what", "for", "to", "in", "on", "with", "and", "or", "not", "yes",
                "poate", "pot", "ai", "am", "as", "ati", "au", "avea", "avem", "aveti"
            };

            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .OrderBy(w => w)
                .ToList();

            return string.Join(" ", words);
        }

        /// <summary>
        /// Verifica daca intrebarea este valida (nu e spam, are continut)
        /// </summary>
        private static bool IsValidQuestion(string normalizedQuestion)
        {
            if (string.IsNullOrWhiteSpace(normalizedQuestion))
                return false;

            var words = normalizedQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Minim 1 cuvant semnificativ
            if (words.Length < 1)
                return false;

            // Maxim 50 cuvinte (evitam spam)
            if (words.Length > 50)
                return false;

            return true;
        }

        /// <summary>
        /// Verifica daca doua intrebari sunt similare semantic (overlap de cuvinte cheie > 60%)
        /// </summary>
        private static bool AreQuestionsSemanticallySimlar(string q1, string q2)
        {
            if (string.IsNullOrWhiteSpace(q1) || string.IsNullOrWhiteSpace(q2))
                return false;

            var words1 = q1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var words2 = q2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            if (words1.Count == 0 || words2.Count == 0)
                return false;

            // Calculam overlap (Jaccard similarity)
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            var similarity = (double)intersection / union;

            // Similaritate > 60% = consideram duplicat
            return similarity > 0.6;
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

            // --- Logica Imagine cu validare magic bytes si redimensionare ---
            if (Image != null && Image.Length > 0)
            {
                // Verificare dimensiune (max 5MB)
                if (!_imageValidationService.IsValidImageSize(Image, 5 * 1024 * 1024))
                {
                    ModelState.AddModelError("Image", "Fisier prea mare sau extensie invalida. Doar JPG, PNG, GIF, max 5MB.");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }

                // Verificare magic bytes (continut real al fisierului)
                if (!_imageValidationService.IsValidImage(Image))
                {
                    ModelState.AddModelError("Image", "Fisierul nu este o imagine valida. Continutul nu corespunde extensiei.");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }

                try
                {
                    // Resize and save the image to standard 800x800
                    product.Image = await _imageValidationService.ResizeAndSaveImageAsync(Image, _env.WebRootPath, 800, 800);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resizing image for product");
                    ModelState.AddModelError("Image", "Eroare la procesarea imaginii. Incercati cu o alta imagine.");
                    ViewBag.Categories = db.Categories;
                    return View(product);
                }
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
                        TempData["message"] = "Produsul a fost adaugat!";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving product");
                    ModelState.AddModelError(string.Empty, "A aparut o eroare la salvarea produsului. Verifica datele introduse.");
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
                TempData["message"] = "Nu ai dreptul sa editezi acest produs!";
                return RedirectToAction("Index");
            }

            if (User.IsInRole("Proposer") && !string.Equals(product.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                TempData["message"] = "Poti edita produsul doar dupa ce a fost respins de admin (cu feedback).";
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

            if (User.IsInRole("Proposer") && !string.Equals(product.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                TempData["message"] = "Poti edita produsul doar dupa ce a fost respins de admin (cu feedback).";
                return RedirectToAction("Index");
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

            // --- Logica Imagine cu validare magic bytes si redimensionare ---
            if (Image != null && Image.Length > 0)
            {
                // Verificare dimensiune (max 5MB)
                if (!_imageValidationService.IsValidImageSize(Image, 5 * 1024 * 1024))
                {
                    ModelState.AddModelError("Image", "Fisier prea mare sau extensie invalida. Doar JPG, PNG, GIF, max 5MB.");
                    ViewBag.Categories = db.Categories;
                    return View(requestProduct);
                }

                // Verificare magic bytes (continut real al fisierului)
                if (!_imageValidationService.IsValidImage(Image))
                {
                    ModelState.AddModelError("Image", "Fisierul nu este o imagine valida. Continutul nu corespunde extensiei.");
                    ViewBag.Categories = db.Categories;
                    return View(requestProduct);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(product.Image) && product.Image != "/images/default-product.jpeg")
                {
                    var oldPath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                try
                {
                    // Resize and save the image to standard 800x800
                    product.Image = await _imageValidationService.ResizeAndSaveImageAsync(Image, _env.WebRootPath, 800, 800);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resizing image for product edit");
                    ModelState.AddModelError("Image", "Eroare la procesarea imaginii. Incercati cu o alta imagine.");
                    ViewBag.Categories = db.Categories;
                    return View(requestProduct);
                }
            }

            if (TryValidateModel(product))
            {
                await db.SaveChangesAsync();

                if (product.Status == "Pending")
                    TempData["message"] = "Produsul modificat necesita o noua aprobare!";
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
                TempData["message"] = "Nu ai dreptul sa stergi acest produs!";
                return RedirectToAction("Index");
            }

            if (User.IsInRole("Proposer") && !string.Equals(product.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                TempData["message"] = "Poti sterge produsul doar daca a fost respins de admin.";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(product.Image) && product.Image != "/images/default-product.jpeg")
            {
                var imagePath = Path.Combine(_env.WebRootPath, product.Image.TrimStart('/'));
                if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
            }

            // Curatam cosurile in lucru (InCart) care contin produsul
            var cartsWithProduct = db.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.ProductId == id && od.Order != null && od.Order.Status == "InCart")
                .ToList();

            if (cartsWithProduct.Any())
            {
                db.OrderDetails.RemoveRange(cartsWithProduct);
            }

            db.Products.Remove(product);
            db.SaveChanges();
            TempData["message"] = "Produsul a fost sters.";
            return RedirectToAction("Index");
        }

        // 6. APPROVE - Doar Admin poate aproba produse
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string? feedbackMessage)
        {
            var product = db.Products.Include(p => p.User).FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            product.Status = "Approved";
            db.SaveChanges();

            // Trimite notificare catre proposer
            if (!string.IsNullOrEmpty(product.UserId))
            {
                var message = $"Produsul tau '{product.Title}' a fost aprobat!";
                await _notificationService.AddNotificationAsync(
                    product.UserId, 
                    message, 
                    "product_approved",
                    product.Id,
                    feedbackMessage);
            }

            TempData["message"] = "Produsul a fost aprobat cu succes!";
            return RedirectToAction("Index");
        }

        // 7. REJECT - Doar Admin poate respinge produse
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string? feedbackMessage)
        {
            var product = db.Products.Include(p => p.User).FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            product.Status = "Rejected";
            db.SaveChanges();

            // Trimite notificare catre proposer
            if (!string.IsNullOrEmpty(product.UserId))
            {
                var message = $"Produsul tau '{product.Title}' a fost respins.";
                await _notificationService.AddNotificationAsync(
                    product.UserId, 
                    message, 
                    "product_rejected",
                    product.Id,
                    feedbackMessage);
            }

            TempData["message"] = "Produsul a fost respins.";
            return RedirectToAction("Index");
        }

        // 8. GETPENDINGCOUNT - Returneaza numarul de produse Pending (pentru badge in navbar)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetPendingCount()
        {
            var count = db.Products.Count(p => p.Status == "Pending");
            return Json(new { count });
        }
    }
}