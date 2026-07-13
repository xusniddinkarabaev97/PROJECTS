namespace GzsBilling.Domain.Entities;

/// <summary>
/// Stores dynamic system configuration as key-value pairs.
/// Business rules (commission rates, card splits, etc.)
/// are stored here and can be updated without redeployment.
/// </summary>
public class SystemSetting
{
    public Guid Id { get; set; }
    
    /// <summary>Unique setting key (e.g., "CardTypeSplits", "CommissionRate")</summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>JSON value</summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>Category grouping (e.g., "Commission", "CardType", "Disbursement")</summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>Human-readable description</summary>
    public string Description { get; set; } = string.Empty;
    
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public string UpdatedBy { get; set; } = "system";
}
