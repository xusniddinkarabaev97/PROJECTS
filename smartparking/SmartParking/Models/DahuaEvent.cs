using System.ComponentModel.DataAnnotations;

namespace SmartParking.Models
{
    /// <summary>
    /// Incoming ANPR event received from Dahua DSS via webhook
    /// </summary>
    public class DahuaEvent
    {
        public int Id { get; set; }

        /// <summary>DSS event ID</summary>
        [MaxLength(100)]
        public string EventId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string EventType { get; set; } = "VehicleDetection"; // VehicleDetection, ANPR, AccessControl

        /// <summary>entry, exit</summary>
        [MaxLength(20)]
        public string Direction { get; set; } = "entry";

        /// <summary>License plate number</summary>
        [MaxLength(30)]
        public string PlateNumber { get; set; } = string.Empty;

        /// <summary>Country code from plate</summary>
        [MaxLength(10)]
        public string? PlateCountry { get; set; }

        /// <summary>Confidence 0-100</summary>
        public int? Confidence { get; set; }

        /// <summary>Snapshot image URL on DSS</summary>
        [MaxLength(500)]
        public string? SnapshotUrl { get; set; }

        /// <summary>Camera/channel ID that captured the event</summary>
        [MaxLength(100)]
        public string ChannelId { get; set; } = string.Empty;

        /// <summary>Channel name</summary>
        [MaxLength(200)]
        public string? ChannelName { get; set; }

        public DateTime EventTime { get; set; } = DateTime.UtcNow;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Raw JSON payload from DSS</summary>
        public string? RawPayload { get; set; }

        /// <summary>Processing status: pending, processed, failed, ignored</summary>
        [MaxLength(20)]
        public string ProcessStatus { get; set; } = "pending";

        public int? DahuaDeviceId { get; set; }
        public DahuaDevice? DahuaDevice { get; set; }

        public int? ParkingSessionId { get; set; }
        public ParkingSession? ParkingSession { get; set; }
    }
}
