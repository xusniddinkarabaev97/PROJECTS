using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ODULink.Models
{
    /// <summary>
    /// Пациент госпиталя (военнослужащий или гражданский)
    /// </summary>
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ExternalId { get; set; } = null!; // ID из внешней системы (Payme account.user_id и т.п.)

        [MaxLength(150)]
        public string? FullName { get; set; }

        /// <summary>Воинское звание (рядовой, сержант, лейтенант, etc.)</summary>
        [MaxLength(50)]
        public string? MilitaryRank { get; set; }

        /// <summary>Войсковая часть</summary>
        [MaxLength(50)]
        public string? MilitaryUnit { get; set; }

        /// <summary>Личный номер / ID военнослужащего</summary>
        [MaxLength(50)]
        public string? PersonalNumber { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        /// <summary>Дата рождения</summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>Группа крови</summary>
        [MaxLength(5)]
        public string? BloodType { get; set; }

        public bool IsVerified { get; set; } = false;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Source { get; set; } = "manual"; // manual, payme, click

        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, discharged, blocked

        [JsonIgnore]
        public List<Transaction> Transactions { get; set; } = new();
    }
}
