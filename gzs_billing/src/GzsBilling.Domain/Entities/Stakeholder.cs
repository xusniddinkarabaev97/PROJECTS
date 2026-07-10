namespace GzsBilling.Domain.Entities;

public class Stakeholder
{
    public Guid Id { get; set; }
    public int FillingStationId { get; set; }
    public int PaymentId { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public decimal SharePercent { get; set; }
    public string FullName { get; set; } = string.Empty;

    public Payment Payment { get; set; } = null!;

    public ICollection<DisbursementTarixi> DisbursementHistory { get; set; } = new List<DisbursementTarixi>();
}
