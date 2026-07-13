using Microsoft.Extensions.DependencyInjection;
using GzsBilling.Application.Services;

namespace GzsBilling.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBillingApplication(this IServiceCollection services)
    {
        // MediatR will be registered via AddMediatR in the API layer
        // Consumers registered via MassTransit

        // Register application services
        services.AddSingleton<ITransactionStore, InMemoryTransactionStore>();
        services.AddSingleton<AntiFraudService>();
        services.AddSingleton<ReconciliationService>();

        return services;
    }
}
