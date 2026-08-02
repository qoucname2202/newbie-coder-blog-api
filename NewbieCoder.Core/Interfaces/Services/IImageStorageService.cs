using Microsoft.AspNetCore.Http;
using NewbieCoder.Core.DTOs.Response.Image;

namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Service interface for image storage operations using Cloudinary.
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Uploads a single image to Cloudinary.
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upload response with image metadata.</returns>
    Task<ImageUploadResponse> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads multiple images to Cloudinary.
    /// </summary>
    /// <param name="files">The collection of image files to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of upload responses.</returns>
    Task<IReadOnlyCollection<ImageUploadResponse>> UploadManyAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image from Cloudinary.
    /// </summary>
    /// <param name="publicId">The public ID of the image to delete.</param>
    Task DeleteAsync(string publicId);
}
