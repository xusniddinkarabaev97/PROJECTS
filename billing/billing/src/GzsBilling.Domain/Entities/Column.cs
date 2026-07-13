namespace GzsBilling.Domain.Entities;

public class Column
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string ColumnNumber { get; set; } = string.Empty;
    public decimal PricePerLiter { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
