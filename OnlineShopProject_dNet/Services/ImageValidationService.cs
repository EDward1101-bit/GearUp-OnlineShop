using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

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

        /// <summary>
        /// Redimensioneaza imaginea la dimensiuni standard (800x800), pastreaza aspect ratio si adauga padding daca e nevoie.
        /// Returneaza calea fisierului salvat relativa la wwwroot.
        /// </summary>
        Task<string> ResizeAndSaveImageAsync(IFormFile file, string webRootPath, int targetWidth = 800, int targetHeight = 800);
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

        public async Task<string> ResizeAndSaveImageAsync(IFormFile file, string webRootPath, int targetWidth = 800, int targetHeight = 800)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file", nameof(file));

            // Generate unique filename to avoid collisions
            var originalExtension = Path.GetExtension(file.FileName).ToLower();
            var uniqueFileName = $"{Guid.NewGuid()}{originalExtension}";
            var imagesFolder = Path.Combine(webRootPath, "images");
            
            // Ensure images directory exists
            if (!Directory.Exists(imagesFolder))
            {
                Directory.CreateDirectory(imagesFolder);
            }

            var outputPath = Path.Combine(imagesFolder, uniqueFileName);
            var databasePath = "/images/" + uniqueFileName;

            using (var inputStream = file.OpenReadStream())
            {
                using var image = await Image.LoadAsync(inputStream);
                
                // Calculate new dimensions preserving aspect ratio
                var ratioX = (double)targetWidth / image.Width;
                var ratioY = (double)targetHeight / image.Height;
                var ratio = Math.Min(ratioX, ratioY);
                
                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);
                
                // Resize the image with padding to fit target dimensions
                image.Mutate(x => x
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(newWidth, newHeight),
                        Mode = ResizeMode.Max
                    })
                    .Pad(targetWidth, targetHeight, Color.White));

                // Save based on original extension
                if (originalExtension == ".png")
                {
                    await image.SaveAsync(outputPath, new PngEncoder());
                }
                else
                {
                    // Default to JPEG for jpg, jpeg, gif (gif animation is lost)
                    await image.SaveAsync(outputPath, new JpegEncoder { Quality = 90 });
                }
            }

            return databasePath;
        }
    }
}
