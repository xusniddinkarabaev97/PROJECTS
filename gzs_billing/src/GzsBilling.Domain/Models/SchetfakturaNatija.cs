namespace GzsBilling.Domain.Models;

public class SchetfakturaNatija
{
    public decimal TotalAmount { get; set; }
    public decimal SystemCommission { get; set; }
    public decimal NetDistributionAmount { get; set; }
    public decimal BankSplit { get; set; }
    public decimal PlatformSplit { get; set; }
    public List<StakeholderLineItem> StakeholderPayouts { get; set; } = new();
}

public class StakeholderLineItem
{
    public Guid StakeholderId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public decimal SharePercent { get; set; }
    public decimal PayoutAmount { get; set; }
}
