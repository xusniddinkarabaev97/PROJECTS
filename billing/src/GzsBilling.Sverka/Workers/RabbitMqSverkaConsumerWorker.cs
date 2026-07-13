using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using GzsBilling.Domain.Configuration;
using GzsBilling.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GzsBilling.Sverka.Workers;

public class RabbitMqSverkaConsumerWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqSverkaConsumerWorker> _logger;
    private readonly BillingOptions _billingOptions;
    private IChannel? _channel;

    public ConcurrentDictionary<DateOnly, List<Tranzaksiya>> DailyBuffer { get; } = new();

    public RabbitMqSverkaConsumerWorker(
        IConnection connection,
        ILogger<RabbitMqSverkaConsumerWorker> logger,
        IOptions<BillingOptions> billingOptions)
    {
        _connection = connection;
        _logger = logger;
        _billingOptions = billingOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _billingOptions.RabbitMq.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _billingOptions.RabbitMq.SverkaQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: _billingOptions.RabbitMq.SverkaQueueName,
            exchange: _billingOptions.RabbitMq.ExchangeName,
            routingKey: _billingOptions.RabbitMq.TransactionCompletedRoutingKey,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, _billingOptions.RabbitMq.ConsumerPrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var tranzaksiya = JsonSerializer.Deserialize<Tranzaksiya>(json);

                if (tranzaksiya is not null)
                {
                    var date = DateOnly.FromDateTime(tranzaksiya.CreatedAt.Date);
                    DailyBuffer.AddOrUpdate(date,
                        _ => new List<Tranzaksiya> { tranzaksiya },
                        (_, list) =>
                        {
                            lock (list)
                            {
                                list.Add(tranzaksiya);
                            }
                            return list;
                        });

                    _logger.LogDebug("Buffered transaction {Id} for date {Date}", tranzaksiya.Id, date);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message. Will not ack.");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _billingOptions.RabbitMq.SverkaQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Sverka consumer started, listening on queue {Queue}", _billingOptions.RabbitMq.SverkaQueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sverka consumer stopping...");
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }
        await base.StopAsync(cancellationToken);
    }
}
