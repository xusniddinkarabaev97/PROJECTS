namespace GzsBilling.Domain.Entities;

public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/json";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
