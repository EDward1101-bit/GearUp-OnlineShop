using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using OnlineShopProject_dNet.Services; // 1. IMPORT IMPORTANT

namespace OnlineShopProject_dNet.Controllers
{
    [Authorize]
    public class OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, CartService cartService) : Controller
    {
        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CartService _cartService = cartService; // 2. DEFINIRE SERVICIU

        // 1. INDEX - Afișarea Coșului de Cumpărături
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Căutăm comanda cu status "InCart" pentru userul curent
            var cart = await db.Orders
                               .Include(o => o.OrderDetails)
                               .ThenInclude(od => od.Product)
                               .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            if (cart != null)
            {
                // Calculăm totalul folosind prețul salvat (UnitPrice), ignorând modificările din magazin
                cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

                // Verificăm stocul pentru a afișa avertismente în View
                bool hasStockIssues = false;

                foreach (var item in cart.OrderDetails)
                {
                    // Dacă produsul a fost șters sau cantitatea din coș depășește stocul actual
                    if (item.Product != null && item.Quantity > item.Product.Stock)
                    {
                        hasStockIssues = true;
                    }
                }

                if (hasStockIssues)
                {
                    ViewBag.ErrorMessage = "Unele produse din coș nu mai sunt disponibile în cantitatea selectată. Te rugăm să actualizezi coșul înainte de a comanda.";
                    ViewBag.HasStockIssues = true;
                }
            }
            return View(cart);
        }


        // 2. ADD TO CART - REFACTORIZAT (Mult mai scurt!)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            if (quantity < 1) quantity = 1;
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Json(new { success = false, message = "Eroare: Utilizator neautentificat." });
            }

            // delegam munca catre serviciu
            bool success = await _cartService.AddItemToCart(userId, productId, quantity);
            if (!success)
            {
                return Json(new { success = false, message = "Stoc insuficient sau produs invalid!" });
            }

            return Json(new { success = true, message = "Produsul a fost adăugat în coș!" });
        }


        /// 3. REMOVE FROM CART - Șterge un produs din coș
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = _userManager.GetUserId(User);

            var order = await db.Orders
                                .Include(o => o.OrderDetails)
                                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            if (order != null)
            {
                var orderDetail = order.OrderDetails.FirstOrDefault(od => od.ProductId == productId);
                if (orderDetail != null)
                {
                    db.OrderDetails.Remove(orderDetail);

                    // --- OPTIMIZARE: Dacă coșul rămâne gol, ștergem și antetul comenzii ---
                    if (order.OrderDetails.Count == 1) // Era 1, acum devine 0
                    {
                        db.Orders.Remove(order);
                    }

                    await db.SaveChangesAsync();
                    TempData["message"] = "Produsul a fost eliminat din coș.";
                }
            }
            return RedirectToAction("Index");
        }


        // 4. UPDATE QUANTITY - Modifică cantitatea (+/-)
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            var order = await db.Orders
                                .Include(o => o.OrderDetails)
                                .ThenInclude(od => od.Product)
                                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            if (order != null)
            {
                var orderDetail = order.OrderDetails.FirstOrDefault(od => od.ProductId == productId);

                if (orderDetail != null)
                {
                    // A. Verificăm dacă userul vrea să șteargă (cantitate 0)
                    if (quantity <= 0)
                    {
                        db.OrderDetails.Remove(orderDetail);
                        TempData["message"] = "Produsul a fost eliminat.";
                    }
                    else
                    {
                        // B. Verificăm STOCUL DISPONIBIL [Important!]
                        if (orderDetail.Product != null && orderDetail.Product.Stock < quantity)
                        {
                            TempData["message"] = $"Stoc insuficient! Doar {orderDetail.Product.Stock} bucăți disponibile.";
                        }
                        else
                        {
                            orderDetail.Quantity = quantity;
                        }
                    }
                    await db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }
        

        // 5. CHECKOUT (Pasul 1 - Afișare Formular)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);

            var cart = await db.Orders
                               .Include(o => o.OrderDetails)
                               .ThenInclude(od => od.Product)
                               .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            // Nu poți face checkout la un coș gol
            if (cart == null || cart.OrderDetails.Count == 0)
            {
                TempData["message"] = "Coșul tău este gol.";
                return RedirectToAction("Index");
            }

            // Validăm stocul înainte să lăsăm omul să completeze adresa
            foreach (var item in cart.OrderDetails)
            {
                if (item.Product != null && item.Quantity > item.Product.Stock)
                {
                    TempData["message"] = "Nu poți finaliza comanda deoarece ai produse cu stoc insuficient!";
                    return RedirectToAction("Index"); // Îl întoarcem în coș
                }
            }
            cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

            // Aici userul va vedea suma finală și va completa adresa
            return View(cart);
        }


        // 6. CHECKOUT [POST] - Finalizarea efectivă a comenzii
        [HttpPost]
        public async Task<IActionResult> Checkout(Order requestOrder)
        {
            var userId = _userManager.GetUserId(User);

            var cart = await db.Orders
                               .Include(o => o.OrderDetails)
                               .ThenInclude(od => od.Product)
                               .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            if (cart == null || cart.OrderDetails.Count == 0)
            {
                TempData["message"] = "Coșul este gol.";
                return RedirectToAction("Index");
            }

            // VALIDARE ADRESĂ: Este obligatoriu să completăm adresa
            if (string.IsNullOrWhiteSpace(requestOrder.ShippingAddress))
            {
                TempData["message"] = "Te rugăm să completezi adresa de livrare!";
                cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
                return View(cart);
            }

            // ULTIMA VERIFICARE DE STOC (Security Check)
            foreach (var item in cart.OrderDetails)
            {
                if (item.Product == null || item.Quantity > item.Product.Stock)
                {
                    TempData["message"] = $"Produsul nu mai este pe stoc. Actualizează coșul.";
                    return RedirectToAction("Index");
                }
            }

            // Procesarea Comenzii - SCĂDEREA STOCULUI
            foreach (var item in cart.OrderDetails)
            {
                if (item.Product != null)
                {
                    item.Product.Stock -= item.Quantity;
                }
            }

            // Finalizarea datelor
            cart.Status = "Placed";
            cart.Date = DateTime.Now;
            cart.ShippingAddress = requestOrder.ShippingAddress;
            cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

            await db.SaveChangesAsync();

            TempData["success"] = "Comanda a fost plasată cu succes! Vei primi un email de confirmare.";
            return RedirectToAction("OrderSuccess", new { orderId = cart.Id });
        }

        // 9. ORDER SUCCESS PAGE - Afișare mesaj după plasarea comenzii
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            var userId = _userManager.GetUserId(User);

            var order = await db.Orders
                                .Include(o => o.OrderDetails)
                                .ThenInclude(od => od.Product)
                                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId && o.Status == "Placed");

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        // 7. ISTORIC COMENZI (MyOrders) - Lista tuturor comenzilor plasate
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);

            // Luăm toate comenzile care NU mai sunt în stadiul de "Coș"
            // Include OrderDetails și Product pentru a afișa corect informațiile
            var orders = await db.Orders
                                 .Include(o => o.OrderDetails)
                                 .ThenInclude(od => od.Product)
                                 .ThenInclude(p => p.Category)
                                 .Where(o => o.UserId == userId && o.Status != "InCart")
                                 .OrderByDescending(o => o.Date) // Cele mai recente primele
                                 .ToListAsync();

            return View(orders);
        }

        // 8. DETALII COMANDĂ (Details) - Ce produse sunt într-o comandă veche
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Căutăm comanda specifică (id) și încărcăm produsele cu categoria
            // Verificăm și UserId pentru securitate (să nu vezi comenzile altuia)
            var order = await db.Orders
                                .Include(o => o.OrderDetails)
                                .ThenInclude(od => od.Product)
                                .ThenInclude(p => p.Category)
                                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                // Dacă comanda nu există sau nu e a ta
                return NotFound();
            }

            // Calculăm totalul dacă nu este setat
            if (order.TotalAmount == 0 && order.OrderDetails.Any())
            {
                order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
            }

            return View(order);
        }
    }
}