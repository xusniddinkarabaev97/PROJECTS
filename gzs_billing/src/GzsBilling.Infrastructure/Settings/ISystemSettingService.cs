using GzsBilling.Domain.Configuration;

namespace GzsBilling.Infrastructure.Settings;

public interface ISystemSettingService
{
    /// <summary>Gets CardTypeSplits from DB or falls back to appsettings.json defaults.</summary>
    Task<Dictionary<string, CardTypeSplitConfig>> GetCardTypeSplitsAsync(CancellationToken ct = default);
    
    /// <summary>Gets commission rate from DB or falls back to default.</summary>
    Task<decimal> GetSystemCommissionRateAsync(CancellationToken ct = default);
    
    /// <summary>Gets net distribution rate from DB or falls back to default.</summary>
    Task<decimal> GetNetDistributionRateAsync(CancellationToken ct = default);
    
    /// <summary>Gets PaymentId->CardType mapping from DB or falls back to default.</summary>
    Task<Dictionary<int, string>> GetPaymentIdCardTypeMapAsync(CancellationToken ct = default);
    
    /// <summary>Gets default card type string.</summary>
    Task<string> GetDefaultCardTypeAsync(CancellationToken ct = default);
    
    /// <summary>Gets active session cache TTL in minutes.</summary>
    Task<int> GetActiveSeansCacheTtlAsync(CancellationToken ct = default);
    
    /// <summary>Gets a raw setting by key. Returns null if not found.</summary>
    Task<string?> GetSettingValueAsync(string key, CancellationToken ct = default);
    
    /// <summary>Upserts a setting value.</summary>
    Task SetSettingValueAsync(string key, string value, string category, string description, string updatedBy = "system", CancellationToken ct = default);
}
