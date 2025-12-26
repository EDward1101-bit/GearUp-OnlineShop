using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShopProject_dNet.Models
{
    public class Wishlist
    {
        // Cheie compusa (UserId + ProductId) definita in Context
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
    }
}