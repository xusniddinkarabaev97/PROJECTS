namespace GzsBilling.Api.Services;

public class PaymentInfoResponse
{
    public int filling_station_id { get; set; }
    public int dispenser_id { get; set; }
    public int operation_id { get; set; }
    public decimal amount { get; set; }
    public decimal volume { get; set; }
    public DateTime created_at { get; set; }
    public string? car_number { get; set; }
}

public class UgazLoginResponse
{
    public string? access_token { get; set; }
    public string? token_type { get; set; }
}

public class UgazProcessResponse
{
    public string? message { get; set; }
    public string? status { get; set; }
}
