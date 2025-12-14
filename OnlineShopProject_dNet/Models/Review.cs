using System.ComponentModel.DataAnnotations;

namespace OnlineShopProject_dNet.Models
{
    public class Review
    {
        // Atribute pentru validare
        [Key]
        public int Id { get; set; }
        public int Rating { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime Date { get; set; }


        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        // UserId to link to the user who made the review
    }
}
