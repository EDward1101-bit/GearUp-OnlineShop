using System.ComponentModel.DataAnnotations;

namespace OnlineShopProject_dNet.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele categoriei este obligatoriu")]
        public string CategoryName { get; set; }

        // Relatia cu produsele (Un produs apartine unei categorii, o categorie are mai multe produse)
        public virtual ICollection<Product>? Products { get; set; }
    }
}