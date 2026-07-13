using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using MediatR;

namespace GzsBilling.Application.Commands.Disputes;

/// <summary>
/// Command to transition a dispute to a new status.
/// </summary>
public class UpdateDisputeStatusCommand : IRequest<Dispute>
{
    public string DisputeId { get; }
    public DisputeStatus NewStatus { get; }
    public string ChangedBy { get; }
    public string? Notes { get; }

    public UpdateDisputeStatusCommand(
        string disputeId,
        DisputeStatus newStatus,
        string changedBy,
        string? notes)
    {
        DisputeId = disputeId;
        NewStatus = newStatus;
        ChangedBy = changedBy;
        Notes = notes;
    }
}
