using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Services
{
    public interface IProductAiService
    {
        Task<string> AskProductAssistantAsync(Product product, List<FAQ> faqs, string userQuestion);
    }
}
