using System.Text.Json;
using GzsBilling.Domain.Configuration;
using GzsBilling.Domain.Entities;
using GzsBilling.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GzsBilling.Infrastructure.Settings;

public class SystemSettingService : ISystemSettingService
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly BillingOptions _fallbackOptions;
    private readonly ILogger<SystemSettingService> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SystemSettingService(
        GzsBillingDbContext dbContext,
        IOptions<BillingOptions> fallbackOptions,
        ILogger<SystemSettingService> logger)
    {
        _dbContext = dbContext;
        _fallbackOptions = fallbackOptions.Value;
        _logger = logger;
    }

    private async Task<T?> GetFromDbAsync<T>(string key, CancellationToken ct) where T : class
    {
        try
        {
            var setting = await _dbContext.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key, ct);
            
            if (setting is not null)
            {
                var value = JsonSerializer.Deserialize<T>(setting.Value, JsonOptions);
                if (value is not null)
                {
                    _logger.LogDebug("Loaded setting '{Key}' from database", key);
                    return value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read setting '{Key}' from database. Using fallback.", key);
        }
        
        return null;
    }

    public async Task<Dictionary<string, CardTypeSplitConfig>> GetCardTypeSplitsAsync(CancellationToken ct = default)
    {
        var dbValue = await GetFromDbAsync<Dictionary<string, CardTypeSplitConfig>>("CardTypeSplits", ct);
        return dbValue ?? _fallbackOptions.CardTypeSplits;
    }

    public async Task<decimal> GetSystemCommissionRateAsync(CancellationToken ct = default)
    {
        try
        {
            var setting = await _dbContext.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "SystemCommissionRate", ct);
            if (setting is not null && decimal.TryParse(setting.Value, out var rate))
                return rate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read SystemCommissionRate from DB. Using fallback.");
        }
        return _fallbackOptions.SystemCommissionRate;
    }

    public async Task<decimal> GetNetDistributionRateAsync(CancellationToken ct = default)
    {
        try
        {
            var setting = await _dbContext.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "NetDistributionRate", ct);
            if (setting is not null && decimal.TryParse(setting.Value, out var rate))
                return rate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read NetDistributionRate from DB. Using fallback.");
        }
        return _fallbackOptions.NetDistributionRate;
    }

    public async Task<Dictionary<int, string>> GetPaymentIdCardTypeMapAsync(CancellationToken ct = default)
    {
        var dbValue = await GetFromDbAsync<Dictionary<int, string>>("PaymentIdCardTypeMap", ct);
        return dbValue ?? _fallbackOptions.PaymentIdCardTypeMap;
    }

    public async Task<string> GetDefaultCardTypeAsync(CancellationToken ct = default)
    {
        try
        {
            var setting = await _dbContext.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "DefaultCardType", ct);
            if (setting is not null && !string.IsNullOrWhiteSpace(setting.Value))
                return setting.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read DefaultCardType from DB. Using fallback.");
        }
        return _fallbackOptions.DefaultCardType;
    }

    public async Task<int> GetActiveSeansCacheTtlAsync(CancellationToken ct = default)
    {
        try
        {
            var setting = await _dbContext.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "ActiveSeansCacheTtlMinutes", ct);
            if (setting is not null && int.TryParse(setting.Value, out var ttl))
                return ttl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read ActiveSeansCacheTtlMinutes from DB. Using fallback.");
        }
        return _fallbackOptions.ActiveSeansCacheTtlMinutes;
    }

    public async Task<string?> GetSettingValueAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var setting = await _dbContext.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key, ct);
            return setting?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read setting '{Key}' from DB.", key);
            return null;
        }
    }

    public async Task SetSettingValueAsync(string key, string value, string category, string description, string updatedBy = "system", CancellationToken ct = default)
    {
        var existing = await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        if (existing is not null)
        {
            existing.Value = value;
            existing.Category = category;
            existing.Description = description;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = updatedBy;
            _dbContext.SystemSettings.Update(existing);
        }
        else
        {
            _dbContext.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                Category = category,
                Description = description,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = updatedBy
            });
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Setting '{Key}' updated in database by {User}", key, updatedBy);
    }
}
