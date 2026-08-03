using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ODULink.DTOs;
using Microsoft.Extensions.Options;

namespace ODULink.Services
{
    public class EmisService : IEmisService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmisService> _logger;
        private readonly string _baseUrl;
        private readonly string _authToken;

        public EmisService(HttpClient httpClient, IConfiguration configuration, ILogger<EmisService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var settings = configuration.GetSection("EmisSettings");
            _baseUrl = settings["BaseUrl"] ?? throw new InvalidOperationException("EmisSettings:BaseUrl not configured");
            _authToken = settings["AuthToken"] ?? throw new InvalidOperationException("EmisSettings:AuthToken not configured");

            var timeoutSec = int.Parse(settings["TimeoutSeconds"] ?? "5");
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSec);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authToken);
        }

        public async Task<EmisCheckResponse> CheckPatientAsync(EmisCheckRequest request)
        {
            var url = $"{_baseUrl}/api/v1/billing/check";

            _logger.LogInformation("EMIS Check: requestId={RequestId}, patientId={PatientId}",
                request.RequestId, request.PatientId);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("EMIS Check response: status={StatusCode}, body={Body}",
                (int)response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<EmisCheckResponse>(body);
                return errorResponse ?? new EmisCheckResponse
                {
                    RequestId = request.RequestId,
                    Status = "NOT_FOUND",
                    Message = $"EMIS returned HTTP {(int)response.StatusCode}"
                };
            }

            var result = JsonSerializer.Deserialize<EmisCheckResponse>(body);
            return result ?? new EmisCheckResponse
            {
                RequestId = request.RequestId,
                Status = "NOT_FOUND",
                Message = "Empty response from EMIS"
            };
        }

        public async Task<EmisPayResponse> ConfirmPaymentAsync(EmisPayRequest request)
        {
            var url = $"{_baseUrl}/api/v1/billing/pay";

            _logger.LogInformation("EMIS Pay: requestId={RequestId}, invoiceId={InvoiceId}, amount={Amount}",
                request.RequestId, request.InvoiceId, request.Transaction.Amount);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("EMIS Pay response: status={StatusCode}, body={Body}",
                (int)response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<EmisPayResponse>(body);
                return errorResponse ?? new EmisPayResponse
                {
                    RequestId = request.RequestId,
                    Status = "FAILED",
                    Message = $"EMIS returned HTTP {(int)response.StatusCode}"
                };
            }

            var result = JsonSerializer.Deserialize<EmisPayResponse>(body);
            return result ?? new EmisPayResponse
            {
                RequestId = request.RequestId,
                Status = "FAILED",
                Message = "Empty response from EMIS"
            };
        }
    }
}
