using System.ComponentModel.DataAnnotations;

namespace SmartParking.Models
{
    /// <summary>
    /// Dahua ANPR camera or access control device configuration
    /// </summary>
    public class DahuaDevice
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>DSS channel ID for this device</summary>
        [MaxLength(100)]
        public string ChannelId { get; set; } = string.Empty;

        /// <summary>Device IP or hostname</summary>
        [MaxLength(100)]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>DSS API base URL (e.g. https://dss-server:443)</summary>
        [MaxLength(300)]
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>Type: camera, access_controller, display</summary>
        [MaxLength(50)]
        public string DeviceType { get; set; } = "camera";

        /// <summary>Direction: entry, exit, both</summary>
        [MaxLength(20)]
        public string Direction { get; set; } = "entry";

        /// <summary>Relay/Alarm output channel for barrier control (1, 2, etc.)</summary>
        public int? BarrierChannel { get; set; }

        public int? StationId { get; set; }
        public Station? Station { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
