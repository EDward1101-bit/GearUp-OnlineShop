using System.ComponentModel.DataAnnotations;

namespace OnlineShopProject_dNet.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public string? Content { get; set; }

        [Range(1, 5, ErrorMessage = "Rating-ul trebuie sa fie intre 1 si 5")]
        public int? Rating { get; set; }

        public DateTime Date { get; set; }

        // FK către Produs
        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        // FK către User
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}