using System.Text;
using System.Text.Json;
using GzsBilling.Domain.Configuration;
using GzsBilling.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GzsBilling.Infrastructure.Messaging;

public class RabbitMqTranzaksiyaPublisher : IRabbitMqTranzaksiyaPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqTranzaksiyaPublisher> _logger;
    private readonly BillingOptions _billingOptions;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqTranzaksiyaPublisher(
        IConnection connection,
        ILogger<RabbitMqTranzaksiyaPublisher> logger,
        IOptions<BillingOptions> billingOptions)
    {
        _connection = connection;
        _logger = logger;
        _billingOptions = billingOptions.Value;

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: false);

        _channel = connection.CreateChannelAsync(channelOptions).GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: _billingOptions.RabbitMq.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false).GetAwaiter().GetResult();
    }

    public async Task PublishTranzaksiyaEventAsync(Tranzaksiya completedTranzaksiya)
    {
        var message = JsonSerializer.Serialize(completedTranzaksiya, JsonOptions);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = completedTranzaksiya.Id.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: _billingOptions.RabbitMq.ExchangeName,
            routingKey: _billingOptions.RabbitMq.TransactionCompletedRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published transaction {TransactionId} to exchange {Exchange} with routing key {RoutingKey}",
            completedTranzaksiya.Id, _billingOptions.RabbitMq.ExchangeName, _billingOptions.RabbitMq.TransactionCompletedRoutingKey);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
