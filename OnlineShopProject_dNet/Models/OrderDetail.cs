using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShopProject_dNet.Models
{
    public class OrderDetail
    {
        // Cheia primara compusa va fi definita in Context, aici avem doar proprietatile
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required(ErrorMessage = "Cantitatea este obligatorie")]
        [Range(1, int.MaxValue, ErrorMessage = "Cantitatea trebuie să fie cel puțin 1")]
        public int Quantity { get; set; }

        // Salvam pretul la momentul comenzii (in caz ca pretul produsului se schimba ulterior)
        [Required(ErrorMessage = "Pretul unitar este obligatoriu")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Prețul trebuie să fie pozitiv")]
        public decimal UnitPrice { get; set; }
    }
}