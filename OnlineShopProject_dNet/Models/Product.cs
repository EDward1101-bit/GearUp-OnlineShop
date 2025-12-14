using System.ComponentModel.DataAnnotations;

namespace OnlineShopProject_dNet.Models
{
    public class Product
    {

        // Atribute pentru validare
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price required")]
        public int Price { get; set; }

        [Required(ErrorMessage = "Stock required")]
        public int Stock { get; set; }

        public bool Status { get; set; } = false; // Will be based on stock



        // Daca se sterge o categorie, se sterg si produsele din acea categorie
        // relatie configuranta folosind conventiile de nume din EF
        [Required(ErrorMessage = "Product category required")]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        // propserId to link to the user who added the product
    }
}
