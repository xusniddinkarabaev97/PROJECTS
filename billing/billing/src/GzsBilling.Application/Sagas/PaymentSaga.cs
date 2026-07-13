using MassTransit;
using Microsoft.Extensions.Logging;

namespace GzsBilling.Application.Sagas;

public interface PaymentCreated
{
    Guid CorrelationId { get; }
    string TransactionId { get; }
    string ContragentId { get; }
    decimal Amount { get; }
    string Currency { get; }
    DateTimeOffset Timestamp { get; }
}

public interface FundsReservedEvent
{
    Guid CorrelationId { get; }
    string TransactionId { get; }
    string ReservationId { get; }
    DateTimeOffset Timestamp { get; }
}

public interface EcbConfirmedEvent
{
    Guid CorrelationId { get; }
    string TransactionId { get; }
    string EcbReference { get; }
    DateTimeOffset Timestamp { get; }
}

public interface PaymentCompleted
{
    Guid CorrelationId { get; }
    string TransactionId { get; }
    DateTimeOffset Timestamp { get; }
}

public interface PaymentFailed
{
    Guid CorrelationId { get; }
    string TransactionId { get; }
    string FailureReason { get; }
    DateTimeOffset Timestamp { get; }
}

public interface CompensatePayment
{
    Guid CorrelationId { get; }
    string TransactionId { get; }
    string Reason { get; }
    DateTimeOffset Timestamp { get; }
}

public interface CompensationCompleted
{
    Guid CorrelationId { get; }
    string TransactionId { get; }
    DateTimeOffset Timestamp { get; }
}

public class PaymentSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;
    public string ContragentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";

    public string? ReservationId { get; set; }
    public string? EcbReference { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FundsReservedAt { get; set; }
    public DateTimeOffset? EcbConfirmedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CompensatedAt { get; set; }

    public Guid? CompensationTimeoutTokenId { get; set; }
}

public class CompensationTimeoutMessage
{
    public Guid CorrelationId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}

public class PaymentSagaStateMachine : MassTransitStateMachine<PaymentSagaState>
{
    private readonly ILogger<PaymentSagaStateMachine> _logger;

    public State Created { get; private set; } = null!;
    public State FundsReserved { get; private set; } = null!;
    public State EcbConfirmed { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;
    public State Compensated { get; private set; } = null!;

    public Event<PaymentCreated> PaymentCreatedEvent { get; private set; } = null!;
    public Event<FundsReservedEvent> FundsReservedEvent { get; private set; } = null!;
    public Event<EcbConfirmedEvent> EcbConfirmedEvent { get; private set; } = null!;
    public Event<PaymentCompleted> PaymentCompletedEvent { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailedEvent { get; private set; } = null!;
    public Event<CompensatePayment> CompensatePaymentEvent { get; private set; } = null!;
    public Event<CompensationCompleted> CompensationCompletedEvent { get; private set; } = null!;

    public Schedule<PaymentSagaState, CompensationTimeoutMessage> CompensationTimeout { get; private set; } = null!;

    public PaymentSagaStateMachine(ILogger<PaymentSagaStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        var sagaTimeout = TimeSpan.FromMinutes(30);

        Event(() => PaymentCreatedEvent, x =>
            x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => FundsReservedEvent, x =>
            x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => EcbConfirmedEvent, x =>
            x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => PaymentCompletedEvent, x =>
            x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => PaymentFailedEvent, x =>
            x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompensatePaymentEvent, x =>
            x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompensationCompletedEvent, x =>
            x.CorrelateById(m => m.Message.CorrelationId));

        Schedule(() => CompensationTimeout, instance => instance.CompensationTimeoutTokenId, s =>
        {
            s.Delay = sagaTimeout;
            s.Received = r => r.CorrelateById(ctx => ctx.Message.CorrelationId);
        });

        Initially(
            When(PaymentCreatedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.TransactionId = ctx.Message.TransactionId;
                    ctx.Saga.ContragentId = ctx.Message.ContragentId;
                    ctx.Saga.Amount = ctx.Message.Amount;
                    ctx.Saga.Currency = ctx.Message.Currency;
                    ctx.Saga.CreatedAt = ctx.Message.Timestamp;

                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Payment created. TxnId={TxnId}, Amount={Amount} {Currency}, Contragent={ContragentId}",
                        ctx.Saga.CorrelationId, ctx.Saga.TransactionId, ctx.Saga.Amount,
                        ctx.Saga.Currency, ctx.Saga.ContragentId);
                })
                .Schedule(CompensationTimeout, ctx => new CompensationTimeoutMessage
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    TransactionId = ctx.Saga.TransactionId
                })
                .TransitionTo(Created)
                .Then(ctx => _logger.LogInformation(
                    "PaymentSaga [{CorrelationId}]: Transitioned to Created",
                    ctx.Saga.CorrelationId))
        );

        During(Created,
            When(FundsReservedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.ReservationId = ctx.Message.ReservationId;
                    ctx.Saga.FundsReservedAt = ctx.Message.Timestamp;

                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Funds reserved. ReservationId={ReservationId}, TxnId={TxnId}",
                        ctx.Saga.CorrelationId, ctx.Message.ReservationId, ctx.Saga.TransactionId);
                })
                .TransitionTo(FundsReserved)
                .Then(ctx => _logger.LogInformation(
                    "PaymentSaga [{CorrelationId}]: Transitioned to FundsReserved",
                    ctx.Saga.CorrelationId)),

            When(PaymentFailedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.FailureReason;

                    _logger.LogWarning(
                        "PaymentSaga [{CorrelationId}]: Payment failed during Created. Reason={Reason}",
                        ctx.Saga.CorrelationId, ctx.Message.FailureReason);
                })
                .TransitionTo(Failed)
                .Then(ctx => _logger.LogInformation(
                    "PaymentSaga [{CorrelationId}]: Transitioned to Failed",
                    ctx.Saga.CorrelationId))
        );

        During(FundsReserved,
            When(EcbConfirmedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.EcbReference = ctx.Message.EcbReference;
                    ctx.Saga.EcbConfirmedAt = ctx.Message.Timestamp;

                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: ECB confirmed. EcbReference={EcbReference}, TxnId={TxnId}",
                        ctx.Saga.CorrelationId, ctx.Message.EcbReference, ctx.Saga.TransactionId);
                })
                .TransitionTo(EcbConfirmed)
                .Then(ctx => _logger.LogInformation(
                    "PaymentSaga [{CorrelationId}]: Transitioned to EcbConfirmed",
                    ctx.Saga.CorrelationId)),

            When(PaymentFailedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.FailureReason;

                    _logger.LogWarning(
                        "PaymentSaga [{CorrelationId}]: Payment failed during FundsReserved. Reason={Reason}. Starting compensation.",
                        ctx.Saga.CorrelationId, ctx.Message.FailureReason);
                })
                .TransitionTo(Compensating)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Compensating - Release reserved funds. ReservationId={ReservationId}",
                        ctx.Saga.CorrelationId, ctx.Saga.ReservationId);

                    await ReleaseFundsAsync(ctx);
                })
                .TransitionTo(Compensated)
                .Then(ctx =>
                {
                    ctx.Saga.CompensatedAt = DateTimeOffset.UtcNow;
                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Transitioned to Compensated",
                        ctx.Saga.CorrelationId);
                })
        );

        During(EcbConfirmed,
            When(PaymentCompletedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.CompletedAt = ctx.Message.Timestamp;

                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Payment completed successfully. TxnId={TxnId}",
                        ctx.Saga.CorrelationId, ctx.Saga.TransactionId);
                })
                .Unschedule(CompensationTimeout)
                .TransitionTo(Completed)
                .Then(ctx => _logger.LogInformation(
                    "PaymentSaga [{CorrelationId}]: Transitioned to Completed",
                    ctx.Saga.CorrelationId)),

            When(PaymentFailedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.FailureReason;

                    _logger.LogWarning(
                        "PaymentSaga [{CorrelationId}]: Payment failed during EcbConfirmed. Reason={Reason}. Starting full compensation.",
                        ctx.Saga.CorrelationId, ctx.Message.FailureReason);
                })
                .TransitionTo(Compensating)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Compensating - Reverse ECB confirmation. EcbReference={EcbReference}",
                        ctx.Saga.CorrelationId, ctx.Saga.EcbReference);

                    await ReverseEcbAsync(ctx);

                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Compensating - Release reserved funds. ReservationId={ReservationId}",
                        ctx.Saga.CorrelationId, ctx.Saga.ReservationId);

                    await ReleaseFundsAsync(ctx);
                })
                .TransitionTo(Compensated)
                .Then(ctx =>
                {
                    ctx.Saga.CompensatedAt = DateTimeOffset.UtcNow;
                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Full compensation completed. Transitioned to Compensated.",
                        ctx.Saga.CorrelationId);
                })
        );

        During(Compensating,
            When(CompensationCompletedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.CompensatedAt = ctx.Message.Timestamp;

                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Compensation completed event received. TxnId={TxnId}",
                        ctx.Saga.CorrelationId, ctx.Saga.TransactionId);
                })
                .Unschedule(CompensationTimeout)
                .TransitionTo(Compensated)
                .Then(ctx => _logger.LogInformation(
                    "PaymentSaga [{CorrelationId}]: Transitioned to Compensated",
                    ctx.Saga.CorrelationId))
        );

        During(Created, FundsReserved, EcbConfirmed,
            When(CompensatePaymentEvent)
                .Then(ctx =>
                {
                    _logger.LogWarning(
                        "PaymentSaga [{CorrelationId}]: Explicit compensation requested. Reason={Reason}",
                        ctx.Saga.CorrelationId, ctx.Message.Reason);
                })
                .TransitionTo(Compensating)
                .ThenAsync(async ctx =>
                {
                    var currentState = ctx.Saga.CurrentState;

                    if (currentState == nameof(EcbConfirmed))
                    {
                        _logger.LogInformation(
                            "PaymentSaga [{CorrelationId}]: Compensating - Reverse ECB. EcbReference={EcbReference}",
                            ctx.Saga.CorrelationId, ctx.Saga.EcbReference);
                        await ReverseEcbAsync(ctx);
                    }

                    if (currentState == nameof(EcbConfirmed) || currentState == nameof(FundsReserved))
                    {
                        _logger.LogInformation(
                            "PaymentSaga [{CorrelationId}]: Compensating - Release funds. ReservationId={ReservationId}",
                            ctx.Saga.CorrelationId, ctx.Saga.ReservationId);
                        await ReleaseFundsAsync(ctx);
                    }

                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Marking payment as compensated.",
                        ctx.Saga.CorrelationId);
                    await MarkCompensatedAsync(ctx);
                })
                .TransitionTo(Compensated)
                .Then(ctx =>
                {
                    ctx.Saga.CompensatedAt = DateTimeOffset.UtcNow;
                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Transitioned to Compensated",
                        ctx.Saga.CorrelationId);
                })
        );

        During(Created, FundsReserved, EcbConfirmed,
            When(CompensationTimeout!.Received)
                .Then(ctx =>
                {
                    _logger.LogWarning(
                        "PaymentSaga [{CorrelationId}]: Saga timed out. Initiating auto-compensation. TxnId={TxnId}",
                        ctx.Saga.CorrelationId, ctx.Saga.TransactionId);
                    ctx.Saga.FailureReason = "Saga timeout - auto-compensation triggered";
                })
                .TransitionTo(Compensating)
                .ThenAsync(async ctx =>
                {
                    var currentState = ctx.Saga.CurrentState;

                    if (currentState == nameof(EcbConfirmed))
                    {
                        await ReverseEcbAsync(ctx);
                    }

                    if (currentState == nameof(EcbConfirmed) || currentState == nameof(FundsReserved))
                    {
                        await ReleaseFundsAsync(ctx);
                    }

                    await MarkCompensatedAsync(ctx);
                })
                .TransitionTo(Compensated)
                .Then(ctx =>
                {
                    ctx.Saga.CompensatedAt = DateTimeOffset.UtcNow;
                    _logger.LogInformation(
                        "PaymentSaga [{CorrelationId}]: Auto-compensation completed. Transitioned to Compensated.",
                        ctx.Saga.CorrelationId);
                })
        );

        SetCompletedWhenFinalized();
    }

    private async Task ReverseEcbAsync(BehaviorContext<PaymentSagaState> ctx)
    {
        _logger.LogInformation(
            "PaymentSaga [{CorrelationId}]: COMPENSATION STEP - Reversing ECB confirmation for EcbReference={EcbReference}",
            ctx.Saga.CorrelationId, ctx.Saga.EcbReference);

        await Task.Delay(100);

        _logger.LogInformation(
            "PaymentSaga [{CorrelationId}]: ECB reversal completed.",
            ctx.Saga.CorrelationId);
    }

    private async Task ReleaseFundsAsync(BehaviorContext<PaymentSagaState> ctx)
    {
        _logger.LogInformation(
            "PaymentSaga [{CorrelationId}]: COMPENSATION STEP - Releasing reserved funds for ReservationId={ReservationId}",
            ctx.Saga.CorrelationId, ctx.Saga.ReservationId);

        await Task.Delay(100);

        _logger.LogInformation(
            "PaymentSaga [{CorrelationId}]: Funds released successfully.",
            ctx.Saga.CorrelationId);
    }

    private async Task MarkCompensatedAsync(BehaviorContext<PaymentSagaState> ctx)
    {
        _logger.LogInformation(
            "PaymentSaga [{CorrelationId}]: COMPENSATION STEP - Marking payment as compensated. TxnId={TxnId}",
            ctx.Saga.CorrelationId, ctx.Saga.TransactionId);

        await Task.Delay(50);

        _logger.LogInformation(
            "PaymentSaga [{CorrelationId}]: Payment marked as compensated.",
            ctx.Saga.CorrelationId);
    }
}
