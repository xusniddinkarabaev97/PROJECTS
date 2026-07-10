namespace GzsBilling.Domain.Configuration;

public class BillingOptions
{
    public const string SectionName = "Billing";
    public decimal SystemCommissionRate { get; set; } = 0.01m;
    public decimal NetDistributionRate { get; set; } = 0.99m;
    public Dictionary<string, CardTypeSplitConfig> CardTypeSplits { get; set; } = new()
    {
        ["Uzcard"] = new CardTypeSplitConfig { BankSplitRate = 0.20m, PlatformSplitRate = 0.80m },
        ["Humo"]   = new CardTypeSplitConfig { BankSplitRate = 0.80m, PlatformSplitRate = 0.20m }
    };
    public Dictionary<int, string> PaymentIdCardTypeMap { get; set; } = new()
    {
        [1] = "Uzcard",
        [2] = "Humo"
    };
    public string DefaultCardType { get; set; } = "Unknown";
    public int ActiveSeansCacheTtlMinutes { get; set; } = 5;
    public RabbitMqConfig RabbitMq { get; set; } = new();
    public UGazConfig UGaz { get; set; } = new();
    public AbcBankConfig AbcBank { get; set; } = new();
}

public class CardTypeSplitConfig
{
    public decimal BankSplitRate { get; set; }
    public decimal PlatformSplitRate { get; set; }
}

public class RabbitMqConfig
{
    public string ExchangeName { get; set; } = "gzs.billing.exchange";
    public string TransactionCompletedRoutingKey { get; set; } = "tranzaksiya.completed";
    public string SverkaQueueName { get; set; } = "gzs.billing.sverka.queue";
    public string ClientProvidedName { get; set; } = "gzs-billing";
    public ushort ConsumerPrefetchCount { get; set; } = 10;
}

public class UGazConfig
{
    public string BaseUrl { get; set; } = "https://api.ugaz.uz";
    public string AuthToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public class AbcBankConfig
{
    public string BaseUrl { get; set; } = "https://api.abcbank.uz";
    public int TimeoutSeconds { get; set; } = 30;
}
