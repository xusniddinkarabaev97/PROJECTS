using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using GzsBilling.Domain.Entities;

namespace GzsBilling.Application.Services;

public class AntiFraudService
{
    private readonly ILogger<AntiFraudService> _logger;

    private readonly ConcurrentDictionary<string, ConcurrentBag<DateTimeOffset>> _userRefundTimestamps = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<decimal>> _userRefundAmounts = new();

    private const int MaxDailyRefunds = 5;
    private const int MaxWeeklyRefunds = 15;
    private const int MaxRefundsPerHour = 3;
    private const decimal MaxDailyRefundAmount = 10_000_000m;

    public AntiFraudService(ILogger<AntiFraudService> logger)
    {
        _logger = logger;
    }

    public (bool Passed, string Reason) CheckDailyRefundLimit(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var now = DateTimeOffset.UtcNow;
        var startOfDay = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);

        var timestamps = _userRefundTimestamps.GetOrAdd(userId, _ => new ConcurrentBag<DateTimeOffset>());
        var refundCountToday = timestamps.Count(t => t >= startOfDay && t <= now);

        if (refundCountToday >= MaxDailyRefunds)
        {
            var reason = string.Format(
                "Daily refund limit exceeded: {0}/{1} refunds for user '{2}'",
                refundCountToday, MaxDailyRefunds, userId);
            _logger.LogWarning("AntiFraud: {Reason}", reason);
            return (false, reason);
        }

        _logger.LogDebug(
            "AntiFraud: Daily refund limit check passed for user '{UserId}'. Count: {Count}/{Max}",
            userId, refundCountToday, MaxDailyRefunds);
        return (true, string.Empty);
    }

    public (bool Passed, string Reason) CheckWeeklyRefundLimit(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var now = DateTimeOffset.UtcNow;
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
        startOfWeek = new DateTimeOffset(startOfWeek.Year, startOfWeek.Month, startOfWeek.Day, 0, 0, 0, now.Offset);

        var timestamps = _userRefundTimestamps.GetOrAdd(userId, _ => new ConcurrentBag<DateTimeOffset>());
        var refundCountThisWeek = timestamps.Count(t => t >= startOfWeek && t <= now);

        if (refundCountThisWeek >= MaxWeeklyRefunds)
        {
            var reason = string.Format(
                "Weekly refund limit exceeded: {0}/{1} refunds for user '{2}'",
                refundCountThisWeek, MaxWeeklyRefunds, userId);
            _logger.LogWarning("AntiFraud: {Reason}", reason);
            return (false, reason);
        }

        _logger.LogDebug(
            "AntiFraud: Weekly refund limit check passed for user '{UserId}'. Count: {Count}/{Max}",
            userId, refundCountThisWeek, MaxWeeklyRefunds);
        return (true, string.Empty);
    }

    public (bool Passed, string Reason) CheckRefundAmountLimit(string userId, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (amount <= 0)
        {
            return (false, "Refund amount must be greater than zero.");
        }

        var now = DateTimeOffset.UtcNow;
        var startOfDay = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);

        var amounts = _userRefundAmounts.GetOrAdd(userId, _ => new ConcurrentBag<decimal>());
        var totalRefundedToday = amounts.Sum();

        var projectedTotal = totalRefundedToday + amount;

        if (projectedTotal > MaxDailyRefundAmount)
        {
            var reason = string.Format(
                "Daily refund amount limit would be exceeded: {0:N0} UZS > {1:N0} UZS for user '{2}'",
                projectedTotal, MaxDailyRefundAmount, userId);
            _logger.LogWarning("AntiFraud: {Reason}", reason);
            return (false, reason);
        }

        _logger.LogDebug(
            "AntiFraud: Refund amount limit check passed for user '{UserId}'. Projected total: {Total:N0}/{Max:N0} UZS",
            userId, projectedTotal, MaxDailyRefundAmount);
        return (true, string.Empty);
    }

    public (bool Passed, string Reason) CheckAmountMatch(decimal originalAmount, decimal refundAmount)
    {
        if (refundAmount <= 0)
        {
            return (false, "Refund amount must be greater than zero.");
        }

        if (refundAmount > originalAmount)
        {
            var reason = string.Format(
                "Refund amount ({0:N2}) exceeds original transaction amount ({1:N2}).",
                refundAmount, originalAmount);
            _logger.LogWarning("AntiFraud: {Reason}", reason);
            return (false, reason);
        }

        _logger.LogDebug(
            "AntiFraud: Amount match check passed. Refund {RefundAmount:N2} <= Original {OriginalAmount:N2}",
            refundAmount, originalAmount);
        return (true, string.Empty);
    }

    public (bool Passed, string Reason) CheckRefundFrequency(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var now = DateTimeOffset.UtcNow;
        var oneHourAgo = now.AddHours(-1);

        var timestamps = _userRefundTimestamps.GetOrAdd(userId, _ => new ConcurrentBag<DateTimeOffset>());
        var refundsInLastHour = timestamps.Count(t => t >= oneHourAgo && t <= now);

        if (refundsInLastHour >= MaxRefundsPerHour)
        {
            var reason = string.Format(
                "Refund frequency limit exceeded: {0}/{1} refunds in the last hour for user '{2}'",
                refundsInLastHour, MaxRefundsPerHour, userId);
            _logger.LogWarning("AntiFraud: {Reason}", reason);
            return (false, reason);
        }

        _logger.LogDebug(
            "AntiFraud: Refund frequency check passed for user '{UserId}'. Count: {Count}/{Max}",
            userId, refundsInLastHour, MaxRefundsPerHour);
        return (true, string.Empty);
    }

    public void RecordRefund(string userId, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var now = DateTimeOffset.UtcNow;

        var timestamps = _userRefundTimestamps.GetOrAdd(userId, _ => new ConcurrentBag<DateTimeOffset>());
        timestamps.Add(now);

        var amounts = _userRefundAmounts.GetOrAdd(userId, _ => new ConcurrentBag<decimal>());
        amounts.Add(amount);

        _logger.LogInformation(
            "AntiFraud: Recorded refund for user '{UserId}'. Amount={Amount:N0} UZS, Timestamp={Timestamp}",
            userId, amount, now);
    }

    public void CleanupExpiredData(TimeSpan retentionPeriod)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(retentionPeriod);

        foreach (var kvp in _userRefundTimestamps)
        {
            var cleaned = new ConcurrentBag<DateTimeOffset>(kvp.Value.Where(t => t >= cutoff));
            _userRefundTimestamps.TryUpdate(kvp.Key, cleaned, kvp.Value);
        }

        _logger.LogDebug("AntiFraud: Cleaned up expired tracking data older than {RetentionPeriod}", retentionPeriod);
    }
}
