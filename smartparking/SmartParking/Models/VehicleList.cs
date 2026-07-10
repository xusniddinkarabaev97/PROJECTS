using System.ComponentModel.DataAnnotations;

namespace SmartParking.Models
{
    /// <summary>
    /// Managed vehicle list — white/black lists, VIP, employees
    /// Maps to DSS "Personnel and Vehicle Management" / "Vehicle Arming List"
    /// </summary>
    public class VehicleList
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string PlateNumber { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? PlateCountry { get; set; }

        [MaxLength(200)]
        public string? OwnerName { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        /// <summary>regular, employee, vip, blocked</summary>
        [Required]
        [MaxLength(30)]
        public string Category { get; set; } = "regular";

        /// <summary>Optional: subscription/pass validity</summary>
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsEnabled { get; set; } = true;

        public int? CompanyId { get; set; }
        public Company? Company { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
