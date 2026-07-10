using SmartParking.Data;
using SmartParking.DTOs;
using SmartParking.Middleware;
using SmartParking.Models;
using SmartParking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SmartParking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DahuaIntegrationController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IParkingSessionService _parking;
        private readonly IDahuaApiService _dahua;
        private readonly IAuditService _audit;
        private readonly ISignatureService _signature;
        private readonly ILogger<DahuaIntegrationController> _logger;

        public DahuaIntegrationController(
            ApplicationDbContext ctx,
            IParkingSessionService parking,
            IDahuaApiService dahua,
            IAuditService audit,
            ISignatureService signature,
            ILogger<DahuaIntegrationController> logger)
        {
            _ctx = ctx;
            _parking = parking;
            _dahua = dahua;
            _audit = audit;
            _signature = signature;
            _logger = logger;
        }

        // ==================== WEBHOOK RECEIVER ====================

        /// <summary>
        /// Webhook endpoint for Dahua DSS Event Transferal.
        /// Receives ANPR/VehicleDetection events in real-time.
        /// Must be configured in DSS: System Integration → Event Transferal → Web Service
        /// </summary>
        [AllowAnonymous]
        [HttpPost("events")]
        public async Task<IActionResult> ReceiveEvent([FromBody] DahuaEventDto dto, [FromHeader(Name = "X-Webhook-Secret")] string? secret)
        {
            _logger.LogInformation("Dahua event received: type={EventType}, plate={Plate}, dir={Direction}",
                dto.EventType, dto.PlateNumber ?? dto.Vehicle?.PlateNumber, dto.Direction);

            // Find settings by device channel to determine company
            var device = await _ctx.DahuaDevices
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.ChannelId == dto.ChannelId);

            if (device == null)
            {
                _logger.LogWarning("Unknown channel: {ChannelId}", dto.ChannelId);
                return Ok(new { status = "ignored", reason = "unknown_channel" });
            }

            var companyId = device.CompanyId;

            // Optional: validate webhook secret
            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (!string.IsNullOrEmpty(settings?.WebhookSecret) && settings.WebhookSecret != secret)
            {
                _logger.LogWarning("Invalid webhook secret for channel {ChannelId}", dto.ChannelId);
                return Unauthorized(new { error = "invalid_secret" });
            }

            var direction = dto.Direction?.ToLower() ?? "entry";
            var plate = dto.PlateNumber ?? dto.Vehicle?.PlateNumber ?? dto.Detail?.PlateNumber ?? "UNKNOWN";

            await _audit.LogAsync("dahua_event", $"ANPR {direction}: {plate}", plate,
                $"dss_channel_{dto.ChannelId}", details: System.Text.Json.JsonSerializer.Serialize(dto));

            if (direction == "exit" || direction == "leave")
            {
                var session = await _parking.ProcessExitEventAsync(dto, companyId);
                return Ok(new
                {
                    status = session != null ? "completed" : "no_session",
                    sessionId = session?.Id,
                    parkingFee = session?.ParkingFee,
                    transactionId = session?.TransactionId,
                    barrierOpened = session?.ExitBarrierOpened ?? false
                });
            }
            else
            {
                var session = await _parking.ProcessEntryEventAsync(dto, companyId);
                return Ok(new
                {
                    status = "active",
                    sessionId = session.Id,
                    plate = session.PlateNumber,
                    category = session.VehicleCategory,
                    barrierOpened = session.EntryBarrierOpened
                });
            }
        }

        // ==================== BARRIER CONTROL ====================

        /// <summary>
        /// Manual barrier open command (for operator)
        /// </summary>
        [Authorize]
        [HttpPost("barrier/open")]
        public async Task<IActionResult> OpenBarrier([FromBody] BarrierCommandDto cmd)
        {
            await _audit.LogAsync("barrier_command", $"Manual barrier open: ch={cmd.ChannelId}, relay={cmd.BarrierChannel}",
                cmd.ChannelId, actor: User.FindFirst("companyId")?.Value);

            var device = await _ctx.DahuaDevices.FirstOrDefaultAsync(d => d.ChannelId == cmd.ChannelId);
            if (device == null) return NotFound("Device not found");

            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == device.CompanyId);
            if (settings == null) return BadRequest("Dahua settings not configured");

            var result = await _dahua.OpenBarrierAsync(
                settings.ServerUrl, settings.Username, settings.Password,
                cmd.ChannelId, cmd.BarrierChannel);

            return result ? Ok(new { status = "opened" }) : StatusCode(502, new { error = "barrier_command_failed" });
        }

        // ==================== CONFIGURATION ====================

        /// <summary>
        /// Get Dahua integration settings for the company
        /// </summary>
        [Authorize]
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var companyId = GetCompanyId();
            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (settings == null) return NotFound("Not configured");
            return Ok(settings);
        }

        /// <summary>
        /// Save Dahua integration settings
        /// </summary>
        [Authorize]
        [HttpPut("settings")]
        public async Task<IActionResult> SaveSettings([FromBody] DahuaSettings updated)
        {
            var companyId = GetCompanyId();
            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (settings == null)
            {
                updated.CompanyId = companyId;
                updated.CreatedAt = DateTime.UtcNow;
                updated.UpdatedAt = DateTime.UtcNow;
                _ctx.DahuaSettings.Add(updated);
            }
            else
            {
                settings.ServerUrl = updated.ServerUrl;
                settings.Username = updated.Username;
                settings.Password = updated.Password;
                settings.WebhookSecret = updated.WebhookSecret;
                settings.HourlyRate = updated.HourlyRate;
                settings.GracePeriodMinutes = updated.GracePeriodMinutes;
                settings.MaxDailyRate = updated.MaxDailyRate;
                settings.AutoOpenForWhitelist = updated.AutoOpenForWhitelist;
                settings.BarrierControlEnabled = updated.BarrierControlEnabled;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _ctx.SaveChangesAsync();
            return Ok(new { status = "saved" });
        }

        /// <summary>
        /// Test connection to DSS server
        /// </summary>
        [Authorize]
        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] DahuaSettings testSettings)
        {
            var result = await _dahua.TestConnectionAsync(
                testSettings.ServerUrl, testSettings.Username, testSettings.Password);
            return result ? Ok(new { status = "connected" }) : BadRequest(new { error = "connection_failed" });
        }

        // ==================== DEVICES ====================

        [Authorize]
        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices()
        {
            var companyId = GetCompanyId();
            var devices = await _ctx.DahuaDevices
                .Where(d => d.CompanyId == companyId)
                .Include(d => d.Station)
                .ToListAsync();
            return Ok(devices);
        }

        [Authorize]
        [HttpPost("devices")]
        public async Task<IActionResult> CreateDevice([FromBody] DahuaDevice device)
        {
            device.CompanyId = GetCompanyId();
            device.CreatedAt = DateTime.UtcNow;
            _ctx.DahuaDevices.Add(device);
            await _ctx.SaveChangesAsync();
            return Ok(device);
        }

        [Authorize]
        [HttpPut("devices/{id}")]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] DahuaDevice updated)
        {
            var device = await _ctx.DahuaDevices.FindAsync(id);
            if (device == null) return NotFound();

            device.Name = updated.Name;
            device.ChannelId = updated.ChannelId;
            device.IpAddress = updated.IpAddress;
            device.ApiBaseUrl = updated.ApiBaseUrl;
            device.DeviceType = updated.DeviceType;
            device.Direction = updated.Direction;
            device.BarrierChannel = updated.BarrierChannel;
            device.StationId = updated.StationId;
            device.IsEnabled = updated.IsEnabled;

            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("devices/{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _ctx.DahuaDevices.FindAsync(id);
            if (device == null) return NotFound();
            _ctx.DahuaDevices.Remove(device);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // ==================== PARKING SESSIONS ====================

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] string? status)
        {
            var query = _ctx.ParkingSessions
                .Include(s => s.Device)
                .Include(s => s.Station)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            var sessions = await query.OrderByDescending(s => s.CreatedAt).Take(200).ToListAsync();
            return Ok(sessions);
        }

        [Authorize]
        [HttpGet("sessions/{id}")]
        public async Task<IActionResult> GetSession(int id)
        {
            var session = await _ctx.ParkingSessions
                .Include(s => s.EntryEvent)
                .Include(s => s.ExitEvent)
                .Include(s => s.Device)
                .Include(s => s.Station)
                .Include(s => s.Transaction)
                .FirstOrDefaultAsync(s => s.Id == id);

            return session != null ? Ok(session) : NotFound();
        }

        // ==================== EVENTS LOG ====================

        [Authorize]
        [HttpGet("events")]
        public async Task<IActionResult> GetEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var events = await _ctx.DahuaEvents
                .OrderByDescending(e => e.ReceivedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _ctx.DahuaEvents.CountAsync();
            return Ok(new { data = events, total, page, pageSize });
        }

        // ==================== VEHICLE LISTS ====================

        [Authorize]
        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles([FromQuery] string? category)
        {
            var query = _ctx.VehicleLists.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(v => v.Category == category);

            return Ok(await query.OrderBy(v => v.PlateNumber).ToListAsync());
        }

        [Authorize]
        [HttpPost("vehicles")]
        public async Task<IActionResult> AddVehicle([FromBody] VehicleList vehicle)
        {
            vehicle.CreatedAt = DateTime.UtcNow;
            vehicle.UpdatedAt = DateTime.UtcNow;
            _ctx.VehicleLists.Add(vehicle);
            await _ctx.SaveChangesAsync();
            return Ok(vehicle);
        }

        [Authorize]
        [HttpPut("vehicles/{id}")]
        public async Task<IActionResult> UpdateVehicle(int id, [FromBody] VehicleList updated)
        {
            var v = await _ctx.VehicleLists.FindAsync(id);
            if (v == null) return NotFound();

            v.PlateNumber = updated.PlateNumber;
            v.PlateCountry = updated.PlateCountry;
            v.OwnerName = updated.OwnerName;
            v.Phone = updated.Phone;
            v.Category = updated.Category;
            v.ValidFrom = updated.ValidFrom;
            v.ValidUntil = updated.ValidUntil;
            v.Notes = updated.Notes;
            v.IsEnabled = updated.IsEnabled;
            v.UpdatedAt = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("vehicles/{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var v = await _ctx.VehicleLists.FindAsync(id);
            if (v == null) return NotFound();
            _ctx.VehicleLists.Remove(v);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // ==================== PARKING SPACE STATUS ====================

        [Authorize]
        [HttpPost("space-status")]
        public async Task<IActionResult> UpdateSpaceStatus([FromBody] ParkingSpaceStatusDto dto)
        {
            var companyId = GetCompanyId();
            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (settings == null) return BadRequest("Settings not configured");

            var result = await _dahua.SendParkingSpaceStatusAsync(
                settings.ServerUrl, settings.Username, settings.Password, dto);

            return result ? Ok(new { status = "sent" }) : StatusCode(502, new { error = "dss_update_failed" });
        }

        // ==================== SECURITY MANAGEMENT ====================

        /// <summary>
        /// Generate API key for service-to-service authentication
        /// </summary>
        [Authorize]
        [HttpPost("security/generate-api-key")]
        public IActionResult GenerateApiKey()
        {
            var key = _signature.GenerateApiKey();
            return Ok(new { apiKey = key, generatedAt = DateTime.UtcNow });
        }

        /// <summary>
        /// Get allowed IPs for Dahua webhook
        /// </summary>
        [Authorize]
        [HttpGet("security/allowed-ips")]
        public async Task<IActionResult> GetAllowedIps()
        {
            var companyId = GetCompanyId();
            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            var ips = settings?.WebhookSecret?.StartsWith("allowlist:") == true
                ? settings.WebhookSecret.Replace("allowlist:", "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();
            return Ok(new { ips });
        }

        /// <summary>
        /// Update IP whitelist for Dahua webhook
        /// </summary>
        [Authorize]
        [HttpPut("security/allowed-ips")]
        public async Task<IActionResult> SetAllowedIps([FromBody] string[] ips)
        {
            var companyId = GetCompanyId();
            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (settings != null)
            {
                settings.WebhookSecret = "allowlist:" + string.Join(",", ips);
                settings.UpdatedAt = DateTime.UtcNow;
            }
            IpWhitelistMiddleware.SetAllowedIps(ips);
            await _ctx.SaveChangesAsync();

            await _audit.LogAsync("security", "IP whitelist updated", actor: User.FindFirst("companyId")?.Value,
                details: string.Join(", ", ips));

            return Ok(new { status = "updated", count = ips.Length });
        }

        /// <summary>
        /// Get audit log (last 200 entries)
        /// </summary>
        [Authorize]
        [HttpGet("security/audit-log")]
        public async Task<IActionResult> GetAuditLog([FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var query = _ctx.AuditLogs.AsQueryable();
            if (!string.IsNullOrEmpty(category))
                query = query.Where(l => l.Category == category);

            var total = await query.CountAsync();
            var logs = await query.OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new { data = logs, total, page, pageSize });
        }

        // ==================== HELPER ====================

        private int GetCompanyId()
        {
            var claim = User.FindFirst("companyId")?.Value;
            return claim != null ? int.Parse(claim) : 1;
        }
    }
}
