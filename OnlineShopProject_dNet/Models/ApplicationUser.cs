using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OnlineShopProject_dNet.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

      
        // Un user (Proposer) propune produse
        public virtual ICollection<Product>? Products { get; set; }

        // Un user (Inregistrat) lasă review-uri (în diagramă Review are UserId)
        public virtual ICollection<Review>? Reviews { get; set; }


        // Relatia 1-M cu Orders
        public virtual ICollection<Order>? Orders { get; set; }

        // Relatia M-M cu Products (prin Wishlist)
        public virtual ICollection<Wishlist>? Wishlists { get; set; }
    }
}