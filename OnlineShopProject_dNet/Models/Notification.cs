using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineShopProject_dNet.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public int? ProductId { get; set; }

        [Required]
        [StringLength(300)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(1000)]
        public string? FeedbackMessage { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
