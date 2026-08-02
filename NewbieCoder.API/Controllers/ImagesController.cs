using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Response.Image;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles image upload and deletion operations using Cloudinary.
/// </summary>
[ApiController]
[Route("api/images")]
[Produces("application/json")]
[Tags("Images")]
public sealed class ImagesController : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        IImageStorageService imageStorageService,
        ILogger<ImagesController> logger)
    {
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a single image to Cloudinary.
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The uploaded image details.</returns>
    [HttpPost("upload")]
    [AllowAnonymous]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ImageUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        try
        {
            var result = await _imageStorageService.UploadAsync(file, cancellationToken);

            _logger.LogInformation("Image uploaded successfully. PublicId: {PublicId}", result.PublicId);

            return Ok(ApiResponse<ImageUploadResponse>.Success(
                result,
                trace,
                ResponseMessages.ImageUploadSuccess));
        }
        catch (ImageUploadException ex)
        {
            _logger.LogWarning(ex, "Image upload validation failed: {Message}", ex.Message);
            return StatusCode(
                ex.StatusCode,
                ApiResponse<object>.Fail(
                    trace,
                    ex.ResponseCode,
                    ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image upload");
            return StatusCode(
                HttpStatusCodes.InternalServerError,
                ApiResponse<object>.Fail(
                    trace,
                    ResponseCodes.InternalError,
                    ResponseMessages.InternalError));
        }
    }

    /// <summary>
    /// Uploads multiple images to Cloudinary.
    /// </summary>
    /// <param name="files">The collection of image files to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The uploaded image details.</returns>
    [HttpPost("upload-multiple")]
    [AllowAnonymous]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ImageUploadResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadMultiple(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        try
        {
            var result = await _imageStorageService.UploadManyAsync(files, cancellationToken);

            _logger.LogInformation("Multiple images uploaded successfully. Count: {Count}", result.Count);

            return Ok(ApiResponse<IReadOnlyCollection<ImageUploadResponse>>.Success(
                result,
                trace,
                ResponseMessages.ImageUploadSuccess));
        }
        catch (ImageUploadException ex)
        {
            _logger.LogWarning(ex, "Multiple image upload validation failed: {Message}", ex.Message);
            return StatusCode(
                ex.StatusCode,
                ApiResponse<object>.Fail(
                    trace,
                    ex.ResponseCode,
                    ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during multiple image upload");
            return StatusCode(
                HttpStatusCodes.InternalServerError,
                ApiResponse<object>.Fail(
                    trace,
                    ResponseCodes.InternalError,
                    ResponseMessages.InternalError));
        }
    }

    /// <summary>
    /// Deletes an image from Cloudinary.
    /// </summary>
    /// <param name="request">The delete request containing the public ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message.</returns>
    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        [FromBody] ImageDeleteRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        try
        {
            await _imageStorageService.DeleteAsync(request.PublicId);

            _logger.LogInformation("Image deleted successfully. PublicId: {PublicId}", request.PublicId);

            return Ok(ApiResponse<string>.Success(
                ResponseMessages.ImageDeleteSuccess,
                trace,
                ResponseMessages.ImageDeleteSuccess));
        }
        catch (ImageUploadException ex)
        {
            _logger.LogWarning(ex, "Image delete failed: {Message}", ex.Message);
            return StatusCode(
                ex.StatusCode,
                ApiResponse<object>.Fail(
                    trace,
                    ex.ResponseCode,
                    ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image deletion. PublicId: {PublicId}", request.PublicId);
            return StatusCode(
                HttpStatusCodes.InternalServerError,
                ApiResponse<object>.Fail(
                    trace,
                    ResponseCodes.InternalError,
                    ResponseMessages.InternalError));
        }
    }
}

/// <summary>
/// Request model for deleting an image.
/// </summary>
public sealed class ImageDeleteRequest
{
    /// <summary>
    /// The public ID of the image to delete.
    /// </summary>
    public string PublicId { get; set; } = null!;
}
