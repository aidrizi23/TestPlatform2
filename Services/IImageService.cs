namespace TestPlatform2.Services;

public interface IImageService
{
    /// <summary>
    /// Uploads an image file for a specific question and user
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="userId">The ID of the user uploading the image</param>
    /// <param name="questionId">The ID of the question the image belongs to</param>
    /// <returns>The relative path to the uploaded image</returns>
    Task<string> UploadQuestionImageAsync(IFormFile file, string userId, string questionId);

    /// <summary>
    /// Deletes an image file for a specific question
    /// </summary>
    /// <param name="imagePath">The relative path to the image to delete</param>
    /// <returns>True if the image was deleted successfully</returns>
    Task<bool> DeleteQuestionImageAsync(string imagePath);

    /// <summary>
    /// Validates if the uploaded file is a valid image
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <returns>True if the file is a valid image</returns>
    bool IsValidImage(IFormFile file);

    /// <summary>
    /// Gets the full file path for a given relative image path
    /// </summary>
    /// <param name="relativePath">The relative path to the image</param>
    /// <returns>The full file system path</returns>
    string GetFullImagePath(string relativePath);

    /// <summary>
    /// Resizes an image to the specified dimensions while maintaining aspect ratio
    /// </summary>
    /// <param name="inputPath">Path to the input image</param>
    /// <param name="outputPath">Path where the resized image will be saved</param>
    /// <param name="maxWidth">Maximum width for the resized image</param>
    /// <param name="maxHeight">Maximum height for the resized image</param>
    /// <returns>True if the image was resized successfully</returns>
    Task<bool> ResizeImageAsync(string inputPath, string outputPath, int maxWidth, int maxHeight);
}