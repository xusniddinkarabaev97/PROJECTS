using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace GzsBilling.Api.Controllers;

[ApiController]
[Route("api/v1/scanner")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "QR")]
public class ScannerProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ScannerProxyController> _logger;

    public ScannerProxyController(IHttpClientFactory httpClientFactory, ILogger<ScannerProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("ugaz")]
    public async Task<IActionResult> ProxyUGaz([FromBody] ScannerRequest request, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        // Login
        var loginBody = System.Text.Json.JsonSerializer.Serialize(new { username = "uzinfocom_billing", password = "o7G1UA5juQIai" });
        var loginContent = new StringContent(loginBody, System.Text.Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync("https://uzinfocom-payment.ugaz.uz/api/v1/auth/login", loginContent, ct);
        loginResponse.EnsureSuccessStatusCode();
        var loginJson = await loginResponse.Content.ReadAsStringAsync(ct);
        var token = System.Text.Json.JsonDocument.Parse(loginJson).RootElement.GetProperty("access_token").GetString();

        // Start payment
        var payBody = System.Text.Json.JsonSerializer.Serialize(new { filling_station_id = request.StationId, dispenser_id = request.DispenserId });
        var payRequest = new HttpRequestMessage(HttpMethod.Post, "https://uzinfocom-payment.ugaz.uz/api/v1/payments/start")
        {
            Content = new StringContent(payBody, System.Text.Encoding.UTF8, "application/json")
        };
        payRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var payResponse = await client.SendAsync(payRequest, ct);
        var payJson = await payResponse.Content.ReadAsStringAsync(ct);

        if (!payResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("UGaz proxy failed: {Code} {Body}", payResponse.StatusCode, payJson);
            return StatusCode((int)payResponse.StatusCode, System.Text.Json.JsonSerializer.Deserialize<object>(payJson));
        }

        return Content(payJson, "application/json");
    }
}

public class ScannerRequest
{
    [Required] public int StationId { get; set; }
    [Required] public int DispenserId { get; set; }
}
