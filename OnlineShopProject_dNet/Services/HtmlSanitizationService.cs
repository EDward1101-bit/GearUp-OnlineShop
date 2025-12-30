using System.Text.RegularExpressions;

namespace OnlineShopProject_dNet.Services
{
    public interface IHtmlSanitizationService
    {
        /// <summary>
        /// Sanitizeaz? text pentru a preveni XSS attacks
        /// Permite formatting text (newlines, emojis) dar elimin? script tags ?i atribute periculoase
        /// </summary>
        string Sanitize(string input);
    }

    public class HtmlSanitizationService : IHtmlSanitizationService
    {
        // Pattern pentru a detecta script tags ?i alte tag-uri periculoase
        private static readonly Regex ScriptTagRegex = new Regex(
            @"<\s*script[^>]*>.*?</\s*script\s*>|<\s*iframe[^>]*>.*?</\s*iframe\s*>|<\s*embed[^>]*>|<\s*object[^>]*>|<\s*form[^>]*>.*?</\s*form\s*>|<\s*style[^>]*>.*?</\s*style\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
        );

        // Pattern pentru a detecta event handlers (onclick, onerror, etc)
        private static readonly Regex EventHandlerRegex = new Regex(
            @"on\w+\s*=\s*[""']?[^""'>\s]*[""']?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        // Pattern pentru a detecta protocol-uri periculoase (javascript:, data:, vbscript:)
        private static readonly Regex MaliciousProtocolRegex = new Regex(
            @"(javascript|data|vbscript):\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        // Pattern pentru a detecta SVG malitios (SVG injection)
        private static readonly Regex SvgTagRegex = new Regex(
            @"<\s*svg[^>]*>.*?</\s*svg\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
        );

        public string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            try
            {
                string sanitized = input;

                // 1. Elimin? script tags, style tags, iframe/embed/object/form tags
                sanitized = ScriptTagRegex.Replace(sanitized, string.Empty);

                // 2. Elimin? SVG tags (pot con?ine event handlers)
                sanitized = SvgTagRegex.Replace(sanitized, string.Empty);

                // 3. Elimin? event handlers (onclick, onerror, etc)
                sanitized = EventHandlerRegex.Replace(sanitized, string.Empty);

                // 4. Elimin? protocol-uri periculoase (javascript:, data:, vbscript:)
                sanitized = MaliciousProtocolRegex.Replace(sanitized, string.Empty);

                return sanitized;
            }
            catch
            {
                // Fallback: Return input as-is dac? ceva merge prost
                return input ?? string.Empty;
            }
        }
    }
}
