using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IUserService
{
    Task<PaginatedResponse<UserListItemResponse>> GetUsersAsync(
        UserFilterRequest filter,
        CancellationToken cancellationToken = default);
}
