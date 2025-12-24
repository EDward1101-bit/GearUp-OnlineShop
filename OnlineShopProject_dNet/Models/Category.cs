using System.ComponentModel.DataAnnotations;
using OnlineShopProject_dNet.Data; // Avem nevoie de asta pentru a accesa baza de date

namespace OnlineShopProject_dNet.Models
{
    // Implementam interfata IValidatableObject
    public class Category : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele categoriei este obligatoriu")]
        public string Name { get; set; } = null!;

        public virtual ICollection<Product>? Products { get; set; }

        // metoda este apelata automat cand se valideaza formularul
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // validationContext ne permite sa cerem servicii configurate in Program.cs
            var _context = (ApplicationDbContext)validationContext.GetService(typeof(ApplicationDbContext));

            if (_context != null)
            {
                // Verificam daca mai exista o categorie cu acelasi nume in baza de date.
                // SQL Server este implicit case-insensitive
                // ne asiguram ca nu ne comparam cu noi insine.
                var duplicateExists = _context.Categories.Any(c => c.Name == Name && c.Id != Id);

                if (duplicateExists)
                {
                    // Returnam eroarea care va fi afisata in View in dreptul campului "Name"
                    yield return new ValidationResult(
                        "Acest nume de categorie există deja!",
                        new[] { nameof(Name) }
                    );
                }
            }
        }
    }
}