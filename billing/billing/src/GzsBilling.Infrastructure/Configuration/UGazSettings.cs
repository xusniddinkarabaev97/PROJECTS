namespace GzsBilling.Infrastructure.Configuration;

public class UGazSettings
{
    public string BaseUrl { get; set; } = "https://dev.uzinfocom-payment.ugaz.uz";
    public string AuthUrl { get; set; } = "/api/v1/auth/login";
    public string StartPaymentUrl { get; set; } = "/api/v1/payments/start";
    public string ProcessPaymentUrl { get; set; } = "/api/v1/payments/process";
    public string Username { get; set; } = "username";
    public string Password { get; set; } = "password";
    public int TimeoutSeconds { get; set; } = 30;
}
