namespace GzsBilling.Domain.Enums;

/// <summary>
/// Tracks the lifecycle of a dispute from creation through resolution.
/// </summary>
public enum DisputeStatus
{
    /// <summary>Dispute has been created but not yet assigned or reviewed.</summary>
    Open = 0,

    /// <summary>Dispute is actively being investigated.</summary>
    UnderReview = 1,

    /// <summary>Dispute has been resolved in favor of the claimant.</summary>
    Resolved = 2,

    /// <summary>Dispute has been rejected / denied.</summary>
    Rejected = 3,

    /// <summary>Dispute was cancelled by the initiator before review.</summary>
    Cancelled = 4
}
