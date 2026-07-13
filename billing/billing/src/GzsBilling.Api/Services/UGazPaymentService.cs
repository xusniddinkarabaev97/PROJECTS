using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GzsBilling.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GzsBilling.Api.Services;

public class UGazPaymentService : IUGazPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly UGazSettings _settings;
    private readonly ILogger<UGazPaymentService> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public UGazPaymentService(
        HttpClient httpClient,
        IOptions<UGazSettings> settings,
        ILogger<UGazPaymentService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<UgazApiResult<PaymentInfoResponse>> StartPaymentAsync(int stationId, int dispenserId)
    {
        var payload = new { filling_station_id = stationId, dispenser_id = dispenserId };
        var url = $"{_settings.BaseUrl}{_settings.StartPaymentUrl}";

        _logger.LogInformation("UGaz StartPayment: Station={Station}, Dispenser={Dispenser}, URL={Url}",
            stationId, dispenserId, url);

        return await SendAuthenticatedRequestAsync<PaymentInfoResponse>(HttpMethod.Post, url, payload);
    }

    public async Task<UgazApiResult<UgazProcessResponse>> ProcessPaymentAsync(
        int stationId, int operationId, string paymentMethod, string paymentStatus)
    {
        var payload = new
        {
            filling_station_id = stationId,
            operation_id = operationId,
            payment_date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            payment_method = paymentMethod,
            payment_status = paymentStatus
        };
        var url = $"{_settings.BaseUrl}{_settings.ProcessPaymentUrl}";

        _logger.LogInformation(
            "UGaz ProcessPayment: Station={Station}, Op={Op}, Method={Method}, Status={Status}, URL={Url}",
            stationId, operationId, paymentMethod, paymentStatus, url);

        return await SendAuthenticatedRequestAsync<UgazProcessResponse>(HttpMethod.Post, url, payload);
    }

    // ──────────────────────────────────────────────
    // Core: authenticated request with auto-token & retry-on-401
    // ──────────────────────────────────────────────
    private async Task<UgazApiResult<T>> SendAuthenticatedRequestAsync<T>(
        HttpMethod method, string url, object? body)
    {
        var requestInfo = new UgazRequestInfo
        {
            Url = url,
            Method = method.Method
        };

        string? requestBodyJson = null;
        if (body is not null)
        {
            requestBodyJson = JsonSerializer.Serialize(body, SnakeCaseOptions);
            requestInfo.RequestBody = requestBodyJson;
        }

        // Try request, retry once on 401 with fresh token
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var token = await GetOrRefreshTokenAsync();
            if (token is null)
            {
                return new UgazApiResult<T>
                {
                    Success = false,
                    HttpCode = 0,
                    ErrorMessage = "Failed to obtain access token from UGaz auth endpoint.",
                    RequestInfo = requestInfo
                };
            }

            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            requestInfo.RequestHeaders = $"Authorization: Bearer {token[..Math.Min(token.Length, 20)]}...";

            if (requestBodyJson is not null)
            {
                request.Content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UGaz HTTP request failed: {Url}", url);
                return new UgazApiResult<T>
                {
                    Success = false,
                    HttpCode = 0,
                    ErrorMessage = $"HTTP request exception: {ex.Message}",
                    RequestInfo = requestInfo
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            requestInfo.ResponseBody = responseBody;
            requestInfo.ResponseHeaders = response.Headers.ToString();

            _logger.LogDebug("UGaz response [{Code}]: {Body}", (int)response.StatusCode, responseBody);

            // 401 → force refresh token and retry
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && attempt == 0)
            {
                _logger.LogWarning("UGaz 401 received, refreshing token and retrying...");
                await ForceRefreshTokenAsync();
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return new UgazApiResult<T>
                {
                    Success = false,
                    HttpCode = (int)response.StatusCode,
                    ErrorMessage = $"UGaz API returned {(int)response.StatusCode}: {responseBody}",
                    RequestInfo = requestInfo
                };
            }

            // HTTP 2xx with empty body → success with default(T)
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                _logger.LogDebug("UGaz returned {Code} with empty body, treating as success.", (int)response.StatusCode);
                return new UgazApiResult<T>
                {
                    Success = true,
                    HttpCode = (int)response.StatusCode,
                    Data = default,
                    RequestInfo = requestInfo
                };
            }

            T? data;
            try
            {
                data = JsonSerializer.Deserialize<T>(responseBody, SnakeCaseOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize UGaz response: {Body}", responseBody);
                return new UgazApiResult<T>
                {
                    Success = false,
                    HttpCode = (int)response.StatusCode,
                    ErrorMessage = $"JSON deserialization failed: {ex.Message}",
                    RequestInfo = requestInfo
                };
            }

            return new UgazApiResult<T>
            {
                Success = true,
                HttpCode = (int)response.StatusCode,
                Data = data,
                RequestInfo = requestInfo
            };
        }

        return new UgazApiResult<T>
        {
            Success = false,
            HttpCode = 401,
            ErrorMessage = "Authentication failed after token refresh.",
            RequestInfo = requestInfo
        };
    }

    // ──────────────────────────────────────────────
    // Token management
    // ──────────────────────────────────────────────
    private async Task<string?> GetOrRefreshTokenAsync()
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        await _tokenLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            await LoginAsync();
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task ForceRefreshTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            await LoginAsync();
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task LoginAsync()
    {
        var authUrl = $"{_settings.BaseUrl}{_settings.AuthUrl}";
        var payload = new
        {
            username = _settings.Username,
            password = _settings.Password
        };
        var bodyJson = JsonSerializer.Serialize(payload, SnakeCaseOptions);

        _logger.LogInformation("UGaz authenticating at {Url} with user {User}", authUrl, _settings.Username);

        using var request = new HttpRequestMessage(HttpMethod.Post, authUrl)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UGaz auth request failed");
            _cachedToken = null;
            _tokenExpiry = DateTime.MinValue;
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("UGaz auth failed [{Code}]: {Body}", (int)response.StatusCode, responseBody);
            _cachedToken = null;
            _tokenExpiry = DateTime.MinValue;
            return;
        }

        UgazLoginResponse? loginResult;
        try
        {
            loginResult = JsonSerializer.Deserialize<UgazLoginResponse>(responseBody, SnakeCaseOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize UGaz auth response: {Body}", responseBody);
            _cachedToken = null;
            _tokenExpiry = DateTime.MinValue;
            return;
        }

        if (loginResult?.access_token is null)
        {
            _logger.LogError("UGaz auth response missing access_token: {Body}", responseBody);
            _cachedToken = null;
            _tokenExpiry = DateTime.MinValue;
            return;
        }

        _cachedToken = loginResult.access_token;
        // Assume token lives 1 hour; expire 5 min early for safety
        _tokenExpiry = DateTime.UtcNow.AddHours(1).Subtract(TimeSpan.FromMinutes(5));

        _logger.LogInformation("UGaz authentication successful, token cached until {Expiry}", _tokenExpiry);
    }
}
