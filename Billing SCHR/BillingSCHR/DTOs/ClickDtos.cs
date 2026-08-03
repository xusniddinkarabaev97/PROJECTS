using System.Text.Json.Serialization;

namespace ODULink.DTOs
{
    /// <summary>
    /// Click Prepare Request — запрос от Click на валидацию перед оплатой.
    /// Спецификация: https://docs.click.uz/ SHOP API
    /// </summary>
    public class ClickPrepareRequest
    {
        /// <summary>Уникальный ID транзакции Click (int64).</summary>
        [JsonPropertyName("click_trans_id")]
        public long ClickTransId { get; set; }

        /// <summary>ID услуги Click.</summary>
        [JsonPropertyName("service_id")]
        public int ServiceId { get; set; }

        /// <summary>ID транзакции мерчанта.</summary>
        [JsonPropertyName("merchant_trans_id")]
        public string MerchantTransId { get; set; } = string.Empty;

        /// <summary>Сумма в тиынах (копейках) — int.</summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>0 = prepare, 1 = complete.</summary>
        [JsonPropertyName("action")]
        public int Action { get; set; }

        /// <summary>Время подписи (формат: "YYYY-MM-DD HH:mm:ss").</summary>
        [JsonPropertyName("sign_time")]
        public string SignTime { get; set; } = string.Empty;

        /// <summary>MD5-подпись.</summary>
        [JsonPropertyName("sign_string")]
        public string SignString { get; set; } = string.Empty;

        /// <summary>Код ошибки (0 = нет ошибки).</summary>
        [JsonPropertyName("error")]
        public int Error { get; set; }

        /// <summary>Описание ошибки.</summary>
        [JsonPropertyName("error_note")]
        public string? ErrorNote { get; set; }

        /// <summary>Доп. параметр 1: patient_id из QR-кода.</summary>
        [JsonPropertyName("param1")]
        public string? PatientId { get; set; }

        /// <summary>Доп. параметр 2.</summary>
        [JsonPropertyName("param2")]
        public string? Param2 { get; set; }

        /// <summary>Доп. параметр 3.</summary>
        [JsonPropertyName("param3")]
        public string? Param3 { get; set; }
    }

    /// <summary>
    /// Click Prepare Response — ответ Биллинга на Prepare.
    /// merchant_prepare_id и merchant_confirm_id — int (спецификация Click).
    /// </summary>
    public class ClickPrepareResponse
    {
        [JsonPropertyName("click_trans_id")]
        public long ClickTransId { get; set; }

        [JsonPropertyName("merchant_trans_id")]
        public string MerchantTransId { get; set; } = string.Empty;

        /// <summary>Внутренний ID транзакции (int — требование Click).</summary>
        [JsonPropertyName("merchant_prepare_id")]
        public int MerchantPrepareId { get; set; }

        /// <summary>ID для подтверждения (int — требование Click).</summary>
        [JsonPropertyName("merchant_confirm_id")]
        public int MerchantConfirmId { get; set; }

        /// <summary>0 = успех, отрицательное = ошибка.</summary>
        [JsonPropertyName("error")]
        public int Error { get; set; }

        /// <summary>Описание ошибки (если error != 0).</summary>
        [JsonPropertyName("error_note")]
        public string? ErrorNote { get; set; }
    }

    /// <summary>
    /// Click Complete Request — запрос от Click на завершение платежа.
    /// </summary>
    public class ClickCompleteRequest
    {
        [JsonPropertyName("click_trans_id")]
        public long ClickTransId { get; set; }

        [JsonPropertyName("service_id")]
        public int ServiceId { get; set; }

        [JsonPropertyName("merchant_trans_id")]
        public string MerchantTransId { get; set; } = string.Empty;

        /// <summary>ID, выданный на шаге Prepare (int).</summary>
        [JsonPropertyName("merchant_prepare_id")]
        public int MerchantPrepareId { get; set; }

        /// <summary>ID подтверждения с Prepare (int).</summary>
        [JsonPropertyName("merchant_confirm_id")]
        public int MerchantConfirmId { get; set; }

        /// <summary>Сумма списания в тиынах.</summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>1 = complete.</summary>
        [JsonPropertyName("action")]
        public int Action { get; set; }

        [JsonPropertyName("sign_time")]
        public string SignTime { get; set; } = string.Empty;

        [JsonPropertyName("sign_string")]
        public string SignString { get; set; } = string.Empty;

        /// <summary>0 = платёж успешен, -1 и др. = отмена/ошибка.</summary>
        [JsonPropertyName("error")]
        public int Error { get; set; }

        [JsonPropertyName("error_note")]
        public string? ErrorNote { get; set; }
    }

    /// <summary>
    /// Click Complete Response — ответ Биллинга на Complete.
    /// </summary>
    public class ClickCompleteResponse
    {
        [JsonPropertyName("click_trans_id")]
        public long ClickTransId { get; set; }

        [JsonPropertyName("merchant_trans_id")]
        public string MerchantTransId { get; set; } = string.Empty;

        /// <summary>ID подтверждения (int).</summary>
        [JsonPropertyName("merchant_confirm_id")]
        public int MerchantConfirmId { get; set; }

        /// <summary>0 = успех, отрицательное = ошибка.</summary>
        [JsonPropertyName("error")]
        public int Error { get; set; }

        [JsonPropertyName("error_note")]
        public string? ErrorNote { get; set; }
    }
}
