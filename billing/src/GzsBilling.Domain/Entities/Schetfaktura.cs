namespace GzsBilling.Domain.Entities;

public class Schetfaktura
{
    public Guid Id { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SystemCommission { get; set; }
    public decimal NetDistributionAmount { get; set; }
    public string CalculationJson { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
    public bool IsPaid { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
