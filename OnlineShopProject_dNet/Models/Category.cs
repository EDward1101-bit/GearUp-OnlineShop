using System.ComponentModel.DataAnnotations;

namespace OnlineShopProject_dNet.Models
{
    public class Category
    {
        // Atribute pentru validare
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Category name required")]
        public string Name { get; set; } = string.Empty;


        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
