using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NewbieCoder.Core.DTOs.Response.User;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

public class FileUploadSettings
{
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB default
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    public string AvatarPath { get; set; } = "uploads/avatars";
}

public sealed class FileUploadService : IFileUploadService
{
    private readonly AppDbContext _db;
    private readonly FileUploadSettings _settings;
    private readonly string _uploadDirectory;

    public FileUploadService(AppDbContext db, IOptions<FileUploadSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
        _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), _settings.AvatarPath);
        
        if (!Directory.Exists(_uploadDirectory))
            Directory.CreateDirectory(_uploadDirectory);
    }

    public async Task<FileUploadResponse> UploadAvatarAsync(
        long userId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        if (!_settings.AllowedExtensions.Contains(extension))
            throw new InvalidOperationException(
                $"File extension {extension} is not allowed. Allowed: {string.Join(", ", _settings.AllowedExtensions)}");

        if (!IsValidImageContentType(contentType))
            throw new InvalidOperationException($"Content type {contentType} is not allowed for image upload");

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        
        if (memoryStream.Length > _settings.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"File size exceeds maximum allowed size of {_settings.MaxFileSizeBytes / 1024 / 1024}MB");

        memoryStream.Position = 0;

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        if (!string.IsNullOrEmpty(user.AvatarUrl))
            DeleteAvatar(user.AvatarUrl);

        var uniqueFileName = $"{userId}_{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

        await using (var fileOutput = new FileStream(filePath, FileMode.Create))
        {
            await memoryStream.CopyToAsync(fileOutput, cancellationToken);
        }

        var url = $"/{_settings.AvatarPath}/{uniqueFileName}";

        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.AvatarUrl, url)
                .SetProperty(u => u.DateLastMaint, DateTimeOffset.UtcNow),
                cancellationToken);

        return new FileUploadResponse
        {
            FileName = uniqueFileName,
            Url = url,
            FileSize = memoryStream.Length,
            ContentType = contentType
        };
    }

    public void DeleteAvatar(string avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl)) return;

        var fileName = Path.GetFileName(avatarUrl);
        var filePath = Path.Combine(_uploadDirectory, fileName);

        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private static bool IsValidImageContentType(string contentType) =>
        contentType == "image/jpeg" ||
        contentType == "image/png" ||
        contentType == "image/webp";
}
