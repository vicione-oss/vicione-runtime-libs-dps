using System;
using System.Collections.Concurrent;

namespace ViciOne.Suite.DataPort;

internal sealed class LoginAttemptTracker(TimeProvider? timeProvider = null)
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationSeconds = 1800;

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, (int FailCount, DateTimeOffset LastAttempt)> _failedAttempts = new();

    internal bool IsLockedOut(string username)
    {
        if (!_failedAttempts.TryGetValue(username, out var record))
            return false;

        if (record.FailCount < MaxFailedAttempts)
            return false;

        if (_timeProvider.GetUtcNow() - record.LastAttempt > TimeSpan.FromSeconds(LockoutDurationSeconds))
        {
            _failedAttempts.TryRemove(username, out _);
            return false;
        }

        return true;
    }

    internal void RecordFailure(string username)
    {
        var now = _timeProvider.GetUtcNow();

        _failedAttempts.AddOrUpdate(
            username,
            _ => (1, now),
            (_, existing) => (existing.FailCount + 1, now));
    }

    internal void ResetFailures(string username)
    {
        _failedAttempts.TryRemove(username, out _);
    }

    internal DateTimeOffset? GetLockoutEnd(string username)
    {
        if (!_failedAttempts.TryGetValue(username, out var record))
            return null;

        if (record.FailCount < MaxFailedAttempts)
            return null;

        return record.LastAttempt + TimeSpan.FromSeconds(LockoutDurationSeconds);
    }

    internal TimeSpan GetRemainingDelay(string username)
    {
        if (!_failedAttempts.TryGetValue(username, out var record))
            return TimeSpan.Zero;

        var delayMs = Math.Min(Math.Pow(2, record.FailCount) * 100, 30_000);
        var required = TimeSpan.FromMilliseconds(delayMs);
        var elapsed = _timeProvider.GetUtcNow() - record.LastAttempt;
        var remaining = required - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
