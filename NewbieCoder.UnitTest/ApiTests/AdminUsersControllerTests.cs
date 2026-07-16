using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewbieCoder.API.Controllers;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;
using Xunit;

namespace NewbieCoder.UnitTest.ApiTests;

/// <summary>
/// Unit tests for AdminUsersController.
/// Controller delegates to IUserManagementService; BusinessException thrown by the service
/// propagates to the ExceptionHandlingMiddleware in the HTTP pipeline.
/// In unit tests (no middleware), we verify the exception directly.
/// </summary>
public class AdminUsersControllerTests
{
    private static AdminUsersController CreateController(
        Mock<IUserManagementService>? mockUserManagement = null,
        Mock<IUserService>? mockUserService = null,
        Mock<IUserRoleService>? mockUserRoleService = null,
        long? userId = 99,
        IEnumerable<string>? roles = null)
    {
        mockUserManagement ??= new Mock<IUserManagementService>();
        mockUserService ??= new Mock<IUserService>();
        mockUserRoleService ??= new Mock<IUserRoleService>();
        var ctrl = new AdminUsersController(
            mockUserManagement.Object,
            mockUserService.Object,
            mockUserRoleService.Object);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId?.ToString() ?? "99"),
            new Claim(ClaimTypes.NameIdentifier, userId?.ToString() ?? "99")
        };
        foreach (var role in (roles ?? new[] { "ADMIN" }))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "Test");
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
                Items = { ["RequestTrace"] = "test-trace-123" }
            }
        };
        ctrl.ControllerContext.HttpContext.Request.Headers["User-Agent"] = "TestAgent";

        return ctrl;
    }

    #region Lock Endpoint — Success

    [Fact]
    public async Task LockUser_ServiceReturnsResponse_ReturnsOkObjectResult()
    {
        var mock = new Mock<IUserManagementService>();
        mock.Setup(s => s.LockUserAsync(
            It.IsAny<long>(), It.IsAny<LockUserRequest>(),
            It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockUserResponse
            {
                Id = 1,
                Status = "Locked",
                LockedAt = DateTimeOffset.UtcNow,
                LockedReason = "Violation of policy",
                LockedBy = new AdminSummaryDto { Id = 99, Username = "admin" }
            });

        var ctrl = CreateController(mock, userId: 99, roles: new[] { "ADMIN" });
        var request = new LockUserRequest { Reason = "Violation of policy" };

        var result = await ctrl.LockUser(1, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<LockUserResponse>>(okResult.Value!);
        Assert.Equal(ResponseCodes.Success, apiResponse.ResponseStatus.ResponseCode);
        Assert.Equal("Locked", apiResponse.ResponseData?.Status);
        Assert.Equal("Violation of policy", apiResponse.ResponseData?.LockedReason);
        Assert.Equal(99, apiResponse.ResponseData?.LockedBy?.Id);
        Assert.Equal(ResponseMessages.LockSucceeded, apiResponse.ResponseStatus.ResponseMessage);

        // Verify traceId is forwarded to the service
        mock.Verify(s => s.LockUserAsync(
            1L, It.IsAny<LockUserRequest>(), 99L,
            It.IsAny<string?>(), It.IsAny<string?>(),
            "test-trace-123",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Lock Endpoint — Error (BusinessException propagates in unit tests)

    [Fact]
    public async Task LockUser_UserNotFound_ThrowsBusinessException404()
    {
        var mock = new Mock<IUserManagementService>();
        mock.Setup(s => s.LockUserAsync(
            It.IsAny<long>(), It.IsAny<LockUserRequest>(),
            It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException(
                ResponseMessages.UserNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.NotFound));

        var ctrl = CreateController(mock, userId: 99, roles: new[] { "ADMIN" });
        var request = new LockUserRequest { Reason = "Violation" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => ctrl.LockUser(9999, request, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.NotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task LockUser_AlreadyLocked_ThrowsBusinessException409()
    {
        var mock = new Mock<IUserManagementService>();
        mock.Setup(s => s.LockUserAsync(
            It.IsAny<long>(), It.IsAny<LockUserRequest>(),
            It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException(
                ResponseMessages.UserAlreadyLocked,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.UserAlreadyLocked));

        var ctrl = CreateController(mock, userId: 99, roles: new[] { "ADMIN" });
        var request = new LockUserRequest { Reason = "Already locked" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => ctrl.LockUser(1, request, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.UserAlreadyLocked, ex.ResponseCode);
    }

    [Fact]
    public async Task LockUser_CannotLockSelf_ThrowsBusinessException403()
    {
        var mock = new Mock<IUserManagementService>();
        mock.Setup(s => s.LockUserAsync(
            It.IsAny<long>(), It.IsAny<LockUserRequest>(),
            It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException(
                ResponseMessages.CannotLockSelf,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.CannotLockSelf));

        var ctrl = CreateController(mock, userId: 99, roles: new[] { "ADMIN" });
        var request = new LockUserRequest { Reason = "Self lock" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => ctrl.LockUser(99, request, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Forbidden, ex.StatusCode);
        Assert.Equal(ResponseCodes.CannotLockSelf, ex.ResponseCode);
    }

    [Fact]
    public async Task LockUser_CannotLockAdmin_ThrowsBusinessException403()
    {
        var mock = new Mock<IUserManagementService>();
        mock.Setup(s => s.LockUserAsync(
            It.IsAny<long>(), It.IsAny<LockUserRequest>(),
            It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException(
                ResponseMessages.CannotLockSuperAdmin,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.CannotLockSuperAdmin));

        var ctrl = CreateController(mock, userId: 99, roles: new[] { "ADMIN" });
        var request = new LockUserRequest { Reason = "Cannot lock admin" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => ctrl.LockUser(50, request, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Forbidden, ex.StatusCode);
        Assert.Equal(ResponseCodes.CannotLockSuperAdmin, ex.ResponseCode);
    }

    #endregion

    #region Unlock Endpoint — Success

    [Fact]
    public async Task UnlockUser_ServiceReturnsResponse_ReturnsOkObjectResult()
    {
        var mock = new Mock<IUserManagementService>();
        mock.Setup(s => s.UnlockUserAsync(
            It.IsAny<long>(), It.IsAny<long>(),
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockedUserDto
            {
                Id = 1,
                Status = "Active",
                LockedAt = null,
                LockedReason = null,
                LockedBy = null
            });

        var ctrl = CreateController(mock, userId: 99, roles: new[] { "ADMIN" });
        var result = await ctrl.UnlockUser(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<LockedUserDto>>(okResult.Value!);
        Assert.Equal(ResponseCodes.Success, apiResponse.ResponseStatus.ResponseCode);
        Assert.Equal("Active", apiResponse.ResponseData?.Status);
        Assert.Equal(ResponseMessages.UnlockSucceeded, apiResponse.ResponseStatus.ResponseMessage);
    }

    #endregion

    #region Unlock Endpoint — Error

    [Fact]
    public async Task UnlockUser_NotLocked_ThrowsBusinessException409()
    {
        var mock = new Mock<IUserManagementService>();
        mock.Setup(s => s.UnlockUserAsync(
            It.IsAny<long>(), It.IsAny<long>(),
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException(
                ResponseMessages.UserNotLocked,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.UserNotLocked));

        var ctrl = CreateController(mock, userId: 99, roles: new[] { "ADMIN" });

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => ctrl.UnlockUser(1, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.UserNotLocked, ex.ResponseCode);
    }

    #endregion
}
