namespace NewbieCoder.Core.Options;

/// <summary>
/// Configuration options for image upload functionality.
/// </summary>
public sealed class ImageUploadOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "ImageUpload";

    /// <summary>
    /// Maximum allowed file size in bytes.
    /// </summary>
    public long MaxFileSizeBytes { get; set; }

    /// <summary>
    /// Maximum number of files allowed in a single upload request.
    /// </summary>
    public int MaxFileCount { get; set; }

    /// <summary>
    /// Allowed file extensions (e.g., .jpg, .png).
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>
    /// Allowed MIME types (e.g., image/jpeg, image/png).
    /// </summary>
    public string[] AllowedMimeTypes { get; set; } = [];

    /// <summary>
    /// Folder path in Cloudinary where images will be stored.
    /// </summary>
    public string CloudinaryFolder { get; set; } = null!;

    /// <summary>
    /// Maximum allowed image width in pixels.
    /// </summary>
    public int? MaxWidth { get; set; }

    /// <summary>
    /// Maximum allowed image height in pixels.
    /// </summary>
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Image quality for uploads (1-100).
    /// </summary>
    public int? ImageQuality { get; set; }

    /// <summary>
    /// Whether to enable automatic optimization (quality_auto and format_auto).
    /// </summary>
    public bool AutoOptimize { get; set; }
}
