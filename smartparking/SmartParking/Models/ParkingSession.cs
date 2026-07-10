using System.ComponentModel.DataAnnotations;

namespace SmartParking.Models
{
    /// <summary>
    /// Active parking session created from ANPR entry event
    /// </summary>
    public class ParkingSession
    {
        public int Id { get; set; }

        /// <summary>License plate</summary>
        [Required]
        [MaxLength(30)]
        public string PlateNumber { get; set; } = string.Empty;

        /// <summary>Entry event reference</summary>
        public int? EntryEventId { get; set; }
        public DahuaEvent? EntryEvent { get; set; }

        /// <summary>Exit event reference</summary>
        public int? ExitEventId { get; set; }
        public DahuaEvent? ExitEvent { get; set; }

        public DateTime EntryTime { get; set; } = DateTime.UtcNow;
        public DateTime? ExitTime { get; set; }

        /// <summary>Calculated parking duration</summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>Calculated fee</summary>
        public decimal? ParkingFee { get; set; }

        /// <summary>active, completed, cancelled, expired</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "active";

        /// <summary>Barrier opened at entry: true/false</summary>
        public bool EntryBarrierOpened { get; set; }

        /// <summary>Barrier opened at exit: true/false</summary>
        public bool ExitBarrierOpened { get; set; }

        /// <summary>Snapshot URL from entry</summary>
        [MaxLength(500)]
        public string? EntrySnapshotUrl { get; set; }

        /// <summary>Snapshot URL from exit</summary>
        [MaxLength(500)]
        public string? ExitSnapshotUrl { get; set; }

        public int? DeviceId { get; set; }
        public DahuaDevice? Device { get; set; }

        public int? StationId { get; set; }
        public Station? Station { get; set; }

        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        public int? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        /// <summary>Vehicle category: regular, employee, vip, blocked</summary>
        [MaxLength(30)]
        public string? VehicleCategory { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
