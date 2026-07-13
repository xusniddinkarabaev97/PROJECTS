namespace GzsBilling.Domain.Models;

public class UGazSeansResponse
{
    public int filling_station_id { get; set; }
    public int dispenser_id { get; set; }
    public int operation_id { get; set; }
    public decimal amount { get; set; }
    public decimal volume { get; set; }
    public DateTime created_at { get; set; }
    public string car_number { get; set; } = string.Empty;
}
