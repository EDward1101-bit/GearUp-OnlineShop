namespace OnlineShopProject_dNet.Services
{
    public interface IImageValidationService
    {
        /// <summary>
        /// Valideaza daca un fisier este o imagine valida pe baza magic bytes
        /// </summary>
        bool IsValidImage(IFormFile file);

        /// <summary>
        /// Valideaza extensia si dimensiunea
        /// </summary>
        bool IsValidImageSize(IFormFile file, long maxSizeInBytes = 5 * 1024 * 1024);
    }

    public class ImageValidationService : IImageValidationService
    {
        // Magic bytes pentru imagini
        private static readonly Dictionary<string, byte[][]> AllowedMagicBytes = new()
        {
            // JPEG: FF D8 FF
            { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            
            // PNG: 89 50 4E 47
            { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            
            // GIF 87a: 47 49 46 38 37 61
            // GIF 89a: 47 49 46 38 39 61
            { ".gif", new[] 
            { 
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 },
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }
            }}
        };

        public bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            try
            {
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                // Verifica daca extensia e permisa
                if (!AllowedMagicBytes.ContainsKey(fileExtension))
                    return false;

                // Citeste magic bytes
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    var bytes = memoryStream.ToArray();

                    // Verifica magic bytes pentru aceasta extensie
                    var allowedBytes = AllowedMagicBytes[fileExtension];
                    foreach (var magicBytes in allowedBytes)
                    {
                        if (bytes.Length >= magicBytes.Length)
                        {
                            bool match = true;
                            for (int i = 0; i < magicBytes.Length; i++)
                            {
                                if (bytes[i] != magicBytes[i])
                                {
                                    match = false;
                                    break;
                                }
                            }
                            if (match) return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidImageSize(IFormFile file, long maxSizeInBytes = 5 * 1024 * 1024)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > maxSizeInBytes)
                return false;

            // Check file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            return allowedExtensions.Contains(fileExtension);
        }
    }
}
