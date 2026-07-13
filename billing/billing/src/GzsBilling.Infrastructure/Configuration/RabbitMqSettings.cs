namespace GzsBilling.Infrastructure.Configuration;

public class RabbitMqSettings
{
    public string Host { get; set; } = "rabbitmq://localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string MessageTtl { get; set; } = "24:00:00";
    public RetryPolicySettings RetryPolicy { get; set; } = new();
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
    public DeadLetterQueueSettings DeadLetterQueue { get; set; } = new();
    public int PrefetchCount { get; set; } = 16;
    public bool AutoAck { get; set; }
}

public class RetryPolicySettings
{
    public List<string> Intervals { get; set; } = new();
    public int MaxRetryCount { get; set; } = 5;
}

public class CircuitBreakerSettings
{
    public string TrackingPeriod { get; set; } = "00:01:00";
    public int TripThreshold { get; set; } = 15;
    public int ActiveThreshold { get; set; } = 10;
    public string ResetInterval { get; set; } = "00:00:30";
}

public class DeadLetterQueueSettings
{
    public bool Enabled { get; set; } = true;
    public string Suffix { get; set; } = ".dlq";
    public int MessageRetentionDays { get; set; } = 30;
}
