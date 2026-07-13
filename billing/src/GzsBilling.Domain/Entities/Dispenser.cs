namespace GzsBilling.Domain.Entities;

/// <summary>
/// Fuel dispenser (kolonka) at a filling station.
/// </summary>
public class Dispenser
{
    public int Id { get; set; }
    public int FillingStationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FuelType { get; set; } = "AI-92";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public FillingStation FillingStation { get; set; } = null!;
    public ICollection<Tranzaksiya> Tranzaktsiyalar { get; set; } = new List<Tranzaksiya>();
}
