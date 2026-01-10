using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;
using OnlineShopProject_dNet.Services;

namespace OnlineShopProject_dNet.Controllers
{
    [Authorize]
    public class OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, CartService cartService) : Controller
    {
        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly CartService _cartService = cartService;

        // 1. INDEX - Afisarea Cosului de Cumparaturi
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Cautam comanda cu status "InCart" pentru userul curent
            var cart = await db.Orders
                               .Include(o => o.OrderDetails)
                               .ThenInclude(od => od.Product)
                               .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            if (cart != null)
            {
                // Daca exista linii fara produs (ex: produs sters), le eliminam din cos
                var orphanLines = cart.OrderDetails.Where(od => od.Product == null && od.ProductId == null).ToList();
                if (orphanLines.Any())
                {
                    db.OrderDetails.RemoveRange(orphanLines);
                    await db.SaveChangesAsync();
                    cart = await db.Orders
                                   .Include(o => o.OrderDetails)
                                   .ThenInclude(od => od.Product)
                                   .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");
                }

                // Calculam totalul folosind pretul salvat (UnitPrice), ignorand modificarile din magazin
                cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

                // Verificam stocul pentru a afisa avertismente in View
                bool hasStockIssues = false;

                foreach (var item in cart.OrderDetails)
                {
                    // Daca produsul a fost sters sau cantitatea din cos depaseste stocul actual
                    if (item.Product != null && item.Quantity > item.Product.Stock.GetValueOrDefault())
                    {
                        hasStockIssues = true;
                    }
                }

                if (hasStockIssues)
                {
                    ViewBag.ErrorMessage = "Unele produse din cos nu mai sunt disponibile in cantitatea selectata. Te rugam sa actualizezi cosul inainte de a comanda.";
                    ViewBag.HasStockIssues = true;
                }
            }
            return View(cart);
        }


        // 2. ADD TO CART - REFACTORIZAT
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

            return Json(new { success = true, message = "Produsul a fost adaugat in cos!" });
        }


        // 3. REMOVE FROM CART - Sterge un produs din cos
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

                    // OPTIMIZARE: Daca cosul ramane gol, stergem si antetul comenzii
                    if (order.OrderDetails.Count == 1) // Era 1, acum devine 0
                    {
                        db.Orders.Remove(order);
                    }

                    await db.SaveChangesAsync();
                    TempData["message"] = "Produsul a fost eliminat din cos.";
                }
            }
            return RedirectToAction("Index");
        }


        // 4. UPDATE QUANTITY - Modifica cantitatea (+/-)
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
                    // A. Verificam daca userul vrea sa stearga (cantitate 0)
                    if (quantity <= 0)
                    {
                        db.OrderDetails.Remove(orderDetail);
                        TempData["message"] = "Produsul a fost eliminat.";
                    }
                    else
                    {
                        // B. Verificam STOCUL DISPONIBIL
                        if (orderDetail.Product != null && orderDetail.Product.Stock < quantity)
                        {
                            TempData["message"] = $"Stoc insuficient! Doar {orderDetail.Product.Stock} bucati disponibile.";
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
        

        // 5. CHECKOUT (Pasul 1 - Afisare Formular)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);

            var cart = await db.Orders
                               .Include(o => o.OrderDetails)
                               .ThenInclude(od => od.Product)
                               .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "InCart");

            // Nu poti face checkout la un cos gol
            if (cart == null || cart.OrderDetails.Count == 0)
            {
                TempData["message"] = "Cosul tau este gol.";
                return RedirectToAction("Index");
            }

            // Validam stocul inainte sa lasam omul sa completeze adresa
            foreach (var item in cart.OrderDetails)
            {
                if (item.Product != null && item.Quantity > item.Product.Stock.GetValueOrDefault())
                {
                    TempData["message"] = "Nu poti finaliza comanda deoarece ai produse cu stoc insuficient!";
                    return RedirectToAction("Index"); // Il intoarcem in cos
                }
            }
            cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

            // Aici userul va vedea suma finala si va completa adresa
            return View(cart);
        }


        // 6. CHECKOUT [POST] - Finalizarea efectiva a comenzii
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
                TempData["message"] = "Cosul este gol.";
                return RedirectToAction("Index");
            }

            // VALIDARE ADRESA: Este obligatoriu sa completam adresa
            if (string.IsNullOrWhiteSpace(requestOrder.ShippingAddress))
            {
                TempData["message"] = "Te rugam sa completezi adresa de livrare!";
                cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
                return View(cart);
            }

            // ULTIMA VERIFICARE DE STOC (Security Check)
            foreach (var item in cart.OrderDetails)
            {
                if (item.Product != null)
                {
                    // Ne asiguram ca snapshot-ul este complet pentru istoricul comenzilor
                    item.ProductTitleSnapshot ??= item.Product.Title;
                    item.ProductImageSnapshot ??= item.Product.Image;
                    item.ProductCategorySnapshot ??= item.Product.Category?.Name;
                }

                if (item.Product == null || item.Quantity > item.Product.Stock.GetValueOrDefault())
                {
                    TempData["message"] = $"Produsul nu mai este pe stoc. Actualizeaza cosul.";
                    return RedirectToAction("Index");
                }
            }

            // Procesarea Comenzii - SCADEREA STOCULUI
            foreach (var item in cart.OrderDetails)
            {
                if (item.Product != null)
                {
                    item.Product.Stock = (item.Product.Stock ?? 0) - item.Quantity;
                }
            }

            // Finalizarea datelor
            cart.Status = "Placed";
            cart.Date = DateTime.Now;
            cart.ShippingAddress = requestOrder.ShippingAddress;
            cart.TotalAmount = cart.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

            await db.SaveChangesAsync();

            TempData["success"] = "Comanda a fost plasata cu succes! Vei primi un email de confirmare.";
            return RedirectToAction("OrderSuccess", new { orderId = cart.Id });
        }

        // 9. ORDER SUCCESS PAGE - Afisare mesaj dupa plasarea comenzii
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

            // Luam toate comenzile care NU mai sunt in stadiul de "Cos"
            // Include OrderDetails si Product pentru a afisa corect informatiile
            var orders = await db.Orders
                                 .Include(o => o.OrderDetails)
                                 .ThenInclude(od => od.Product)
                                 .ThenInclude(p => p.Category)
                                 .Where(o => o.UserId == userId && o.Status != "InCart")
                                 .OrderByDescending(o => o.Date) // Cele mai recente primele
                                 .ToListAsync();

            return View(orders);
        }

        // 8. DETALII COMANDA (Details) - Ce produse sunt intr-o comanda veche
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Cautam comanda specifica (id) si incarcam produsele cu categoria
            // Verificam si UserId pentru securitate (sa nu vezi comenzile altuia)
            var order = await db.Orders
                                .Include(o => o.OrderDetails)
                                .ThenInclude(od => od.Product)
                                .ThenInclude(p => p.Category)
                                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                // Daca comanda nu exista sau nu e a ta
                return NotFound();
            }

            // Calculam totalul daca nu este setat
            if (order.TotalAmount == 0 && order.OrderDetails.Any())
            {
                order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);
            }

            return View(order);
        }
    }
}