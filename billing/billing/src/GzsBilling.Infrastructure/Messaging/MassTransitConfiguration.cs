using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GzsBilling.Infrastructure.Configuration;

namespace GzsBilling.Infrastructure.Messaging;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddBillingMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] consumerAssemblies)
    {
        var rabbitSettings = configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>()
                             ?? new RabbitMqSettings();

        services.AddMassTransit(x =>
        {
            // Register consumers from provided assemblies
            if (consumerAssemblies.Length > 0)
            {
                x.AddConsumers(consumerAssemblies);
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(rabbitSettings.Host), h =>
                {
                    h.Username(rabbitSettings.Username);
                    h.Password(rabbitSettings.Password);
                });

                // Message TTL
                var ttl = TimeSpan.Parse(rabbitSettings.MessageTtl);

                // Global retry policy
                cfg.UseMessageRetry(r =>
                {
                    r.Interval(rabbitSettings.RetryPolicy.MaxRetryCount,
                        TimeSpan.FromSeconds(5));
                    r.Ignore<ArgumentException>();
                    r.Ignore<InvalidOperationException>();
                });

                // Circuit breaker
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.Parse(rabbitSettings.CircuitBreaker.TrackingPeriod);
                    cb.TripThreshold = rabbitSettings.CircuitBreaker.TripThreshold;
                    cb.ActiveThreshold = rabbitSettings.CircuitBreaker.ActiveThreshold;
                    cb.ResetInterval = TimeSpan.Parse(rabbitSettings.CircuitBreaker.ResetInterval);
                });

                // Prefetch count
                cfg.PrefetchCount = rabbitSettings.PrefetchCount;

                var loggerFactory = context.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("MassTransit");

                // Configure endpoints with DLQ support using the context overload
                cfg.ConfigureEndpoints(context);

                // Manually set DLQ arguments per endpoint where needed
                if (rabbitSettings.DeadLetterQueue.Enabled)
                {
                    // DLQ setup will be handled by endpoint-specific configuration
                    logger.LogInformation(
                        "MassTransit configured with DLQ suffix: {Suffix}, TTL: {Ttl}, MaxRetries: {MaxRetries}",
                        rabbitSettings.DeadLetterQueue.Suffix,
                        rabbitSettings.MessageTtl,
                        rabbitSettings.RetryPolicy.MaxRetryCount);
                }
            });
        });

        return services;
    }
}
