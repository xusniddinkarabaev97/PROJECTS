using System.Text;
using System.Text.Json;
using GzsBilling.Domain.Configuration;
using GzsBilling.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace GzsBilling.Infrastructure.Clients;

public class UGazInfrastrukturaClient : IUGazInfrastrukturaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UGazInfrastrukturaClient> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly string _authToken;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public UGazInfrastrukturaClient(
        HttpClient httpClient,
        ILogger<UGazInfrastrukturaClient> logger,
        IOptions<BillingOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authToken = options.Value.UGaz.AuthToken ?? string.Empty;

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(1, _ => TimeSpan.FromSeconds(1),
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for UGaz API after {Delay}s. Status: {StatusCode}",
                        retryCount, timeSpan.TotalSeconds, outcome.Result?.StatusCode);
                });
    }

    private async Task<string> GetTokenAsync()
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        // Try Bearer token from config first
        if (!string.IsNullOrEmpty(_authToken) && !_authToken.Contains(":"))
        {
            _cachedToken = _authToken;
            _tokenExpiry = DateTime.MaxValue;
            return _cachedToken;
        }

        // Login with username:password
        var parts = _authToken.Split(':', 2);
        if (parts.Length == 2)
        {
            var loginBody = JsonSerializer.Serialize(new { username = parts[0], password = parts[1] });
            var content = new StringContent(loginBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/v1/auth/login", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            _cachedToken = result.GetProperty("access_token").GetString()!;
            _tokenExpiry = DateTime.UtcNow.AddHours(1);
            _logger.LogInformation("UGaz token obtained successfully");
            return _cachedToken!;
        }

        throw new InvalidOperationException("Invalid UGaz AuthToken format");
    }

    public async Task<UGazSeansResponse?> GetZapravkaSeansAsync(int fillingStationId, int dispenserId)
    {
        var token = await GetTokenAsync();
        var requestUri = "/api/v1/payments/start";
        var requestBody = JsonSerializer.Serialize(new
        {
            filling_station_id = fillingStationId,
            dispenser_id = dispenserId
        });

        _logger.LogInformation("Calling UGaz API: {Uri} with body: {Body}", requestUri, requestBody);

        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _retryPolicy.ExecuteAsync(() => _httpClient.SendAsync(request));

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("UGaz API raw response: {Response}", responseContent);

        var result = JsonSerializer.Deserialize<UGazSeansResponse>(responseContent, JsonOptions);

        _logger.LogInformation("UGaz API response for station {StationId}, dispenser {DispenserId}: Amount={Amount}",
            fillingStationId, dispenserId, result?.amount);

        return result;
    }
}
