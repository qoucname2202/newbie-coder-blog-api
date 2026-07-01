namespace NewbieCoder.Core.Interfaces.Services;

public interface IAuthRateLimitService
{
    bool IsBlocked(string loginId, string ipAddress);
    void RecordFailedAttempt(string loginId, string ipAddress);
    void ClearAttempts(string loginId, string ipAddress);
}
