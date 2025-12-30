using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using OnlineShopProject_dNet.Services;

namespace OnlineShopProject_dNet.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TextProcessingService _textProcessor;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, TextProcessingService textProcessor)
        {
            db = context;
            _userManager = userManager;
            _textProcessor = textProcessor;
        }

        // POST: Adaugarea unui review (doar utilizatori înregistrați)
        [Authorize]
        [HttpPost]
        public IActionResult New(Review rev)
        {
            if (rev == null)
            {
                TempData["message"] = "Datele review-ului sunt invalide.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Products/Index");
            }

            rev.Date = DateTime.Now;
            rev.UserId = _userManager.GetUserId(User);

            // Preserve formatting - sanitize HTML but keep structure
            if (!string.IsNullOrWhiteSpace(rev.Content))
            {
                rev.Content = _textProcessor.ProcessForStorage(_textProcessor.SanitizeHtml(rev.Content));
            }

            // Validare: Verifică dacă utilizatorul are deja un review pentru acest produs
            if (rev.ProductId.HasValue && !string.IsNullOrEmpty(rev.UserId))
            {
                var existingReview = db.Reviews
                    .FirstOrDefault(r => r.ProductId == rev.ProductId.Value && r.UserId == rev.UserId);

                if (existingReview != null)
                {
                    TempData["message"] = "Aveți deja un review pentru acest produs. Puteți edita review-ul existent.";
                    return Redirect("/Products/Show/" + rev.ProductId);
                }

                // Validare IMPORTANTĂ: Verifică dacă utilizatorul a cumpărat produsul
                var hasPurchased = db.OrderDetails
                    .Any(od => od.ProductId == rev.ProductId.Value &&
                              od.Order != null &&
                              od.Order.UserId == rev.UserId &&
                              od.Order.Status == "Placed");

                if (!hasPurchased)
                {
                    TempData["message"] = "Puteți lăsa review-uri doar pentru produsele pe care le-ați cumpărat.";
                    return Redirect("/Products/Show/" + rev.ProductId);
                }
            }

            if (ModelState.IsValid)
            {
                db.Reviews.Add(rev);
                db.SaveChanges();

                // Folosim .HasValue pentru a fi siguri ca nu e null
                if (rev.ProductId.HasValue)
                {
                    SetProductRating(rev.ProductId.Value);
                }

                TempData["message"] = "Review-ul a fost adăugat cu succes!";
                return Redirect("/Products/Show/" + rev.ProductId);
            }

            return Redirect("/Products/Show/" + rev.ProductId);
        }

        // GET: Editare review (doar utilizatori înregistrați)
        [Authorize]
        public IActionResult Edit(int id)
        {
            Review? rev = db.Reviews.Find(id);

            if (rev == null)
            {
                return NotFound();
            }

            if (rev.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să editați acest review!";
                return Redirect("/Products/Show/" + rev.ProductId);
            }

            ViewBag.Review = rev;
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult Edit(int id, Review requestReview)
        {
            Review? rev = db.Reviews.Find(id);

            if (rev == null)
            {
                return NotFound();
            }

            if (rev.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să editați acest review!";
                return Redirect("/Products/Show/" + rev.ProductId);
            }

            try
            {
                // Preserve formatting - sanitize HTML but keep structure
                if (!string.IsNullOrWhiteSpace(requestReview.Content))
                {
                    rev.Content = _textProcessor.ProcessForStorage(_textProcessor.SanitizeHtml(requestReview.Content));
                }
                rev.Rating = requestReview.Rating;

                db.SaveChanges();

                if (rev.ProductId.HasValue)
                {
                    SetProductRating(rev.ProductId.Value);
                }

                return Redirect("/Products/Show/" + rev.ProductId);
            }
            catch (Exception)
            {
                return Redirect("/Products/Show/" + rev.ProductId);
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            Review? rev = db.Reviews.Find(id);

            if (rev == null)
            {
                return NotFound();
            }

            if (rev.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți acest review!";
                return Redirect("/Products/Show/" + rev.ProductId);
            }

            // Salvam ID-ul inainte sa stergem obiectul
            int? productId = rev.ProductId;

            db.Reviews.Remove(rev);
            db.SaveChanges();

            if (productId.HasValue)
            {
                SetProductRating(productId.Value);
            }

            return Redirect("/Products/Show/" + productId);
        }

        // METODA PRIVATA PENTRU CALCULUL MEDIEI
        private void SetProductRating(int productId)
        {
            var product = db.Products.Include(p => p.Reviews).FirstOrDefault(p => p.Id == productId);

            // Verificam explicit daca produsul si review-urile exista
            if (product != null && product.Reviews.Any())
            {
                // Selectam rating-urile care au valoare (nu sunt null)
                var validRatings = product.Reviews.Where(r => r.Rating.HasValue).Select(r => r.Rating!.Value);

                if (validRatings.Any())
                {
                    float average = (float)validRatings.Average(r => (double)r);
                    product.Rating = average;
                }
                else
                {
                    product.Rating = null;
                }
            }
            else if (product != null)
            {
                product.Rating = null;
            }

            db.SaveChanges();
        }
    }
}