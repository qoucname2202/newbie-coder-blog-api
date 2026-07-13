using NewbieCoder.Core.DTOs.Request.User;
using NewbieCoder.Core.DTOs.Response.User;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IUserService
{
    Task<UpdateUserResponse> UpdateUserAsync(
        long userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);
}
