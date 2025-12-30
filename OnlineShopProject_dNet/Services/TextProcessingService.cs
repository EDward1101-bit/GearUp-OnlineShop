using Ganss.Xss;

namespace OnlineShopProject_dNet.Services
{
    public class TextProcessingService
    {
        private readonly HtmlSanitizer _sanitizer;

        public TextProcessingService()
        {
            _sanitizer = new HtmlSanitizer();

            // Configure sanitizer to allow basic formatting and emojis
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("u");

            // Allow common attributes
            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowDataAttributes = false;

            // Keep emojis and other Unicode characters
            _sanitizer.KeepChildNodes = true;
        }

        /// <summary>
        /// Processes text for safe display, preserving line breaks and emojis while sanitizing HTML
        /// </summary>
        public string ProcessForDisplay(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // If input already contains HTML tags (from storage), sanitize it directly
            // Otherwise, convert line breaks to <br> tags first
            string withBreaks;
            if (input.Contains("<br>") || input.Contains("<p>") || input.Contains("<ul>") || input.Contains("<li>"))
            {
                // Already has HTML, just sanitize
                withBreaks = input;
            }
            else
            {
                // Convert line breaks to <br> tags
                withBreaks = input.Replace("\r\n", "<br>").Replace("\n", "<br>").Replace("\r", "<br>");
            }

            // Sanitize the HTML (preserves allowed tags like <br>, <p>, <strong>, <ul>, <li>)
            var sanitized = _sanitizer.Sanitize(withBreaks);

            return sanitized;
        }

        /// <summary>
        /// Processes text for storage (preserves HTML structure for allowed tags)
        /// </summary>
        public string ProcessForStorage(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Preserve user formatting by converting new lines to <br>
            // and sanitizing with the same rules used for display
            var withBreaks = input.Replace("\r\n", "<br>")
                                  .Replace("\n", "<br>")
                                  .Replace("\r", "<br>");

            return _sanitizer.Sanitize(withBreaks);
        }

        /// <summary>
        /// Sanitizes HTML content without converting line breaks
        /// </summary>
        public string SanitizeHtml(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return _sanitizer.Sanitize(input);
        }

        /// <summary>
        /// Sanitizes text input by removing HTML tags but keeping the text content
        /// </summary>
        public string SanitizeText(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove HTML tags but keep text content
            var sanitized = System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
            return sanitized.Trim();
        }
    }
}
