namespace GzsBilling.Domain.Entities;

public class Shareholder
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public decimal OwnershipPercentage { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Company { get; set; }
    public decimal SharePercentage { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContractNumber { get; set; }
    public DateTimeOffset ContractDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
