using NewbieCoder.Core.DTOs.Response.User;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IFileUploadService
{
    Task<FileUploadResponse> UploadAvatarAsync(
        long userId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    void DeleteAvatar(string avatarUrl);
}
