using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Controllers
{
    [Authorize]
    public class WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // 1. INDEX - Afișarea listei
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null) return RedirectToAction("Index", "Home"); // Safety Check

            var wishlist = await db.Wishlists
                                   .Include(w => w.Product)
                                   .Where(w => w.UserId == userId)
                                   .ToListAsync();
            return View(wishlist);
        }

        // 2. TOGGLE - Inimioara Inteligentă (AJAX)
        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { success = false, message = "Trebuie să fii autentificat." });

            var productExists = await db.Products.AnyAsync(p => p.Id == productId);
            if (!productExists) return Json(new { success = false, message = "Produs invalid" });

            var existingItem = await db.Wishlists
                                       .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
            {
                // SCENARIUL A: Produsul exista -> ÎL ȘTERGEM (Undo)
                db.Wishlists.Remove(existingItem);
                await db.SaveChangesAsync();
                return Json(new { success = true, action = "removed", message = "Produsul a fost scos de la favorite." });
            }
            else
            {
                // SCENARIUL B: Nu exista -> ÎL ADĂUGĂM
                var newItem = new Wishlist { UserId = userId, ProductId = productId };
                db.Wishlists.Add(newItem);
                await db.SaveChangesAsync();
                return Json(new { success = true, action = "added", message = "Produsul a fost adăugat la favorite!" });
            }
        }

        // 3. DELETE - Butonul de "Gunoi" din pagina de Wishlist
        [HttpPost]
        public async Task<IActionResult> Delete(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Index", "Home");

            var item = await db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item != null)
            {
                db.Wishlists.Remove(item);
                await db.SaveChangesAsync();
                TempData["message"] = "Produsul a fost șters.";
            }
            return RedirectToAction("Index");
        }

        // ZONA 2: TRANSFER ÎN COȘ (Cu validare strictă de stoc)

        // 2.1 COPY TO CART - Un singur produs
        [HttpPost]
        public async Task<IActionResult> AddToCartFromWishlist(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Index", "Home");

            // 1. Căutăm produsul în Wishlist
            var wishlistItem = await db.Wishlists
                                       .Include(w => w.Product)
                                       .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            // Verificăm dacă item-ul sau produsul sunt null
            if (wishlistItem == null || wishlistItem.Product == null)
            {
                return RedirectToAction("Index");
            }

            // 2. Încercăm să adăugăm în coș (acum wishlistItem.Product sigur nu e null)
            bool success = await AddProductToCartInternal(userId, wishlistItem.Product);

            if (success)
            {
                TempData["message"] = "Produsul a fost copiat în coș!";
            }
            else
            {
                TempData["message"] = "Nu s-a putut adăuga! Stoc insuficient sau limita atinsă.";
            }

            return RedirectToAction("Index");
        }

        // 2.2 ADD ALL TO CART - Tot ce e valid
        [HttpPost]
        public async Task<IActionResult> AddAllToCart()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Index", "Home");

            // 1. Luăm DOAR produsele care au stoc fizic > 0 si nu sunt null
            var validItems = await db.Wishlists
                                     .Include(w => w.Product)
                                     .Where(w => w.UserId == userId && w.Product != null && w.Product.Stock > 0)
                                     .ToListAsync();

            if (validItems.Count == 0)
            {
                TempData["message"] = "Nu ai produse disponibile în stoc pentru a fi adăugate.";
                return RedirectToAction("Index");
            }

            int countAdded = 0;

            // 2. Iterăm și încercăm să adăugăm fiecare produs
            foreach (var item in validItems)
            {
                // Extra check: Deși filtrul Where s-a asigurat, compiler-ul vrea să fie sigur aici
                if (item.Product != null)
                {
                    bool result = await AddProductToCartInternal(userId, item.Product);
                    if (result)
                    {
                        countAdded++;
                    }
                }
            }

            TempData["message"] = $"{countAdded} produse au fost adăugate în coș!";
            return RedirectToAction("Index");
        }

        // ZONA 3: HELPER PRIVAT (Creierul operațiunii)
        private async Task<bool> AddProductToCartInternal(string userId, Product product)
        {
            // Validare de bază
            if (product.Stock <= 0) return false;

            // A. Găsim sau Creăm Coșul
            var order = await db.Orders
                                .Include(o => o.OrderDetails)
                                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            if (order == null)
            {
                order = new Order
                {
                    UserId = userId,
                    Date = DateTime.Now,
                    Status = "InCart",
                    TotalAmount = 0
                };
                db.Orders.Add(order);
                await db.SaveChangesAsync();
            }

            // B. Verificăm produsul în coș
            var detail = order.OrderDetails.FirstOrDefault(od => od.ProductId == product.Id);

            if (detail != null)
            {
                // SCENARIUL 1: Produsul e deja în coș.
                if (detail.Quantity + 1 <= product.Stock)
                {
                    detail.Quantity++;
                }
                else
                {
                    return false; // Eșec: Ar depăși stocul
                }
            }
            else
            {
                // SCENARIUL 2: Produs nou în coș.
                if (product.Stock >= 1)
                {
                    var newDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = 1,
                        UnitPrice = product.Price // Înghețăm prețul
                    };
                    db.OrderDetails.Add(newDetail);
                }
                else
                {
                    return false;
                }
            }

            await db.SaveChangesAsync();
            return true; // Succes
        }
    }
}