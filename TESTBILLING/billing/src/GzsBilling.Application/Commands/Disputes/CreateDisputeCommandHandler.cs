using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GzsBilling.Application.Commands.Disputes;

/// <summary>
/// Handles CreateDisputeCommand. Generates a new Dispute with a unique ID,
/// sets SLA deadline to +30 calendar days, and creates the initial history entry.
/// </summary>
public class CreateDisputeCommandHandler : IRequestHandler<CreateDisputeCommand, string>
{
    private readonly ILogger<CreateDisputeCommandHandler> _logger;

    // In-memory store for disputes (production would use a repository/database).
    private static readonly Dictionary<string, Dispute> _disputes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _lock = new();

    public CreateDisputeCommandHandler(ILogger<CreateDisputeCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<string> Handle(CreateDisputeCommand request, CancellationToken cancellationToken)
    {
        string disputeId = GenerateDisputeId();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var dispute = new Dispute
        {
            DisputeId = disputeId,
            TransactionId = request.TransactionId,
            ContragentId = request.ContragentId,
            Amount = request.Amount,
            Reason = request.Reason,
            Status = DisputeStatus.Open,
            CreatedAt = now,
            SlaDeadline = now.AddDays(30),
            History = new List<DisputeHistoryEntry>
            {
                new DisputeHistoryEntry
                {
                    Timestamp = now,
                    Action = "DisputeCreated",
                    ChangedBy = request.CreatedBy,
                    Notes = $"Dispute created for transaction {request.TransactionId}. Reason: {request.Reason}",
                    PreviousStatus = null,
                    NewStatus = DisputeStatus.Open
                }
            },
            Evidence = new List<DisputeEvidence>()
        };

        lock (_lock)
        {
            _disputes[disputeId] = dispute;
        }

        _logger.LogInformation(
            "Dispute created: DisputeId={DisputeId}, TxnId={TxnId}, Contragent={Contragent}, Amount={Amount}, SlaDeadline={SlaDeadline}",
            disputeId, request.TransactionId, request.ContragentId, request.Amount, dispute.SlaDeadline);

        return Task.FromResult(disputeId);
    }

    /// <summary>
    /// Generates a dispute ID in the format DSP-YYYYMMDD-{8 char GUID}.
    /// </summary>
    private static string GenerateDisputeId()
    {
        string datePart = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        string guidPart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"DSP-{datePart}-{guidPart}";
    }

    /// <summary>
    /// Retrieves a dispute by ID. Used internally by other handlers.
    /// </summary>
    public static Dispute? GetDispute(string disputeId)
    {
        lock (_lock)
        {
            return _disputes.TryGetValue(disputeId, out var dispute) ? dispute : null;
        }
    }

    public static void UpdateDispute(Dispute dispute)
    {
        lock (_lock)
        {
            _disputes[dispute.DisputeId] = dispute;
        }
    }

    public static IEnumerable<Dispute> ListDisputes(
        string? status = null,
        string? contragentId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
    {
        lock (_lock)
        {
            var query = _disputes.Values.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(contragentId))
            {
                query = query.Where(d => d.ContragentId.Equals(contragentId, StringComparison.OrdinalIgnoreCase));
            }

            if (from.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(d => d.CreatedAt <= to.Value);
            }

            return query.OrderByDescending(d => d.CreatedAt).ToList();
        }
    }
}
