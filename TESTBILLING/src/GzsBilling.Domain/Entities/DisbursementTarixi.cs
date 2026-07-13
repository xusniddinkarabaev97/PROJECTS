using GzsBilling.Domain.Enums;

namespace GzsBilling.Domain.Entities;

public class DisbursementTarixi
{
    public Guid Id { get; set; }
    public Guid StakeholderId { get; set; }
    public decimal Amount { get; set; }
    public string BankReference { get; set; } = string.Empty;
    public DisbursementStatus Status { get; set; } = DisbursementStatus.Pending;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public Stakeholder Stakeholder { get; set; } = null!;
    public Guid TranzaksiyaId { get; set; }
    public Tranzaksiya Tranzaksiya { get; set; } = null!;
}
