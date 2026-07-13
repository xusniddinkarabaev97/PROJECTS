using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GzsBilling.Application.Commands.Disputes;

/// <summary>
/// Handles UpdateDisputeStatusCommand. Validates status transition rules,
/// records a history entry, and sets resolution metadata when applicable.
///
/// Allowed transitions:
///   Open        → UnderReview
///   UnderReview → Resolved | Rejected
///   Rejected    → UnderReview  (re-open)
///   Any         → Cancelled
/// </summary>
public class UpdateDisputeStatusCommandHandler : IRequestHandler<UpdateDisputeStatusCommand, Dispute>
{
    private readonly ILogger<UpdateDisputeStatusCommandHandler> _logger;

    private static readonly Dictionary<DisputeStatus, HashSet<DisputeStatus>> AllowedTransitions = new()
    {
        [DisputeStatus.Open] = new HashSet<DisputeStatus> { DisputeStatus.UnderReview, DisputeStatus.Cancelled },
        [DisputeStatus.UnderReview] = new HashSet<DisputeStatus> { DisputeStatus.Resolved, DisputeStatus.Rejected, DisputeStatus.Cancelled },
        [DisputeStatus.Rejected] = new HashSet<DisputeStatus> { DisputeStatus.UnderReview, DisputeStatus.Cancelled },
        [DisputeStatus.Resolved] = new HashSet<DisputeStatus> { DisputeStatus.Cancelled },
        [DisputeStatus.Cancelled] = new HashSet<DisputeStatus>()
    };

    public UpdateDisputeStatusCommandHandler(ILogger<UpdateDisputeStatusCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<Dispute> Handle(UpdateDisputeStatusCommand request, CancellationToken cancellationToken)
    {
        Dispute? dispute = CreateDisputeCommandHandler.GetDispute(request.DisputeId);

        if (dispute is null)
        {
            throw new KeyNotFoundException($"Dispute '{request.DisputeId}' not found.");
        }

        // ── Validate status transition ──────────────────────────────────
        DisputeStatus currentStatus = dispute.Status;

        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedTargets)
            || !allowedTargets.Contains(request.NewStatus))
        {
            string allowed = allowedTargets is not null
                ? string.Join(", ", allowedTargets)
                : "none";

            throw new InvalidOperationException(
                $"Invalid status transition from '{currentStatus}' to '{request.NewStatus}'. " +
                $"Allowed transitions: {allowed}.");
        }

        // ── Create history entry ────────────────────────────────────────
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string action = $"StatusChanged_{currentStatus}_To_{request.NewStatus}";

        dispute.History.Add(new DisputeHistoryEntry
        {
            Timestamp = now,
            Action = action,
            ChangedBy = request.ChangedBy,
            Notes = request.Notes,
            PreviousStatus = currentStatus,
            NewStatus = request.NewStatus
        });

        // ── Apply status change ─────────────────────────────────────────
        dispute.Status = request.NewStatus;

        if (request.NewStatus == DisputeStatus.Resolved)
        {
            dispute.ResolvedAt = now;
            dispute.ResolutionNotes = request.Notes;
        }

        if (request.NewStatus == DisputeStatus.Rejected)
        {
            dispute.ResolutionNotes = request.Notes;
        }

        CreateDisputeCommandHandler.UpdateDispute(dispute);

        _logger.LogInformation(
            "Dispute status updated: DisputeId={DisputeId}, {OldStatus}→{NewStatus}, ChangedBy={ChangedBy}",
            request.DisputeId, currentStatus, request.NewStatus, request.ChangedBy);

        return Task.FromResult(dispute);
    }
}
