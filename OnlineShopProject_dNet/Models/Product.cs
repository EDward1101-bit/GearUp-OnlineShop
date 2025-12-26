using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShopProject_dNet.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [StringLength(100, ErrorMessage = "Titlul nu poate avea mai mult de 100 de caractere")]
        [MinLength(3, ErrorMessage = "Titlul trebuie sa aiba mai mult de 3 caractere")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        public string Description { get; set; } = null!;

        public string? Image { get; set; }

        [Required(ErrorMessage = "Pretul este obligatoriu")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stocul este obligatoriu")]
        public int Stock { get; set; }

        // Rating-ul mediu
        public float? Rating { get; set; }

        //TODO: Schimba tipul in string
        // Status: "Pending", "Approved", "Rejected"
        public string? Status { get; set; }

        // Relația cu Categoria
        [Required(ErrorMessage = "Categoria este obligatorie")]
        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        // Relația cu Userul (Proposer)
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}