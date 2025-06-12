using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace TestPlatform2.Services;

public class ImageService : IImageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const int MaxImageWidth = 1920;
    private const int MaxImageHeight = 1080;

    public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> UploadQuestionImageAsync(IFormFile file, string userId, string questionId)
    {
        try
        {
            // Validate the file
            if (!IsValidImage(file))
            {
                throw new ArgumentException("Invalid image file");
            }

            // Create user-specific directory structure
            var userDirectory = Path.Combine(_environment.WebRootPath, "images", "questions", userId);
            Directory.CreateDirectory(userDirectory);

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{questionId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(userDirectory, fileName);

            // Save the original file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Create thumbnail
            var thumbnailFileName = $"{questionId}_{Guid.NewGuid()}_thumb{fileExtension}";
            var thumbnailPath = Path.Combine(userDirectory, thumbnailFileName);
            await ResizeImageAsync(filePath, thumbnailPath, 300, 300);

            // Return relative path
            var relativePath = Path.Combine("images", "questions", userId, fileName).Replace("\\", "/");
            
            _logger.LogInformation("Image uploaded successfully: {RelativePath}", relativePath);
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for user {UserId}, question {QuestionId}", userId, questionId);
            throw;
        }
    }

    public async Task<bool> DeleteQuestionImageAsync(string imagePath)
    {
        try
        {
            var fullPath = GetFullImagePath(imagePath);
            
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                
                // Also try to delete thumbnail if it exists
                var directory = Path.GetDirectoryName(fullPath);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
                var extension = Path.GetExtension(fullPath);
                
                var thumbnailPath = Path.Combine(directory, $"{fileNameWithoutExtension}_thumb{extension}");
                if (File.Exists(thumbnailPath))
                {
                    await Task.Run(() => File.Delete(thumbnailPath));
                }
                
                _logger.LogInformation("Image deleted successfully: {ImagePath}", imagePath);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image: {ImagePath}", imagePath);
            return false;
        }
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return false;
        }

        // Check file size
        if (file.Length > MaxFileSize)
        {
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return false;
        }

        // Check MIME type
        if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return false;
        }

        // Validate actual image content
        try
        {
            using var stream = file.OpenReadStream();
            using var image = Image.Load(stream);
            
            // Additional validation - check image dimensions
            if (image.Width > MaxImageWidth || image.Height > MaxImageHeight)
            {
                return false;
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetFullImagePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return string.Empty;
        }

        return Path.Combine(_environment.WebRootPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    public async Task<bool> ResizeImageAsync(string inputPath, string outputPath, int maxWidth, int maxHeight)
    {
        try
        {
            using var image = await Image.LoadAsync(inputPath);
            
            // Calculate new dimensions while maintaining aspect ratio
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            // Resize the image
            image.Mutate(x => x.Resize(newWidth, newHeight));
            
            // Save the resized image
            await image.SaveAsync(outputPath);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image from {InputPath} to {OutputPath}", inputPath, outputPath);
            return false;
        }
    }
}