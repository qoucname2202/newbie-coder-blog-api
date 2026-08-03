namespace NewbieCoder.Core.Options;

/// <summary>
/// Configuration options for Cloudinary cloud storage.
/// </summary>
public sealed class CloudinaryOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Cloudinary";

    /// <summary>
    /// Cloud name from Cloudinary dashboard.
    /// </summary>
    public string CloudName { get; set; } = null!;

    /// <summary>
    /// API key from Cloudinary dashboard.
    /// </summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>
    /// API secret from Cloudinary dashboard.
    /// </summary>
    public string ApiSecret { get; set; } = null!;
}
