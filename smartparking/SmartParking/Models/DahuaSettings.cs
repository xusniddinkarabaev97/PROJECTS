using System.ComponentModel.DataAnnotations;

namespace SmartParking.Models
{
    /// <summary>
    /// Dahua DSS integration settings per company
    /// </summary>
    public class DahuaSettings
    {
        public int Id { get; set; }

        /// <summary>DSS server base URL (e.g. https://192.168.1.100:443)</summary>
        [MaxLength(300)]
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>DSS API username</summary>
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        /// <summary>DSS API password (encrypted in production)</summary>
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Webhook secret for validating incoming events</summary>
        [MaxLength(200)]
        public string? WebhookSecret { get; set; }

        /// <summary>Hourly parking rate (default)</summary>
        public decimal HourlyRate { get; set; } = 5000;

        /// <summary>Free exit time in minutes (grace period)</summary>
        public int GracePeriodMinutes { get; set; } = 15;

        /// <summary>Max daily rate cap, 0 = unlimited</summary>
        public decimal? MaxDailyRate { get; set; }

        /// <summary>Barrier auto-open for whitelisted vehicles</summary>
        public bool AutoOpenForWhitelist { get; set; } = true;

        /// <summary>Enable automatic barrier control</summary>
        public bool BarrierControlEnabled { get; set; } = true;

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
