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
        /// Primeste o intrebare despre un produs si returneaza raspunsul AI
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AskQuestion(int productId, string question)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(question))
                {
                    return Json(new { success = false, message = "Va rugam sa introduceti o intrebare." });
                }

                if (question.Length > 500)
                {
                    return Json(new { success = false, message = "Intrebarea este prea lunga. Limita este de 500 de caractere." });
                }

                var answer = await _aiService.GetAnswerForQuestion(productId, question);

                return Json(new { success = true, answer });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "A aparut o eroare. Va rugam sa incercati din nou." });
            }
        }
    }
}
