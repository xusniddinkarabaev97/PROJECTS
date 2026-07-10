using System.ComponentModel.DataAnnotations;

namespace SmartParking.Models
{
    /// <summary>
    /// Immutable audit log for security monitoring (PCI DSS requirement)
    /// Logs all critical operations: API calls, barrier commands, payment events, login attempts
    /// Entries are hash-chained for integrity verification
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }

        /// <summary>Event category: api_call, barrier_command, payment, auth, security, system</summary>
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>Action description</summary>
        [Required]
        [MaxLength(500)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Affected entity (plate number, transaction ID, user email)</summary>
        [MaxLength(200)]
        public string? EntityId { get; set; }

        /// <summary>Source IP address</summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>Authenticated user or service</summary>
        [MaxLength(200)]
        public string? Actor { get; set; }

        /// <summary>Additional details (JSON)</summary>
        public string? Details { get; set; }

        /// <summary>Success or failure</summary>
        [MaxLength(20)]
        public string Outcome { get; set; } = "success";

        /// <summary>SHA-256 hash of this entry + previous entry's hash (integrity chain)</summary>
        [MaxLength(64)]
        public string? IntegrityHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
