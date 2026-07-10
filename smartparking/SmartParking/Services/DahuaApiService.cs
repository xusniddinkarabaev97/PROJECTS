using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SmartParking.DTOs;
using Microsoft.Extensions.Logging;

namespace SmartParking.Services
{
    /// <summary>
    /// HTTP client for Dahua DSS Professional REST API
    /// Handles authentication, barrier control, device management
    /// </summary>
    public interface IDahuaApiService
    {
        Task<bool> OpenBarrierAsync(string serverUrl, string username, string password, string channelId, int barrierChannel);
        Task<bool> TestConnectionAsync(string serverUrl, string username, string password);
        Task<bool> SendParkingSpaceStatusAsync(string serverUrl, string username, string password, ParkingSpaceStatusDto status);
    }

    public class DahuaApiService : IDahuaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DahuaApiService> _logger;

        public DahuaApiService(HttpClient httpClient, ILogger<DahuaApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Send command to open barrier via DSS alarm output or access control
        /// DSS API: POST /api/device/control/alarmoutput
        /// </summary>
        public async Task<bool> OpenBarrierAsync(string serverUrl, string username, string password, string channelId, int barrierChannel)
        {
            try
            {
                var token = await AuthenticateAsync(serverUrl, username, password);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Dahua auth failed for barrier open on {ServerUrl}", serverUrl);
                    return false;
                }

                var requestUrl = $"{serverUrl.TrimEnd('/')}/api/device/control/alarmoutput";
                var payload = new
                {
                    channelId,
                    alarmChannel = barrierChannel,
                    action = "pulse",  // pulse to open, then auto-close
                    duration = 2       // 2 seconds pulse
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = content };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Barrier opened: ch={ChannelId}, relay={BarrierChannel}", channelId, barrierChannel);
                    return true;
                }

                _logger.LogWarning("Barrier open failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barrier open error: {ServerUrl}", serverUrl);
                return false;
            }
        }

        /// <summary>
        /// Test connection to DSS server
        /// </summary>
        public async Task<bool> TestConnectionAsync(string serverUrl, string username, string password)
        {
            try
            {
                var token = await AuthenticateAsync(serverUrl, username, password);
                return !string.IsNullOrEmpty(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dahua connection test failed: {ServerUrl}", serverUrl);
                return false;
            }
        }

        /// <summary>
        /// Send parking space occupancy status to DSS for external display update
        /// DSS API: POST /api/parking/space/status
        /// </summary>
        public async Task<bool> SendParkingSpaceStatusAsync(string serverUrl, string username, string password, ParkingSpaceStatusDto status)
        {
            try
            {
                var token = await AuthenticateAsync(serverUrl, username, password);
                if (string.IsNullOrEmpty(token)) return false;

                var requestUrl = $"{serverUrl.TrimEnd('/')}/api/parking/space/status";
                var json = JsonSerializer.Serialize(status);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = content };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parking space status update failed");
                return false;
            }
        }

        /// <summary>
        /// Authenticate with DSS API and get bearer token
        /// DSS API: POST /api/auth/login
        /// </summary>
        private async Task<string?> AuthenticateAsync(string serverUrl, string username, string password)
        {
            try
            {
                var authUrl = $"{serverUrl.TrimEnd('/')}/api/auth/login";
                var payload = new { username, password };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(authUrl, content);
                if (!response.IsSuccessStatusCode) return null;

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                // Try common token field names
                if (doc.RootElement.TryGetProperty("accessToken", out var at))
                    return at.GetString();
                if (doc.RootElement.TryGetProperty("token", out var t))
                    return t.GetString();
                if (doc.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("accessToken", out var dat))
                    return dat.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
