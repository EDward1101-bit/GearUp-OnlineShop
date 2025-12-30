using System.ComponentModel.DataAnnotations;

namespace OnlineShopProject_dNet.Models
{
    public class FAQ
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Question { get; set; } = null!;

        [Required]
        public string Answer { get; set; } = null!;

        // Legătură cu produsul (opțional - FAQ-uri generale)
        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        // Câți utilizatori au găsit această întrebare utilă
        public int HelpfulCount { get; set; } = 0;
    }
}
