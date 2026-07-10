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

        if (!string.IsNullOrEmpty(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        }

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                1,
                retryAttempt => TimeSpan.FromSeconds(1),
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} for UGaz API after {Delay}s. Status: {StatusCode}",
                        retryCount, timeSpan.TotalSeconds,
                        outcome.Result?.StatusCode);
                });
    }

    public async Task<UGazSeansResponse?> GetZapravkaSeansAsync(int fillingStationId, int dispenserId)
    {
        var requestUri = "/api/v1/payments/start";
        var requestBody = JsonSerializer.Serialize(new
        {
            filling_station_id = fillingStationId,
            dispenser_id = dispenserId
        });

        _logger.LogInformation("Calling UGaz API: {Uri} with body: {Body}", requestUri, requestBody);

        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync(requestUri, content));

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("UGaz API raw response: {Response}", responseContent);

        var result = JsonSerializer.Deserialize<UGazSeansResponse>(responseContent, JsonOptions);

        _logger.LogInformation("UGaz API response for station {StationId}, dispenser {DispenserId}: Amount={Amount}",
            fillingStationId, dispenserId, result?.amount);

        return result;
    }
}
