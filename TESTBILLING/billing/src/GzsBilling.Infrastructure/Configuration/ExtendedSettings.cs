namespace GzsBilling.Infrastructure.Configuration;

public class IdempotencySettings
{
    public string HeaderName { get; set; } = "X-Idempotency-Key";
    public int KeyTtlHours { get; set; } = 24;
    public string CachePrefix { get; set; } = "idem:";
    public bool Return503OnRedisFailure { get; set; } = true;
}

public class RefundSettings
{
    public int SlaHours { get; set; } = 24;
    public int DailyLimit { get; set; } = 5;
    public int WeeklyLimit { get; set; } = 15;
    public decimal DailyAmountLimit { get; set; } = 10_000_000;
    public int HourlyFrequencyLimit { get; set; } = 3;
    public int MaxWorkingDays { get; set; } = 5;
}

public class ReconciliationSettings
{
    public decimal DiscrepancyThresholdPercent { get; set; } = 0.01m;
    public string CronExpression { get; set; } = "0 3 * * *";
    public int BatchSize { get; set; } = 1000;
    public bool AutoAlertOnThresholdExceeded { get; set; } = true;
    public string AlertEmail { get; set; } = "reconciliation@company.uz";
    public string AlertChannel { get; set; } = "#billing-reconciliation-alerts";
}

public class DisputeSettings
{
    public int SlaCalendarDays { get; set; } = 30;
    public int FirstReminderDays { get; set; } = 15;
    public int SecondReminderDays { get; set; } = 25;
    public int FinalReminderDays { get; set; } = 29;
    public int MaxEvidenceFiles { get; set; } = 10;
    public long MaxEvidenceFileSizeMb { get; set; } = 25;
}
