using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GzsBilling.Infrastructure.Resilience;

/// <summary>
/// Extension methods for registering named resilience pipelines into the DI container.
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Registers three singleton resilience pipelines keyed by downstream target:
    /// PaymentSystem, ErspEcb, and InternalMicroservice.
    /// </summary>
    public static IServiceCollection AddResiliencePipelines(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Resilience.PaymentSystem");
            return ResiliencePolicies.CreatePaymentSystemPipeline(logger);
        });

        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Resilience.ErspEcb");
            return ResiliencePolicies.CreateErspEcbPipeline(logger);
        });

        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Resilience.Internal");
            return ResiliencePolicies.CreateInternalMicroservicePipeline(logger);
        });

        return services;
    }
}
