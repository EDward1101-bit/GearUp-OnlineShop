using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Services
{
    public class CartService(ApplicationDbContext context)
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Metoda centralizată pentru adăugarea produselor în coș.
        /// Gestionează automat: crearea coșului, verificarea stocului și înghețarea prețului.
        /// Returnează TRUE dacă a reușit, FALSE dacă nu e stoc.
        /// </summary>
        public async Task<bool> AddItemToCart(string userId, int productId, int quantity)
        {
            // 1. Validare de bază a produsului
            var product = await _context.Products.FindAsync(productId);

            // Dacă produsul nu există sau stocul e epuizat din start
            if (product == null || product.Stock < 1)
            {
                return false;
            }

            // 2. Găsim sau Creăm Coșul (Order cu status "InCart")
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
                // Salvăm imediat pentru a genera ID-ul comenzii, necesar pentru OrderDetails
                await _context.SaveChangesAsync();
            }

            // 3. Gestionăm linia din coș (OrderDetail)
            var detail = order.OrderDetails.FirstOrDefault(od => od.ProductId == productId);

            if (detail != null)
            {
                // SCENARIUL A: Produsul e deja în coș -> Verificăm stocul cumulat
                // (Cantitatea actuală din coș + ce vrea să adauge acum <= Stocul Real)
                if (detail.Quantity + quantity <= product.Stock)
                {
                    detail.Quantity += quantity;
                }
                else
                {
                    // Stoc insuficient pentru cantitatea totală cerută
                    return false;
                }
            }
            else
            {
                // SCENARIUL B: Produs nou în coș -> Verificăm stocul pentru cantitatea cerută
                if (product.Stock >= quantity)
                {
                    var newDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price // Înghețăm prețul aici!
                    };
                    _context.OrderDetails.Add(newDetail);
                }
                else
                {
                    return false; // Nu avem destule bucăți nici pentru prima adăugare
                }
            }

            // 4. Finalizare: Salvăm modificările în baza de date
            await _context.SaveChangesAsync();
            return true;
        }
    }
}