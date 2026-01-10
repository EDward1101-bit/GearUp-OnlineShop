using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Services
{
    public class CartService(ApplicationDbContext context)
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Metoda centralizata pentru adaugarea produselor in cos.
        /// Gestioneaza automat: crearea cosului, verificarea stocului si inghetarea pretului.
        /// Returneaza TRUE daca a reusit, FALSE daca nu e stoc.
        /// </summary>
        public async Task<bool> AddItemToCart(string userId, int productId, int quantity)
        {
            // 1. Validare de baza a produsului
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            // Daca produsul nu exista sau stocul e epuizat din start
            if (product == null || (product.Stock ?? 0) < 1)
            {
                return false;
            }

            // 2. Gasim sau Cream Cosul (Order cu status "InCart")
            var order = await _context.Orders
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
                _context.Orders.Add(order);
                // Salvam imediat pentru a genera ID-ul comenzii, necesar pentru OrderDetails
                await _context.SaveChangesAsync();
            }

            // 3. Gestionam linia din cos (OrderDetail)
            var detail = order.OrderDetails.FirstOrDefault(od => od.ProductId == productId);

            if (detail != null)
            {
                // SCENARIUL A: Produsul e deja in cos -> Verificam stocul cumulat
                // (Cantitatea actuala din cos + ce vrea sa adauge acum <= Stocul Real)
                if (detail.Quantity + quantity <= (product.Stock ?? 0))
                {
                    detail.Quantity += quantity;
                    // Asiguram snapshot-ul pentru istoricul comenzilor
                    detail.ProductTitleSnapshot ??= product.Title;
                    detail.ProductImageSnapshot ??= product.Image;
                    detail.ProductCategorySnapshot ??= product.Category?.Name;
                }
                else
                {
                    // Stoc insuficient pentru cantitatea totala ceruta
                    return false;
                }
            }
            else
            {
                // SCENARIUL B: Produs nou in cos -> Verificam stocul pentru cantitatea ceruta
                if ((product.Stock ?? 0) >= quantity)
                {
                    var newDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price ?? 0, // Inghetam pretul aici!
                        ProductTitleSnapshot = product.Title,
                        ProductImageSnapshot = product.Image,
                        ProductCategorySnapshot = product.Category?.Name
                    };
                    _context.OrderDetails.Add(newDetail);
                }
                else
                {
                    return false; // Nu avem destule bucati nici pentru prima adaugare
                }
            }

            // 4. Finalizare: Salvam modificarile in baza de date
            await _context.SaveChangesAsync();
            return true;
        }
    }
}