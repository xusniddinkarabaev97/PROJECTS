using System.ComponentModel.DataAnnotations;

namespace ODULink.Models
{
    /// <summary>
    /// Запись о платёжной транзакции для идемпотентности Click
    /// </summary>
    public class ClickPayment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Уникальный ID транзакции Click</summary>
        public long ClickTransId { get; set; }

        /// <summary>Наш внутренний ID транзакции</summary>
        public string MerchantTransId { get; set; } = string.Empty;

        /// <summary>ID счета из EMIS (invoiceId)</summary>
        public string EmisInvoiceId { get; set; } = string.Empty;

        /// <summary>ID пациента из EMIS</summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>Сумма в тиынах/сумах</summary>
        public decimal Amount { get; set; }

        /// <summary>Статус: PREPARE, COMPLETE, FAILED, CANCELLED</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "PREPARE";

        /// <summary>Номер чека из EMIS</summary>
        public string? EmisReceiptNumber { get; set; }

        /// <summary>PAN-маска карты</summary>
        public string? PanMask { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
