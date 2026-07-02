using NewbieCoder.Core.DTOs.Response.User;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IUserProfileService
{
    Task<UserMeResponse> GetMyProfileAsync(
        long userId,
        long sessionId,
        CancellationToken cancellationToken = default);
}
