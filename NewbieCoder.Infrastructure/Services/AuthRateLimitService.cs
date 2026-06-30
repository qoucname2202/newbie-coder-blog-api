using System.Collections.Concurrent;
using NewbieCoder.Core.Interfaces.Services;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Tracks failed login attempts in memory to protect against brute-force attacks.
/// Separate limits apply per login_id (5/5 min) and per IP address (10/5 min).
/// </summary>
public class AuthRateLimitService : IAuthRateLimitService
{
    private const int LoginIdMaxAttempts = 5;
    private const int IpMaxAttempts = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, AttemptBucket> _loginIdBuckets = new();
    private readonly ConcurrentDictionary<string, AttemptBucket> _ipBuckets = new();
    private readonly Timer _cleanupTimer;

    public AuthRateLimitService()
    {
        _cleanupTimer = new Timer(
            _ => CleanupExpiredEntries(),
            null,
            CleanupInterval,
            CleanupInterval);
    }

    /// <summary>
    /// Returns true if the given login_id or IP has exceeded the allowed number of failed attempts.
    /// </summary>
    public bool IsBlocked(string loginId, string ipAddress)
    {
        return GetLoginIdRemainingAttempts(loginId) <= 0
               || GetIpRemainingAttempts(ipAddress) <= 0;
    }

    /// <summary>
    /// Records a failed login attempt for the given login_id and IP address.
    /// </summary>
    public void RecordFailedAttempt(string loginId, string ipAddress)
    {
        GetOrCreate(_loginIdBuckets, loginId).Record();
        GetOrCreate(_ipBuckets, ipAddress).Record();
    }

    /// <summary>
    /// Clears all failed attempts for the given login_id and IP address (call on successful login).
    /// </summary>
    public void ClearAttempts(string loginId, string ipAddress)
    {
        _loginIdBuckets.TryRemove(loginId, out _);
        _ipBuckets.TryRemove(ipAddress, out _);
    }

    public int GetLoginIdRemainingAttempts(string loginId)
    {
        return GetOrCreate(_loginIdBuckets, loginId).Remaining(LoginIdMaxAttempts, Window);
    }

    public int GetIpRemainingAttempts(string ipAddress)
    {
        return GetOrCreate(_ipBuckets, ipAddress).Remaining(IpMaxAttempts, Window);
    }

    private static AttemptBucket GetOrCreate(
        ConcurrentDictionary<string, AttemptBucket> buckets,
        string key) =>
        buckets.GetOrAdd(key, _ => new AttemptBucket());

    private void CleanupExpiredEntries()
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(Window.Add(Window));
        foreach (var bucket in _loginIdBuckets.Values)
            bucket.RemoveOld(cutoff);
        foreach (var bucket in _ipBuckets.Values)
            bucket.RemoveOld(cutoff);

        CleanupEmpty(_loginIdBuckets);
        CleanupEmpty(_ipBuckets);
    }

    private static void CleanupEmpty(ConcurrentDictionary<string, AttemptBucket> buckets)
    {
        foreach (var key in buckets.Keys.ToList())
        {
            if (buckets.TryGetValue(key, out var bucket) && bucket.IsEmpty)
                buckets.TryRemove(key, out _);
        }
    }

    private sealed class AttemptBucket
    {
        private readonly List<(DateTimeOffset Time, int Count)> _attempts = new();
        private readonly object _lock = new();

        public void Record()
        {
            lock (_lock)
            {
                _attempts.Add((DateTimeOffset.UtcNow, 1));
            }
        }

        public int Remaining(int maxAttempts, TimeSpan window)
        {
            lock (_lock)
            {
                var cutoff = DateTimeOffset.UtcNow.Subtract(window);
                _attempts.RemoveAll(a => a.Time < cutoff);
                return Math.Max(0, maxAttempts - _attempts.Count);
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _attempts.Count == 0;
                }
            }
        }

        public void RemoveOld(DateTimeOffset cutoff)
        {
            lock (_lock)
            {
                _attempts.RemoveAll(a => a.Time < cutoff);
            }
        }
    }
}
