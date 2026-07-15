namespace NewbieCoder.Core.DTOs.Response.User;

public sealed class FileUploadResponse
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}
