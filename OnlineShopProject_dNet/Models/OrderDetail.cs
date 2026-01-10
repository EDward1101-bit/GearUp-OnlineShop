using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShopProject_dNet.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        // Legatura catre comanda
        [Required]
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        // Legatura catre produs devine optionala pentru a putea sterge produsele
        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        // Copie locala a datelor produsului pentru istoricul comenzilor
        public string? ProductTitleSnapshot { get; set; }
        public string? ProductImageSnapshot { get; set; }
        public string? ProductCategorySnapshot { get; set; }

        [Required(ErrorMessage = "Cantitatea este obligatorie")]
        [Range(1, int.MaxValue, ErrorMessage = "Cantitatea trebuie sa fie cel putin 1")]
        public int Quantity { get; set; }

        // Salvam pretul la momentul comenzii (in caz ca pretul produsului se schimba ulterior)
        [Required(ErrorMessage = "Pretul unitar este obligatoriu")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Pretul trebuie sa fie pozitiv")]
        public decimal UnitPrice { get; set; }
    }
}