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
        /// Raspunde la o intrebare despre un produs folosind descrierea si FAQ-urile
        /// </summary>
        public async Task<string> GetAnswerForQuestion(int productId, string question)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return "Produsul nu a fost gasit.";
            }

            // Curatam intrebarea
            question = question.ToLower().Trim();

            // Cautam in FAQ-uri specifice produsului
            var productFAQs = _context.FAQs.Where(f => f.ProductId == productId).ToList();

            // Cautam intrebari similare
            foreach (var faq in productFAQs)
            {
                if (IsQuestionSimilar(question, faq.Question.ToLower()))
                {
                    return faq.Answer;
                }
            }

            // Cautam in FAQ-uri generale
            var generalFAQs = _context.FAQs.Where(f => f.ProductId == null).ToList();
            foreach (var faq in generalFAQs)
            {
                if (IsQuestionSimilar(question, faq.Question.ToLower()))
                {
                    return faq.Answer;
                }
            }

            // Analizam intrebarea si descrierea produsului
            return AnalyzeProductDescription(product, question);
        }

        /// <summary>
        /// Verifica daca doua intrebari sunt similare
        /// </summary>
        private bool IsQuestionSimilar(string userQuestion, string faqQuestion)
        {
            // Verificari simple de similaritate
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

            // Daca cel putin 50% din cuvintele cheie se potrivesc
            return commonWords >= Math.Min(userWords.Length, faqWords.Length) * 0.5;
        }

        /// <summary>
        /// Analizeaza descrierea produsului pentru a raspunde la intrebare
        /// </summary>
        private string AnalyzeProductDescription(Product product, string question)
        {
            var description = product.Description.ToLower();

            // Intrebari despre garantie
            if (question.Contains("garantie") || question.Contains("warranty"))
            {
                if (description.Contains("garantie") || description.Contains("warranty"))
                {
                    return "Conform descrierii produsului, acesta beneficiaza de garantie. Va recomand sa verificati detaliile exacte in specificatiile produsului.";
                }
                else
                {
                    return "Momentan nu avem detalii specifice despre garantie pentru acest produs. Va rugam sa ne contactati pentru informatii suplimentare.";
                }
            }

            // Intrebari despre compatibilitate/copii
            if (question.Contains("copii") || question.Contains("copil") || question.Contains("children") || question.Contains("kid"))
            {
                if (description.Contains("copii") || description.Contains("copil") || description.Contains("children") || description.Contains("kid"))
                {
                    return "Da, conform descrierii, acest produs este potrivit pentru copii.";
                }
                else
                {
                    return "Acest produs nu este mentionat ca fiind special conceput pentru copii. Va recomand sa verificati daca este potrivit pentru varsta dorita.";
                }
            }

            // Intrebari despre materiale
            if (question.Contains("material") || question.Contains("fabric") || question.Contains("materiale"))
            {
                if (description.Contains("material") || description.Contains("bumbac") || description.Contains("aluminium") ||
                    description.Contains("plastic") || description.Contains("lemn") || description.Contains("otel"))
                {
                    return "Conform descrierii, produsul este fabricat din materiale de calitate. Va rugam sa verificati specificatiile detaliate pentru informatii complete despre materiale.";
                }
            }

            // Intrebari despre dimensiuni
            if (question.Contains("dimensiune") || question.Contains("dimensiuni") || question.Contains("size") ||
                question.Contains("marime") || question.Contains("lungime") ||
                question.Contains("latime"))
            {
                return "Pentru informatii detaliate despre dimensiuni, va rugam sa verificati specificatiile complete ale produsului sau sa ne contactati.";
            }

            // Intrebari despre utilizare
            if (question.Contains("cum se foloseste") || question.Contains("utilizare") || question.Contains("instructiuni") ||
                question.Contains("how to use") || question.Contains("instructions"))
            {
                return "Instructiunile de utilizare sunt incluse cu produsul. Daca aveti intrebari specifice despre utilizare, va rugam sa ne contactati.";
            }

            // Raspuns generic
            return "Imi pare rau, nu am putut gasi informatii specifice despre aceasta intrebare in descrierea produsului. Va recomand sa ne contactati direct pentru mai multe detalii sau sa verificati specificatiile complete ale produsului.";
        }

        /// <summary>
        /// Adauga o noua intrebare frecventa
        /// </summary>
        public async Task AddFAQ(int? productId, string question, string answer)
        {
            var faq = new FAQ
            {
                ProductId = productId,
                Question = question,
                Answer = answer
            };

            _context.FAQs.Add(faq);
            await _context.SaveChangesAsync();
        }
    }
}
