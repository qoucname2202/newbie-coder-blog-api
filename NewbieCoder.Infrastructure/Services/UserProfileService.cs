using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Response.User;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Retrieves the authenticated user's profile, statistics, and current session info.
/// </summary>
public sealed class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _db;

    public UserProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserMeResponse> GetMyProfileAsync(
        long userId,
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == sessionId && s.UserId == userId,
                cancellationToken);

        if (session == null)
        {
            throw new BusinessException(
                ResponseMessages.SessionNotFound,
                statusCode: HttpStatusCodes.Unauthorized,
                responseCode: ResponseCodes.Unauthorized);
        }

        switch (session.Status)
        {
            case SessionStatus.Active:
                break;
            case SessionStatus.Revoked:
                throw new BusinessException(
                    ResponseMessages.SessionRevoked,
                    statusCode: HttpStatusCodes.Unauthorized,
                    responseCode: ResponseCodes.SessionRevoked);
            case SessionStatus.Expired:
                throw new BusinessException(
                    ResponseMessages.SessionNotFound,
                    statusCode: HttpStatusCodes.Unauthorized,
                    responseCode: ResponseCodes.Unauthorized);
            default:
                throw new BusinessException(
                    ResponseMessages.SessionNotFound,
                    statusCode: HttpStatusCodes.Unauthorized,
                    responseCode: ResponseCodes.Unauthorized);
        }

        if (session.ExpiredAt <= DateTimeOffset.UtcNow)
        {
            throw new BusinessException(
                ResponseMessages.SessionNotFound,
                statusCode: HttpStatusCodes.Unauthorized,
                responseCode: ResponseCodes.Unauthorized);
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new BusinessException(
                ResponseMessages.UserNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.NotFound);
        }

        switch (user.Status)
        {
            case UserStatus.Active:
                break;
            case UserStatus.Banned:
                await RevokeAllUserSessionsAsync(userId, cancellationToken);
                throw new BusinessException(
                    ResponseMessages.UserBlocked,
                    statusCode: HttpStatusCodes.Forbidden,
                    responseCode: ResponseCodes.Forbidden);
            case UserStatus.Closed:
                throw new BusinessException(
                    ResponseMessages.UserNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.NotFound);
            default:
                throw new BusinessException(
                    ResponseMessages.UserBlocked,
                    statusCode: HttpStatusCodes.Forbidden,
                    responseCode: ResponseCodes.Forbidden);
        }

        var statistics = await GetUserStatisticsAsync(userId, cancellationToken);

        var currentSession = await GetCurrentSessionAsync(sessionId, userId, cancellationToken);

        return new UserMeResponse
        {
            User = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                Status = user.Status.ToString().ToLowerInvariant(),
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.EffDate,
                UpdatedAt = user.DateLastMaint
            },
            Statistics = statistics,
            CurrentSession = currentSession
        };
    }

    #region Statistics

    private async Task<UserStatisticsDto> GetUserStatisticsAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var totalBookmarked = await _db.InterviewQuestions
            .Where(q => q.BookmarkCount > 0 && q.AuthorId == userId)
            .Select(q => q.BookmarkCount)
            .SumAsync(cancellationToken);

        var totalCreated = await _db.InterviewQuestions
            .CountAsync(q => q.AuthorId == userId, cancellationToken);

        var totalPublished = await _db.InterviewQuestions
            .CountAsync(q => q.AuthorId == userId && q.Status == PostStatus.Published, cancellationToken);

        var totalComments = await _db.CommunityAnswers
            .CountAsync(a => a.AuthorId == userId, cancellationToken);

        return new UserStatisticsDto
        {
            TotalBookmarkedQuestions = (int)totalBookmarked,
            TotalMasteredQuestions = 0,
            TotalLearningQuestions = 0,
            TotalCreatedQuestions = totalCreated,
            TotalPublishedQuestions = totalPublished,
            TotalComments = totalComments
        };
    }

    #endregion

    #region Current session

    private async Task<CurrentSessionDto> GetCurrentSessionAsync(
        long sessionId,
        long userId,
        CancellationToken cancellationToken)
    {
        var result = await _db.UserSessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId && s.UserId == userId)
            .Join(
                _db.UserDevices,
                s => s.DeviceId,
                d => d.Id,
                (s, d) => new CurrentSessionDto
                {
                    SessionId = s.Id,
                    DeviceId = d.Id,
                    DeviceName = d.DeviceName,
                    DeviceType = d.DeviceType.ToString().ToLowerInvariant(),
                    IpAddress = s.IpAddress,
                    LoginAt = s.LoginAt,
                    LastActiveAt = s.LastActiveAt,
                    ExpiredAt = s.ExpiredAt
                })
            .FirstOrDefaultAsync(cancellationToken);

        return result ?? new CurrentSessionDto
        {
            SessionId = sessionId,
            DeviceId = 0,
            DeviceType = "unknown",
            LoginAt = DateTimeOffset.UtcNow,
            ExpiredAt = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Helpers

    private async Task RevokeAllUserSessionsAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.UserSessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ExecuteUpdateAsync(s => s
                .SetProperty(s => s.Status, SessionStatus.Revoked)
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevokedReason, "user_blocked"),
                cancellationToken);
    }

    #endregion
}
