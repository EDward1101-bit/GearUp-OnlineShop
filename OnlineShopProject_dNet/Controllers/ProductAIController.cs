using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShopProject_dNet.Services;

namespace OnlineShopProject_dNet.Controllers
{
    [Authorize]
    public class ProductAIController : Controller
    {
        private readonly ProductAIService _aiService;

        public ProductAIController(ProductAIService aiService)
        {
            _aiService = aiService;
        }

        /// <summary>
        /// Primește o întrebare despre un produs și returnează răspunsul AI
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AskQuestion(int productId, string question)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(question))
                {
                    return Json(new { success = false, message = "Vă rugăm să introduceți o întrebare." });
                }

                if (question.Length > 500)
                {
                    return Json(new { success = false, message = "Întrebarea este prea lungă. Limita este de 500 de caractere." });
                }

                var answer = await _aiService.GetAnswerForQuestion(productId, question);

                return Json(new { success = true, answer });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "A apărut o eroare. Vă rugăm să încercați din nou." });
            }
        }

        /// <summary>
        /// Marchează un răspuns FAQ ca fiind util
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkHelpful(int faqId)
        {
            try
            {
                await _aiService.MarkFAQHelpful(faqId);
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }
    }
}
