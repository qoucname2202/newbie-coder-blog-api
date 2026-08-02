namespace NewbieCoder.Core.DTOs.Response.Image;

/// <summary>
/// Response returned after a successful image upload to Cloudinary.
/// </summary>
public sealed class ImageUploadResponse
{
    /// <summary>
    /// Unique identifier for the uploaded image in Cloudinary.
    /// </summary>
    public string PublicId { get; set; } = null!;

    /// <summary>
    /// HTTPS URL to access the uploaded image.
    /// </summary>
    public string SecureUrl { get; set; } = null!;

    /// <summary>
    /// File format of the uploaded image (e.g., jpg, png, webp).
    /// </summary>
    public string Format { get; set; } = null!;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Bytes { get; set; }

    /// <summary>
    /// Image width in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    public int Height { get; set; }
}
