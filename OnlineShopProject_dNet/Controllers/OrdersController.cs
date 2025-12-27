using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Controllers
{
    [Authorize] // Doar utilizatorii logati pot avea cos
    public class OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;


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


        // 2. ADD TO CART - Varianta AJAX (Modificată)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            // Validare simplă (cantitate minimă 1)
            if (quantity < 1) quantity = 1;

            var userId = _userManager.GetUserId(User);
            var product = await db.Products.FindAsync(productId);

            if (product == null) return Json(new { success = false, message = "Produsul nu a fost găsit." });

            // 1. Validare Stoc (Returnăm JSON cu eroare)
            if (product.Stock < quantity)
            {
                return Json(new { success = false, message = "Stoc insuficient!" });
            }

            // 2. Găsim sau creăm coșul
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

            var orderDetail = order.OrderDetails.FirstOrDefault(od => od.ProductId == productId);

            if (orderDetail != null)
            {
                // Validare cumulativă (Ce e în coș + Ce adaugă acum)
                if (product.Stock < orderDetail.Quantity + quantity)
                {
                    return Json(new { success = false, message = "Nu poți adăuga mai mult decât stocul disponibil!" });
                }
                orderDetail.Quantity += quantity;
            }
            else
            {
                var newOrderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price // Înghețăm prețul
                };
                db.OrderDetails.Add(newOrderDetail);
            }

            await db.SaveChangesAsync();

            // AICI E SCHIMBAREA MAJORĂ: Returnăm JSON
            return Json(new { success = true, message = "Produsul a fost adăugat în coș!", cartCount = order.OrderDetails.Sum(x => x.Quantity) });
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

            // 1. VALIDARE ADRESĂ: Este obligatoriu să completăm adresa
            if (string.IsNullOrWhiteSpace(requestOrder.ShippingAddress))
            {
                TempData["message"] = "Te rugăm să completezi adresa de livrare!";

                // Recalculăm totalul pentru a reafisa corect pagina (fiindcă nu s-a salvat încă)
                cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
                return View(cart);
            }

            // 2. ULTIMA VERIFICARE DE STOC (Security Check)
            foreach (var item in cart.OrderDetails)
            {
                // Verificăm dacă produsul e null sau stocul e insuficient
                if (item.Product == null || item.Quantity > item.Product.Stock)
                {
                    TempData["message"] = $"Produsul {item.Product?.Title} nu mai este pe stoc. Actualizează coșul.";
                    return RedirectToAction("Index");
                }
            }

            // 3. Procesarea Comenzii - SCĂDEREA STOCULUI
            foreach (var item in cart.OrderDetails)
            {
                // Verificare de siguranță pentru a evita warning-urile
                if (item.Product != null)
                {
                    item.Product.Stock -= item.Quantity;
                }
            }

            // 4. Finalizarea datelor
            cart.Status = "Placed";
            cart.Date = DateTime.Now;
            cart.ShippingAddress = requestOrder.ShippingAddress; // Salvăm adresa validată
            cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

            await db.SaveChangesAsync();

            TempData["message"] = "Comanda a fost plasată cu succes!";
            return RedirectToAction("Index", "Products");
        }
    }
}