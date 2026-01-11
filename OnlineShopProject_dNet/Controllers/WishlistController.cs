using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using OnlineShopProject_dNet.Services;

namespace OnlineShopProject_dNet.Controllers
{
    [Authorize]
    public class WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, CartService cartService) : Controller
    {
        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CartService _cartService = cartService;
        
        
        // 1. INDEX - Afisarea listei
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

        // 2. TOGGLE - Inimioara Inteligenta (AJAX)
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) 
            {
                return Json(new { 
                    success = false, 
                    requiresAuth = true,
                    message = "Pentru a continua, autentifică-te sau creează un cont" 
                });
            }

            var productExists = await db.Products.AnyAsync(p => p.Id == productId);
            if (!productExists) return Json(new { success = false, message = "Produs invalid" });

            var existingItem = await db.Wishlists
                                       .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
            {
                // SCENARIUL A: Produsul exista -> IL STERGEM (Undo)
                db.Wishlists.Remove(existingItem);
                await db.SaveChangesAsync();
                var wishlistCount = await db.Wishlists.CountAsync(w => w.UserId == userId);
                return Json(new { success = true, action = "removed", message = "Produsul a fost scos de la favorite.", wishlistCount });
            }
            else
            {
                // SCENARIUL B: Nu exista -> IL ADAUGAM
                var newItem = new Wishlist { UserId = userId, ProductId = productId };
                db.Wishlists.Add(newItem);
                await db.SaveChangesAsync();
                var wishlistCount = await db.Wishlists.CountAsync(w => w.UserId == userId);
                return Json(new { success = true, action = "added", message = "Produsul a fost adaugat la favorite!", wishlistCount });
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
                TempData["message"] = "Produsul a fost sters.";
            }
            return RedirectToAction("Index");
        }

        // 4. COPY TO CART (Single)
        [HttpPost]
        public async Task<IActionResult> AddToCartFromWishlist(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Index", "Home");

            // Verificam doar daca e in wishlist-ul tau (Securitate)
            // Nu mai verificam stocul aici, se ocupa Serviciul
            var inWishlist = await db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (!inWishlist) return RedirectToAction("Index");

            // APELAM SERVICIUL
            bool success = await _cartService.AddItemToCart(userId, productId, 1);

            if (success)
            {
                TempData["message"] = "Produsul a fost adaugat in cos!";
            }
            else
            {
                TempData["message"] = "Nu s-a putut adauga! Stoc insuficient sau limita atinsa.";
            }

            return RedirectToAction("Index");
        }

        // 5. ADD ALL TO CART
        [HttpPost]
        public async Task<IActionResult> AddAllToCart()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Index", "Home");

            // Luam doar ID-urile produselor din wishlist
            var wishlistItems = await db.Wishlists
                                        .Where(w => w.UserId == userId)
                                        .ToListAsync();

            if (wishlistItems.Count == 0)
            {
                TempData["message"] = "Nu ai produse in wishlist.";
                return RedirectToAction("Index");
            }

            int countAdded = 0;

            foreach (var item in wishlistItems)
            {
                // APELAM SERVICIUL PENTRU FIECARE
                // Serviciul verifica singur daca mai e stoc > 0. 
                // Daca stocul e 0, returneaza false si nu se intampla nimic rau.
                bool result = await _cartService.AddItemToCart(userId, item.ProductId, 1);

                if (result)
                {
                    countAdded++;
                }
            }

            if (countAdded > 0)
                TempData["message"] = $"{countAdded} produse au fost adaugate in cos!";
            else
                TempData["message"] = "Niciun produs nu a putut fi adaugat (stoc epuizat).";

            return RedirectToAction("Index");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Count()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { count = 0 });

            var count = await db.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { count });
        }

        // 6. MERGE LOCAL WISHLIST - Merge localStorage wishlist into user's server-side wishlist after login
        [HttpPost]
        [IgnoreAntiforgeryToken] // JSON requests with [FromBody] don't need form tokens
        public async Task<IActionResult> MergeLocalWishlist([FromBody] List<LocalWishlistItem>? items)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (items == null || items.Count == 0) return Json(new { success = true, merged = 0 });

            int merged = 0;
            foreach (var item in items)
            {
                if (item == null || item.productId <= 0) continue;
                
                // Check if product exists
                var productExists = await db.Products.AnyAsync(p => p.Id == item.productId && p.Status == "Approved");
                if (!productExists) continue;

                // Check if already in wishlist
                var exists = await db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == item.productId);
                if (exists) continue;

                // Add to wishlist
                db.Wishlists.Add(new Wishlist { UserId = userId, ProductId = item.productId });
                merged++;
            }

            if (merged > 0)
            {
                await db.SaveChangesAsync();
            }

            var totalCount = await db.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { success = true, merged, wishlistCount = totalCount });
        }

        public class LocalWishlistItem
        {
            public int productId { get; set; }
        }
    }
}