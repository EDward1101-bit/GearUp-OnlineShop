using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShopProject_dNet.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public DateTime Date { get; set; }

        // Statusul va fi folosit pentru a diferenția coșul de comanda plasată
        // Ex: "InCos", "Plasata", "Finalizata"
        public string? Status { get; set; }

        // Adresa poate fi null cat timp e doar in stadiul de Cos
        [StringLength(200, ErrorMessage = "Adresa nu poate depăși 200 de caractere")]
        public string? ShippingAddress { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        // Relatia cu produsele prin tabelul asociativ OrderDetail
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
    }
}