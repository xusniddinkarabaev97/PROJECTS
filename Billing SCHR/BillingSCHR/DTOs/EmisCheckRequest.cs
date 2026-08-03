using System.Text.Json.Serialization;

namespace ODULink.DTOs
{
    /// <summary>
    /// Запрос в EMIS: проверка задолженности пациента перед приёмом.
    /// EMIS должен вернуть ФИО, сумму и invoiceId.
    /// </summary>
    public class EmisCheckRequest
    {
        /// <summary>Уникальный UUID запроса (идемпотентность).</summary>
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>ID пациента в EMIS (из QR-кода).</summary>
        [JsonPropertyName("patientId")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>Код услуги (опционально, если известен из QR).</summary>
        [JsonPropertyName("serviceCode")]
        public string? ServiceCode { get; set; }
    }

    /// <summary>
    /// Ответ EMIS на проверку задолженности.
    /// </summary>
    public class EmisCheckResponse
    {
        /// <summary>UUID запроса (echo).</summary>
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = string.Empty;

        /// <summary>FOUND | NOT_FOUND | ALREADY_PAID</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Сообщение об ошибке (если статус не FOUND).</summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>Данные пациента (если FOUND).</summary>
        [JsonPropertyName("patient")]
        public EmisPatientInfo? Patient { get; set; }

        /// <summary>Платёжные данные (если FOUND).</summary>
        [JsonPropertyName("billing")]
        public EmisBillingInfo? Billing { get; set; }
    }

    /// <summary>
    /// Информация о пациенте из EMIS.
    /// </summary>
    public class EmisPatientInfo
    {
        /// <summary>ID пациента в EMIS.</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }

        /// <summary>Воинское звание (майор, полковник...).</summary>
        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        /// <summary>ФИО одной строкой.</summary>
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    }

    /// <summary>
    /// Платёжная информация из EMIS.
    /// </summary>
    public class EmisBillingInfo
    {
        /// <summary>ID счёта в EMIS.</summary>
        [JsonPropertyName("invoiceId")]
        public string InvoiceId { get; set; } = string.Empty;

        /// <summary>Сумма к оплате (decimal, например 1500.00).</summary>
        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        /// <summary>Валюта (UZS).</summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "UZS";

        /// <summary>Назначение платежа.</summary>
        [JsonPropertyName("purpose")]
        public string? Purpose { get; set; }
    }
}
