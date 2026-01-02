using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineShopProject_dNet.Models; // Ensure using directive still resolves FAQ after type update

namespace OnlineShopProject_dNet.Services
{
    public class GoogleProductAiService : IProductAiService
    {
        private const string FallbackAnswer = "Momentan nu avem detalii despre acest aspect.";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleProductAiService> _logger;

        public GoogleProductAiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GoogleProductAiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> AskProductAssistantAsync(Product product, List<FAQ> faqs, string userQuestion)
        {
            if (product == null || string.IsNullOrWhiteSpace(userQuestion))
            {
                return FallbackAnswer;
            }

            var apiKey = _configuration["GoogleAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Google Gemini API key is missing.");
                return FallbackAnswer;
            }

            var prompt = BuildPrompt(product, faqs ?? new List<FAQ>(), userQuestion.Trim());

            try
            {
                var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={Uri.EscapeDataString(apiKey)}";
                var httpClient = _httpClientFactory.CreateClient();

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = prompt } }
                        }
                    }
                };

                using var httpContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                using var response = await httpClient.PostAsync(requestUri, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini request failed with status {StatusCode}", response.StatusCode);
                    return FallbackAnswer;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var answer = ParseResponse(responseContent);

                if (string.IsNullOrWhiteSpace(answer))
                {
                    return FallbackAnswer;
                }

                return answer.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling Google Gemini API");
                return FallbackAnswer;
            }
        }

        private static string BuildPrompt(Product product, List<FAQ> faqs, string userQuestion)
        {
            var builder = new StringBuilder();

            builder.AppendLine("You are an AI Product Assistant for an online shop.");
            builder.AppendLine("You must answer ONLY using the information provided below.");
            builder.AppendLine("You are NOT allowed to invent, assume, or guess.");
            builder.AppendLine();
            builder.AppendLine("If the information is missing, respond exactly with:");
            builder.AppendLine("\"Momentan nu avem detalii despre acest aspect.\"");
            builder.AppendLine();
            builder.AppendLine("RULES:");
            builder.AppendLine("- Answer only in Romanian");
            builder.AppendLine("- Be concise and polite");
            builder.AppendLine("- No external knowledge");
            builder.AppendLine();
            builder.AppendLine("PRODUCT INFORMATION:");
            builder.AppendLine($"Name: {product.Title ?? string.Empty}");
            builder.AppendLine($"Description: {product.Description ?? string.Empty}");
            builder.AppendLine($"Price: {product.Price?.ToString("0.##", CultureInfo.InvariantCulture) ?? "N/A"}");
            builder.AppendLine($"Stock: {product.Stock?.ToString() ?? "N/A"}");
            builder.AppendLine($"Category: {product.Category?.Name ?? "N/A"}");
            builder.AppendLine();
            builder.AppendLine("FAQ:");

            if (faqs.Count > 0)
            {
                foreach (var faq in faqs)
                {
                    builder.AppendLine($"Q: {faq.Question}");
                    builder.AppendLine($"A: {faq.Answer}");
                }
            }
            else
            {
                builder.AppendLine("Q: -");
                builder.AppendLine("A: -");
            }

            builder.AppendLine();
            builder.AppendLine("USER QUESTION:");
            builder.AppendLine($"\"{userQuestion}\"");

            return builder.ToString();
        }

        private static string? ParseResponse(string responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(responseContent);

                if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                    candidates.ValueKind != JsonValueKind.Array ||
                    candidates.GetArrayLength() == 0)
                {
                    return null;
                }

                var firstCandidate = candidates[0];
                if (!firstCandidate.TryGetProperty("content", out var content))
                {
                    return null;
                }

                if (content.TryGetProperty("parts", out var parts) &&
                    parts.ValueKind == JsonValueKind.Array &&
                    parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var textElement))
                    {
                        return textElement.GetString();
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}
