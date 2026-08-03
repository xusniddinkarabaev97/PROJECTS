using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ODULink.Enums;

namespace ODULink.Models
{
    /// <summary>
    /// Транзакция — оплата медицинских услуг
    /// </summary>
    public class Transaction
    {
        public int Id { get; set; }

        /// <summary>Отделение, оказавшее услугу</summary>
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        /// <summary>Диагноз (код МКБ-10 или описание)</summary>
        [MaxLength(500)]
        public string? Diagnosis { get; set; }

        /// <summary>Назначенное лечение / процедура</summary>
        [MaxLength(500)]
        public string? TreatmentDescription { get; set; }

        /// <summary>Лечащий врач</summary>
        [MaxLength(150)]
        public string? DoctorName { get; set; }

        [Required]
        public decimal TotalSum { get; set; }

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.New;

        /// <summary>Способ оплаты: cash, card, click, payme, insurance, budget</summary>
        public string? PaymentMethod { get; set; }

        public DateTime FilledAt { get; set; } = DateTime.UtcNow;

        /// <summary>Тип: medical, pharmacy, laboratory, procedure</summary>
        [Required]
        public string Status { get; set; } = "medical";

        public int? CompanyId { get; set; }
    }
}
