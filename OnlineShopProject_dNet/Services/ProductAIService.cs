using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Services
{
    public class ProductAIService
    {
        private readonly ApplicationDbContext _context;

        public ProductAIService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Răspunde la o întrebare despre un produs folosind descrierea și FAQ-urile
        /// </summary>
        public async Task<string> GetAnswerForQuestion(int productId, string question)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return "Produsul nu a fost găsit.";
            }

            // Curățăm întrebarea
            question = question.ToLower().Trim();

            // Căutăm în FAQ-uri specifice produsului
            var productFAQs = _context.FAQs.Where(f => f.ProductId == productId).ToList();

            // Căutăm întrebări similare
            foreach (var faq in productFAQs)
            {
                if (IsQuestionSimilar(question, faq.Question.ToLower()))
                {
                    return faq.Answer;
                }
            }

            // Căutăm în FAQ-uri generale
            var generalFAQs = _context.FAQs.Where(f => f.ProductId == null).ToList();
            foreach (var faq in generalFAQs)
            {
                if (IsQuestionSimilar(question, faq.Question.ToLower()))
                {
                    return faq.Answer;
                }
            }

            // Analizăm întrebarea și descrierea produsului
            return AnalyzeProductDescription(product, question);
        }

        /// <summary>
        /// Verifică dacă două întrebări sunt similare
        /// </summary>
        private bool IsQuestionSimilar(string userQuestion, string faqQuestion)
        {
            // Verificări simple de similaritate
            var userWords = userQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var faqWords = faqQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int commonWords = 0;
            foreach (var userWord in userWords)
            {
                if (userWord.Length > 2 && faqWords.Any(faqWord => faqWord.Contains(userWord) || userWord.Contains(faqWord)))
                {
                    commonWords++;
                }
            }

            // Dacă cel puțin 50% din cuvintele cheie se potrivesc
            return commonWords >= Math.Min(userWords.Length, faqWords.Length) * 0.5;
        }

        /// <summary>
        /// Analizează descrierea produsului pentru a răspunde la întrebare
        /// </summary>
        private string AnalyzeProductDescription(Product product, string question)
        {
            var description = product.Description.ToLower();

            // Întrebări despre garanție
            if (question.Contains("garanție") || question.Contains("garantie") || question.Contains("warranty"))
            {
                if (description.Contains("garanție") || description.Contains("garantie") || description.Contains("warranty"))
                {
                    return "Conform descrierii produsului, acesta beneficiază de garanție. Vă recomand să verificați detaliile exacte în specificațiile produsului.";
                }
                else
                {
                    return "Momentan nu avem detalii specifice despre garanție pentru acest produs. Vă rugăm să ne contactați pentru informații suplimentare.";
                }
            }

            // Întrebări despre compatibilitate/copii
            if (question.Contains("copii") || question.Contains("copil") || question.Contains("children") || question.Contains("kid"))
            {
                if (description.Contains("copii") || description.Contains("copil") || description.Contains("children") || description.Contains("kid"))
                {
                    return "Da, conform descrierii, acest produs este potrivit pentru copii.";
                }
                else
                {
                    return "Acest produs nu este menționat ca fiind special conceput pentru copii. Vă recomand să verificați dacă este potrivit pentru vârsta dorită.";
                }
            }

            // Întrebări despre materiale
            if (question.Contains("material") || question.Contains("fabric") || question.Contains("materiale"))
            {
                if (description.Contains("material") || description.Contains("bumbac") || description.Contains("aluminium") ||
                    description.Contains("plastic") || description.Contains("lemn") || description.Contains("oțel"))
                {
                    return "Conform descrierii, produsul este fabricat din materiale de calitate. Vă rugăm să verificați specificațiile detaliate pentru informații complete despre materiale.";
                }
            }

            // Întrebări despre dimensiuni
            if (question.Contains("dimensiune") || question.Contains("dimensiuni") || question.Contains("size") ||
                question.Contains("mărime") || question.Contains("marime") || question.Contains("lungime") ||
                question.Contains("lățime") || question.Contains("latime"))
            {
                return "Pentru informații detaliate despre dimensiuni, vă rugăm să verificați specificațiile complete ale produsului sau să ne contactați.";
            }

            // Întrebări despre utilizare
            if (question.Contains("cum se folosește") || question.Contains("utilizare") || question.Contains("instrucțiuni") ||
                question.Contains("how to use") || question.Contains("instructions"))
            {
                return "Instrucțiunile de utilizare sunt incluse cu produsul. Dacă aveți întrebări specifice despre utilizare, vă rugăm să ne contactați.";
            }

            // Răspuns generic
            return "Îmi pare rău, nu am putut găsi informații specifice despre această întrebare în descrierea produsului. Vă recomand să ne contactați direct pentru mai multe detalii sau să verificați specificațiile complete ale produsului.";
        }

        /// <summary>
        /// Adaugă o nouă întrebare frecventă
        /// </summary>
        public async Task AddFAQ(int? productId, string question, string answer)
        {
            var faq = new FAQ
            {
                ProductId = productId,
                Question = question,
                Answer = answer,
                HelpfulCount = 0
            };

            _context.FAQs.Add(faq);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Marchează o întrebare ca fiind utilă
        /// </summary>
        public async Task MarkFAQHelpful(int faqId)
        {
            var faq = await _context.FAQs.FindAsync(faqId);
            if (faq != null)
            {
                faq.HelpfulCount++;
                await _context.SaveChangesAsync();
            }
        }
    }
}
