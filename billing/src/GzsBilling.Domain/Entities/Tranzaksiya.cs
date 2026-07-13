using GzsBilling.Domain.Enums;

namespace GzsBilling.Domain.Entities;

public class Tranzaksiya
{
    public Guid Id { get; set; }
    public decimal TotalSum { get; set; }
    public int FillingStationId { get; set; }
    public int? DispenserId { get; set; }
    public Dispenser? Dispenser { get; set; }
    public string CardType { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int PaymentId { get; set; }
    public TranzaksiyaStatus Status { get; set; } = TranzaksiyaStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Payment Payment { get; set; } = null!;

    public ICollection<DisbursementTarixi> DisbursementHistory { get; set; } = new List<DisbursementTarixi>();
}
