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

        // 4. COPY TO CART (Single)
        [HttpPost]
        public async Task<IActionResult> AddToCartFromWishlist(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Index", "Home");

            // Verificăm doar dacă e în wishlist-ul tău (Securitate)
            // Nu mai verificăm stocul aici, se ocupă Serviciul
            var inWishlist = await db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (!inWishlist) return RedirectToAction("Index");

            // APELĂM SERVICIUL
            bool success = await _cartService.AddItemToCart(userId, productId, 1);

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

        // 5. ADD ALL TO CART
        [HttpPost]
        public async Task<IActionResult> AddAllToCart()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Index", "Home");

            // Luăm doar ID-urile produselor din wishlist
            var wishlistItems = await db.Wishlists
                                        .Where(w => w.UserId == userId)
                                        .ToListAsync();

            if (wishlistItems.Count == 0)
            {
                TempData["message"] = "Nu ai produse în wishlist.";
                return RedirectToAction("Index");
            }

            int countAdded = 0;

            foreach (var item in wishlistItems)
            {
                // APELĂM SERVICIUL PENTRU FIECARE
                // Serviciul verifică singur dacă mai e stoc > 0. 
                // Dacă stocul e 0, returnează false și nu se întâmplă nimic rău.
                bool result = await _cartService.AddItemToCart(userId, item.ProductId, 1);

                if (result)
                {
                    countAdded++;
                }
            }

            if (countAdded > 0)
                TempData["message"] = $"{countAdded} produse au fost adăugate în coș!";
            else
                TempData["message"] = "Niciun produs nu a putut fi adăugat (stoc epuizat).";

            return RedirectToAction("Index");
        }

        // NOTĂ: Metoda AddProductToCartInternal a fost ștearsă complet!
        // Codul este acum mult mai simplu și folosește logica centralizată din CartService.
    }
}