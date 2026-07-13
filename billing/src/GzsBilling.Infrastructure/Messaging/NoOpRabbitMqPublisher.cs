using GzsBilling.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GzsBilling.Infrastructure.Messaging;

/// <summary>
/// No-operation RabbitMQ publisher used as fallback when RabbitMQ is unavailable.
/// </summary>
public class NoOpRabbitMqPublisher : IRabbitMqTranzaksiyaPublisher
{
    private readonly ILogger<NoOpRabbitMqPublisher> _logger;

    public NoOpRabbitMqPublisher(ILogger<NoOpRabbitMqPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishTranzaksiyaEventAsync(Tranzaksiya completedTranzaksiya)
    {
        _logger.LogWarning(
            "RabbitMQ unavailable — transaction {Id} event NOT published.",
            completedTranzaksiya.Id);
        return Task.CompletedTask;
    }
}
