using SmartParking.Data;
using SmartParking.DTOs;
using SmartParking.Enums;
using SmartParking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SmartParking.Services
{
    public interface IParkingSessionService
    {
        Task<ParkingSession> ProcessEntryEventAsync(DahuaEventDto dto, int companyId);
        Task<ParkingSession?> ProcessExitEventAsync(DahuaEventDto dto, int companyId);
        Task<decimal> CalculateParkingFeeAsync(ParkingSession session);
        Task<bool> TryOpenBarrierAsync(int sessionId, string direction);
    }

    public class ParkingSessionService : IParkingSessionService
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IDahuaApiService _dahua;
        private readonly ILogger<ParkingSessionService> _logger;

        public ParkingSessionService(ApplicationDbContext ctx, IDahuaApiService dahua, ILogger<ParkingSessionService> logger)
        {
            _ctx = ctx;
            _dahua = dahua;
            _logger = logger;
        }

        /// <summary>
        /// Process vehicle ENTRY event from Dahua ANPR.
        /// Creates parking session, checks vehicle lists, optionally auto-opens barrier.
        /// </summary>
        public async Task<ParkingSession> ProcessEntryEventAsync(DahuaEventDto dto, int companyId)
        {
            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            var device = await _ctx.DahuaDevices.FirstOrDefaultAsync(d => d.ChannelId == dto.ChannelId);

            var dhEvent = new DahuaEvent
            {
                EventId = dto.EventId ?? Guid.NewGuid().ToString(),
                EventType = dto.EventType ?? "VehicleDetection",
                Direction = "entry",
                PlateNumber = dto.PlateNumber ?? dto.Vehicle?.PlateNumber ?? dto.Detail?.PlateNumber ?? "UNKNOWN",
                PlateCountry = dto.PlateCountry ?? dto.Vehicle?.PlateCountry,
                Confidence = dto.Confidence ?? dto.Vehicle?.Confidence,
                SnapshotUrl = dto.SnapshotUrl ?? dto.Detail?.SnapshotUrl,
                ChannelId = dto.ChannelId ?? "unknown",
                ChannelName = dto.ChannelName,
                EventTime = dto.EventTime ?? DateTime.UtcNow,
                RawPayload = System.Text.Json.JsonSerializer.Serialize(dto),
                ProcessStatus = "processed",
                DahuaDeviceId = device?.Id
            };
            _ctx.DahuaEvents.Add(dhEvent);

            // Check vehicle category from managed lists
            var vehicleEntry = await _ctx.VehicleLists
                .FirstOrDefaultAsync(v => v.PlateNumber == dhEvent.PlateNumber && v.IsEnabled);
            var category = vehicleEntry?.Category ?? "regular";

            // Check for existing active session for this plate
            var existingSession = await _ctx.ParkingSessions
                .Where(s => s.PlateNumber == dhEvent.PlateNumber && s.Status == "active")
                .FirstOrDefaultAsync();
            if (existingSession != null)
            {
                _logger.LogWarning("Duplicate entry for plate {Plate}, ignoring", dhEvent.PlateNumber);
                dhEvent.ProcessStatus = "ignored";
                await _ctx.SaveChangesAsync();
                return existingSession;
            }

            // Find or create client
            var client = await _ctx.Clients.FirstOrDefaultAsync(c => c.ExternalId == dhEvent.PlateNumber);
            if (client == null)
            {
                client = new Client
                {
                    ExternalId = dhEvent.PlateNumber,
                    FullName = dhEvent.PlateNumber,
                    Source = "dahua_anpr",
                    Status = "active"
                };
                _ctx.Clients.Add(client);
                await _ctx.SaveChangesAsync();
            }

            var session = new ParkingSession
            {
                PlateNumber = dhEvent.PlateNumber,
                EntryEventId = dhEvent.Id,
                EntryTime = dhEvent.EventTime,
                EntrySnapshotUrl = dhEvent.SnapshotUrl,
                Status = "active",
                VehicleCategory = category,
                DeviceId = device?.Id,
                StationId = device?.StationId,
                ClientId = client.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _ctx.ParkingSessions.Add(session);
            await _ctx.SaveChangesAsync();

            dhEvent.ParkingSessionId = session.Id;

            // Auto-open barrier for entry?
            var shouldOpen = category != "blocked" &&
                             (settings?.AutoOpenForWhitelist != false || category != "blocked");

            if (shouldOpen && settings?.BarrierControlEnabled == true && device != null)
            {
                var opened = await TryOpenBarrierAsync(session.Id, "entry");
                session.EntryBarrierOpened = opened;
            }

            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Entry session #{SessionId} for {Plate} [{Category}]", session.Id, dhEvent.PlateNumber, category);

            return session;
        }

        /// <summary>
        /// Process vehicle EXIT event. Completes parking session, calculates fee.
        /// </summary>
        public async Task<ParkingSession?> ProcessExitEventAsync(DahuaEventDto dto, int companyId)
        {
            var plate = dto.PlateNumber ?? dto.Vehicle?.PlateNumber ?? dto.Detail?.PlateNumber ?? "UNKNOWN";
            var settings = await _ctx.DahuaSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            var device = await _ctx.DahuaDevices.FirstOrDefaultAsync(d => d.ChannelId == dto.ChannelId);

            var dhEvent = new DahuaEvent
            {
                EventId = dto.EventId ?? Guid.NewGuid().ToString(),
                EventType = dto.EventType ?? "VehicleDetection",
                Direction = "exit",
                PlateNumber = plate,
                PlateCountry = dto.PlateCountry,
                Confidence = dto.Confidence,
                SnapshotUrl = dto.SnapshotUrl ?? dto.Detail?.SnapshotUrl,
                ChannelId = dto.ChannelId ?? "unknown",
                ChannelName = dto.ChannelName,
                EventTime = dto.EventTime ?? DateTime.UtcNow,
                RawPayload = System.Text.Json.JsonSerializer.Serialize(dto),
                ProcessStatus = "processed",
                DahuaDeviceId = device?.Id
            };
            _ctx.DahuaEvents.Add(dhEvent);

            // Find active session
            var session = await _ctx.ParkingSessions
                .Where(s => s.PlateNumber == plate && s.Status == "active")
                .OrderByDescending(s => s.EntryTime)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                _logger.LogWarning("No active session for exit plate {Plate}", plate);
                dhEvent.ProcessStatus = "ignored";
                await _ctx.SaveChangesAsync();
                return null;
            }

            session.ExitEventId = dhEvent.Id;
            session.ExitTime = dhEvent.EventTime;
            session.ExitSnapshotUrl = dhEvent.SnapshotUrl;
            session.Duration = session.ExitTime.Value - session.EntryTime;

            // Calculate fee
            session.ParkingFee = await CalculateParkingFeeAsync(session);

            // Create transaction for payment
            var txn = new Transaction
            {
                ClientId = session.ClientId ?? 1,
                TotalSum = session.ParkingFee ?? 0,
                PaymentStatus = PaymentStatus.New,
                PaymentMethod = System.Text.Json.JsonSerializer.Serialize(new
                {
                    plate = session.PlateNumber,
                    entry = session.EntryTime.ToString("o"),
                    exit = session.ExitTime?.ToString("o"),
                    duration = session.Duration?.ToString(),
                    sessionId = session.Id
                }),
                Status = "parking",
                FilledAt = DateTime.UtcNow,
                StationId = session.StationId
            };
            _ctx.Transactions.Add(txn);
            await _ctx.SaveChangesAsync();

            session.TransactionId = txn.Id;

            // Auto-open barrier for exit if whitelisted or grace period
            var shouldOpen = session.VehicleCategory == "employee" || session.VehicleCategory == "vip";
            if (settings?.BarrierControlEnabled == true && shouldOpen && device != null)
            {
                var opened = await TryOpenBarrierAsync(session.Id, "exit");
                session.ExitBarrierOpened = opened;
            }

            dhEvent.ParkingSessionId = session.Id;
            session.Status = "completed";
            session.UpdatedAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            _logger.LogInformation("Exit session #{SessionId} for {Plate}, fee={Fee}, duration={Duration}",
                session.Id, plate, session.ParkingFee, session.Duration);

            return session;
        }

        /// <summary>
        /// Calculate parking fee based on duration and settings
        /// </summary>
        public async Task<decimal> CalculateParkingFeeAsync(ParkingSession session)
        {
            if (session.Duration == null) return 0;

            var companyId = session.Station?.CompanyId ?? 1;
            var settings = await _ctx.DahuaSettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            var totalMinutes = (decimal)session.Duration.Value.TotalMinutes;
            var rate = settings?.HourlyRate ?? 5000;
            var graceMinutes = settings?.GracePeriodMinutes ?? 15;

            // Free if under grace period
            if (totalMinutes <= graceMinutes) return 0;

            // Free for employees and VIPs
            if (session.VehicleCategory == "employee" || session.VehicleCategory == "vip") return 0;

            // Calculate: round up to next hour
            var hours = Math.Ceiling(totalMinutes / 60);
            var fee = hours * rate;

            // Apply daily cap if configured
            if (settings?.MaxDailyRate > 0 && fee > settings.MaxDailyRate)
                fee = settings.MaxDailyRate.Value;

            return fee;
        }

        /// <summary>
        /// Send barrier open command to Dahua DSS
        /// </summary>
        public async Task<bool> TryOpenBarrierAsync(int sessionId, string direction)
        {
            var session = await _ctx.ParkingSessions
                .Include(s => s.Device)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session?.Device == null) return false;

            var settings = await _ctx.DahuaSettings
                .FirstOrDefaultAsync(s => s.CompanyId == session.Device.CompanyId);

            if (settings == null || !settings.BarrierControlEnabled) return false;

            return await _dahua.OpenBarrierAsync(
                settings.ServerUrl,
                settings.Username,
                settings.Password,
                session.Device.ChannelId,
                session.Device.BarrierChannel ?? 1
            );
        }
    }
}
