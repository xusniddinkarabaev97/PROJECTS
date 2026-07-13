namespace GzsBilling.Api.Services;

public interface IUGazPaymentService
{
    Task<UgazApiResult<PaymentInfoResponse>> StartPaymentAsync(int stationId, int dispenserId);
    Task<UgazApiResult<UgazProcessResponse>> ProcessPaymentAsync(int stationId, int operationId, string paymentMethod, string paymentStatus);
}

public class UgazApiResult<T>
{
    public bool Success { get; set; }
    public int HttpCode { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public UgazRequestInfo? RequestInfo { get; set; }
}

public class UgazRequestInfo
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string RequestHeaders { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public string? ResponseBody { get; set; }
    public string? ResponseHeaders { get; set; }
}
