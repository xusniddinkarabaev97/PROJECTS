using GzsBilling.Domain.Entities;

namespace GzsBilling.Infrastructure.Messaging;

public interface IRabbitMqTranzaksiyaPublisher
{
    Task PublishTranzaksiyaEventAsync(Tranzaksiya completedTranzaksiya);
}
