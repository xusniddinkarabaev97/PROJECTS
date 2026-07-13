using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GzsBilling.Infrastructure.Configuration;
using GzsBilling.Infrastructure.Persistence;

namespace GzsBilling.Infrastructure.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBillingInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<MtlsSettings>(configuration.GetSection("MtlsSettings"));
        services.Configure<RateLimitingSettings>(configuration.GetSection("RateLimitingSettings"));
        services.Configure<WebhookSettings>(configuration.GetSection("WebhookSettings"));
        services.Configure<ReplayProtectionSettings>(configuration.GetSection("ReplayProtectionSettings"));
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMqSettings"));
        services.Configure<AuditLogSettings>(configuration.GetSection("AuditLogSettings"));
        services.Configure<IdempotencySettings>(configuration.GetSection("IdempotencySettings"));
        services.Configure<RefundSettings>(configuration.GetSection("RefundSettings"));
        services.Configure<ReconciliationSettings>(configuration.GetSection("ReconciliationSettings"));
        services.Configure<DisputeSettings>(configuration.GetSection("DisputeSettings"));

        // Register infrastructure services as singletons
        services.AddSingleton<IMtlsHttpClientFactory, MtlsHttpClientFactory>();

        // Add Redis distributed cache
        var redisConnection = configuration.GetSection("Redis").GetValue<string>("ConnectionString")
                              ?? "localhost:6379";
        var instanceName = configuration.GetSection("Redis").GetValue<string>("InstanceName") ?? "billing:";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = instanceName;
        });

        return services;
    }
}
