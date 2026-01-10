using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using OnlineShopProject_dNet.Data;

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
                // Normalizam pentru a evita duplicate cauzate de spatii, majuscule/minuscule sau diacritice
                string Normalize(string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                    s = s.Trim();
                    // Remove diacritics
                    var normalized = s.Normalize(NormalizationForm.FormD);
                    var sb = new System.Text.StringBuilder();
                    foreach (var ch in normalized)
                    {
                        var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                        if (uc != UnicodeCategory.NonSpacingMark)
                            sb.Append(ch);
                    }
                    return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
                }

                var thisName = Normalize(Name);

                // Incarcam in memorie (set mic) si comparam normalizat - suficient pentru numarul mic de categorii
                var duplicateExists = _context.Categories
                    .AsEnumerable()
                    .Any(c => c.Id != Id && Normalize(c.Name) == thisName);

                if (duplicateExists)
                {
                    yield return new ValidationResult("Acest nume de categorie exista deja!", new[] { nameof(Name) });
                }
            }
        }
    }
}