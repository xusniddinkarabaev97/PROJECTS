using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.Logging;
using GzsBilling.Domain.Entities;
using GzsBilling.Application.Services;

namespace GzsBilling.Application.Commands.Refunds;

public class CreateRefundCommand : IRequest<CreateRefundResult>
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string InitiatorId { get; set; } = string.Empty;
    public string InitiatorRole { get; set; } = string.Empty;
}

public class CreateRefundResult
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
    public string? AlertMessage { get; set; }
}

public class DuplicateRefundException : Exception
{
    public string TransactionId { get; }

    public DuplicateRefundException(string transactionId)
        : base(string.Format("A refund already exists for transaction '{0}'", transactionId))
    {
        TransactionId = transactionId;
    }

    public DuplicateRefundException(string transactionId, string existingRefundId)
        : base(string.Format("A refund ('{0}') already exists for transaction '{1}'", existingRefundId, transactionId))
    {
        TransactionId = transactionId;
    }
}

public class TransactionNotFoundException : Exception
{
    public string TransactionId { get; }

    public TransactionNotFoundException(string transactionId)
        : base(string.Format("Transaction '{0}' not found", transactionId))
    {
        TransactionId = transactionId;
    }
}

public class CreateRefundCommandHandler : IRequestHandler<CreateRefundCommand, CreateRefundResult>
{
    private readonly ITransactionStore _transactionStore;
    private readonly AntiFraudService _antiFraudService;
    private readonly ILogger<CreateRefundCommandHandler> _logger;

    private readonly ConcurrentDictionary<string, Refund> _refunds = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _transactionRefunds = new();

    public CreateRefundCommandHandler(
        ITransactionStore transactionStore,
        AntiFraudService antiFraudService,
        ILogger<CreateRefundCommandHandler> logger)
    {
        _transactionStore = transactionStore;
        _antiFraudService = antiFraudService;
        _logger = logger;
    }

    public Task<CreateRefundResult> Handle(CreateRefundCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing CreateRefundCommand: TxnId={TxnId}, Amount={Amount}, InitiatorId={InitiatorId}, InitiatorRole={InitiatorRole}",
            request.TransactionId, request.Amount, request.InitiatorId, request.InitiatorRole);

        // Step 1: Validate transaction exists and is Completed
        var transaction = _transactionStore.GetByTransactionId(request.TransactionId);
        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found: TxnId={TxnId}", request.TransactionId);
            throw new TransactionNotFoundException(request.TransactionId);
        }

        if (transaction.Status != TransactionStatus.Completed)
        {
            var msg = string.Format(
                "Transaction '{0}' is not in Completed status. Current status: {1}",
                request.TransactionId, transaction.Status);
            _logger.LogWarning(msg);
            throw new InvalidOperationException(msg);
        }

        _logger.LogInformation(
            "Transaction validated: TxnId={TxnId}, OriginalAmount={OriginalAmount}, Status={Status}",
            request.TransactionId, transaction.Amount, transaction.Status);

        // Step 2: Check for duplicate refund
        if (_transactionRefunds.TryGetValue(request.TransactionId, out var existingRefunds) &&
            existingRefunds.Count > 0)
        {
            var existingRefundId = existingRefunds.First();
            _logger.LogWarning(
                "Duplicate refund attempt detected: TxnId={TxnId}, ExistingRefundId={ExistingRefundId}",
                request.TransactionId, existingRefundId);
            throw new DuplicateRefundException(request.TransactionId, existingRefundId);
        }

        // Step 3: Validate amount
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Refund amount must be greater than zero.", nameof(request.Amount));
        }

        if (request.Amount > transaction.Amount)
        {
            throw new ArgumentException(
                string.Format("Refund amount ({0:N2}) cannot exceed original transaction amount ({1:N2}).",
                    request.Amount, transaction.Amount));
        }

        // Step 4: Run anti-fraud checks
        var fraudChecksPassed = true;
        var fraudFailureReasons = new List<string>();

        var dailyLimitResult = _antiFraudService.CheckDailyRefundLimit(request.InitiatorId);
        if (!dailyLimitResult.Passed)
        {
            fraudChecksPassed = false;
            fraudFailureReasons.Add(dailyLimitResult.Reason);
        }

        var weeklyLimitResult = _antiFraudService.CheckWeeklyRefundLimit(request.InitiatorId);
        if (!weeklyLimitResult.Passed)
        {
            fraudChecksPassed = false;
            fraudFailureReasons.Add(weeklyLimitResult.Reason);
        }

        var amountLimitResult = _antiFraudService.CheckRefundAmountLimit(request.InitiatorId, request.Amount);
        if (!amountLimitResult.Passed)
        {
            fraudChecksPassed = false;
            fraudFailureReasons.Add(amountLimitResult.Reason);
        }

        var amountMatchResult = _antiFraudService.CheckAmountMatch(transaction.Amount, request.Amount);
        if (!amountMatchResult.Passed)
        {
            fraudChecksPassed = false;
            fraudFailureReasons.Add(amountMatchResult.Reason);
        }

        var frequencyResult = _antiFraudService.CheckRefundFrequency(request.InitiatorId);
        if (!frequencyResult.Passed)
        {
            fraudChecksPassed = false;
            fraudFailureReasons.Add(frequencyResult.Reason);
        }

        // Step 5-10: Determine fraud status and create refund
        var now = DateTimeOffset.UtcNow;
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            RefundId = GenerateRefundId(now),
            OriginalTransactionId = request.TransactionId,
            OriginalAmount = transaction.Amount,
            RefundAmount = request.Amount,
            Currency = transaction.Currency,
            InitiatorId = request.InitiatorId,
            InitiatorRole = request.InitiatorRole,
            Reason = request.Reason,
            ReasonCode = request.ReasonCode,
            CreatedAt = now,
            FraudCheckPassed = fraudChecksPassed,
            StatusHistory = new List<RefundStatusHistory>()
        };

        string? alertMessage = null;

        if (!fraudChecksPassed)
        {
            refund.Status = RefundStatus.FraudCheckPending;
            refund.FraudCheckNotes = string.Join(" | ", fraudFailureReasons);

            alertMessage = string.Format(
                "FRAUD_CHECK_FAILED: Refund {0} for transaction {1} requires manual review. Reasons: {2}",
                refund.RefundId, request.TransactionId, refund.FraudCheckNotes);

            _logger.LogWarning("AntiFraud: {AlertMessage}", alertMessage);

            refund.StatusHistory.Add(new RefundStatusHistory
            {
                Status = RefundStatus.Initiated,
                Timestamp = now,
                ChangedBy = request.InitiatorId,
                Notes = "Refund initiated"
            });

            refund.StatusHistory.Add(new RefundStatusHistory
            {
                Status = RefundStatus.FraudCheckPending,
                Timestamp = now,
                ChangedBy = "system",
                Notes = refund.FraudCheckNotes
            });
        }
        else
        {
            refund.Status = RefundStatus.Approved;
            refund.SlaDeadline = now.AddHours(24);

            _logger.LogInformation(
                "Refund approved: RefundId={RefundId}, TxnId={TxnId}, SlaDeadline={SlaDeadline}",
                refund.RefundId, request.TransactionId, refund.SlaDeadline);

            refund.StatusHistory.Add(new RefundStatusHistory
            {
                Status = RefundStatus.Initiated,
                Timestamp = now,
                ChangedBy = request.InitiatorId,
                Notes = "Refund initiated"
            });

            refund.StatusHistory.Add(new RefundStatusHistory
            {
                Status = RefundStatus.Approved,
                Timestamp = now,
                ChangedBy = "system",
                Notes = "All fraud checks passed. Refund approved automatically."
            });

            _antiFraudService.RecordRefund(request.InitiatorId, request.Amount);
        }

        // Store refund
        _refunds[refund.RefundId] = refund;

        var refundSet = _transactionRefunds.GetOrAdd(request.TransactionId, _ => new HashSet<string>());
        lock (refundSet)
        {
            refundSet.Add(refund.RefundId);
        }

        // Full audit trail logging
        _logger.LogInformation(
            "REFUND_AUDIT: RefundId={RefundId}, TxnId={TxnId}, Amount={Amount}, Currency={Currency}, " +
            "OriginalAmount={OriginalAmount}, Status={Status}, FraudCheckPassed={FraudCheckPassed}, " +
            "InitiatorId={InitiatorId}, InitiatorRole={InitiatorRole}, Reason={Reason}, ReasonCode={ReasonCode}, " +
            "SlaDeadline={SlaDeadline}, CreatedAt={CreatedAt}",
            refund.RefundId, refund.OriginalTransactionId, refund.RefundAmount, refund.Currency,
            refund.OriginalAmount, refund.Status, refund.FraudCheckPassed,
            refund.InitiatorId, refund.InitiatorRole, refund.Reason, refund.ReasonCode,
            refund.SlaDeadline, refund.CreatedAt);

        var result = new CreateRefundResult
        {
            Success = true,
            RefundId = refund.RefundId,
            Status = refund.Status,
            AlertMessage = alertMessage
        };

        _logger.LogInformation(
            "CreateRefundCommand completed: RefundId={RefundId}, Status={Status}",
            result.RefundId, result.Status);

        return Task.FromResult(result);
    }

    private static string GenerateRefundId(DateTimeOffset timestamp)
    {
        var datePart = timestamp.ToString("yyyyMMdd");
        var guidPart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return string.Format("REF-{0}-{1}", datePart, guidPart);
    }
}
