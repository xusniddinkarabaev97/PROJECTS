using System.Text.Json.Serialization;

namespace ODULink.DTOs
{
    /// <summary>
    /// Запрос в EMIS: подтверждение успешной оплаты.
    /// </summary>
    public class EmisPayRequest
    {
        /// <summary>Уникальный UUID запроса (идемпотентность).</summary>
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>ID счёта из EMIS (получен на шаге check).</summary>
        [JsonPropertyName("invoiceId")]
        public string InvoiceId { get; set; } = string.Empty;

        /// <summary>ID пациента.</summary>
        [JsonPropertyName("patientId")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>Детали платёжной транзакции.</summary>
        [JsonPropertyName("transaction")]
        public EmisPayTransactionInfo Transaction { get; set; } = new();
    }

    /// <summary>
    /// Данные платёжной транзакции, передаваемые в EMIS.
    /// </summary>
    public class EmisPayTransactionInfo
    {
        /// <summary>ID транзакции в Click (click_trans_id).</summary>
        [JsonPropertyName("paymentId")]
        public string PaymentId { get; set; } = string.Empty;

        /// <summary>Метод оплаты: CLICK.</summary>
        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = "CLICK";

        /// <summary>Сумма списания.</summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>Время списания (UTC).</summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Маскированный номер карты.</summary>
        [JsonPropertyName("panMask")]
        public string? PanMask { get; set; }
    }

    /// <summary>
    /// Ответ EMIS на подтверждение оплаты.
    /// </summary>
    public class EmisPayResponse
    {
        /// <summary>UUID запроса (echo).</summary>
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = string.Empty;

        /// <summary>SUCCESS | FAILED.</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Номер чека в EMIS.</summary>
        [JsonPropertyName("receiptNumber")]
        public string? ReceiptNumber { get; set; }

        /// <summary>Сообщение об ошибке (если FAILED).</summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
