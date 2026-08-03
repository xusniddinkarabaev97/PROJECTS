using ODULink.DTOs;
using ODULink.Services;
using Microsoft.AspNetCore.Mvc;

namespace ODULink.Controllers
{
    /// <summary>
    /// Интеграция с платёжной системой Click.
    /// Биллинг выступает брокером между Click и EMIS: валидирует пациента,
    /// получает сумму к оплате из EMIS, подтверждает платёж.
    /// </summary>
    /// <remarks>
    /// **Бизнес-процесс (Happy Path):**
    /// 1. Click → `/prepare`: валидация пациента и суммы через EMIS
    /// 2. Click показывает окно оплаты, списывает средства
    /// 3. Click → `/complete`: подтверждение оплаты, уведомление EMIS
    ///
    /// **Коды ошибок Click:**
    /// - `0` — успех
    /// - `-2` — неверная подпись / сумма не совпадает
    /// - `-4` — услуга уже оплачена (из EMIS: ALREADY_PAID)
    /// - `-5` — пациент/транзакция не найден
    /// - `-9` — EMIS недоступен / внутренняя ошибка
    /// </remarks>
    [Route("api/click")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "integration")]
    public class ClickController : ControllerBase
    {
        private readonly IClickService _clickService;
        private readonly ILogger<ClickController> _logger;

        public ClickController(IClickService clickService, ILogger<ClickController> logger)
        {
            _clickService = clickService;
            _logger = logger;
        }

        /// <summary>
        /// Шаг 1: Prepare — валидация пациента и сверка суммы.
        /// </summary>
        /// <remarks>
        /// Click присылает `patient_id` (из QR-кода) в поле `param1`.
        /// Биллинг делает запрос в EMIS (`POST /api/v1/billing/check`),
        /// получает ФИО пациента и сумму к оплате (`invoiceId`).
        ///
        /// **Пример запроса (Click → Биллинг):**
        /// ```json
        /// {
        ///   "click_trans_id": 20240520001,
        ///   "service_id": 2005,
        ///   "merchant_trans_id": "MED-12345",
        ///   "amount": 1500.00,
        ///   "action": 0,
        ///   "sign_time": "2024-05-20T10:30:00Z",
        ///   "sign_string": "a1b2c3d4e5f6...",
        ///   "param1": "987654321"
        /// }
        /// ```
        ///
        /// **Пример ответа (успех):**
        /// ```json
        /// {
        ///   "click_trans_id": 20240520001,
        ///   "merchant_trans_id": "MED-12345",
        ///   "merchant_prepare_id": "abc123def456",
        ///   "merchant_confirm_id": "INV-2024-001",
        ///   "error": 0,
        ///   "error_note": "Success"
        /// }
        /// ```
        ///
        /// **Пример ответа (пациент не найден):**
        /// ```json
        /// {
        ///   "click_trans_id": 20240520001,
        ///   "merchant_trans_id": "MED-12345",
        ///   "error": -5,
        ///   "error_note": "Patient not found"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Параметры из Click (включая patient_id в param1).</param>
        /// <returns>merchant_prepare_id и merchant_confirm_id (invoiceId из EMIS).</returns>
        /// <response code="200">Результат валидации (error=0 — успех).</response>
        [HttpPost("prepare")]
        [ProducesResponseType(typeof(ClickPrepareResponse), 200)]
        public async Task<IActionResult> Prepare([FromBody] ClickPrepareRequest request)
        {
            _logger.LogInformation("Click Prepare: clickTransId={ClickTransId}, patientId={PatientId}, amount={Amount}",
                request.ClickTransId, request.PatientId, request.Amount);

            var response = await _clickService.PrepareAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Шаг 2: Complete — подтверждение оплаты и уведомление EMIS.
        /// </summary>
        /// <remarks>
        /// Если `error` = 0 — платёж успешен. Биллинг отправляет подтверждение в EMIS
        /// (`POST /api/v1/billing/pay`), передавая `paymentId` (Click), `amount`, `timestamp`.
        ///
        /// Если `error` ≠ 0 — платёж отменён/провален. Статус в БД меняется на `CANCELLED`.
        ///
        /// **Идемпотентность:** повторный Complete с тем же `click_trans_id`
        /// возвращает `error=0` без повторного вызова EMIS.
        ///
        /// **Пример запроса (успешная оплата):**
        /// ```json
        /// {
        ///   "click_trans_id": 20240520001,
        ///   "service_id": 2005,
        ///   "merchant_trans_id": "MED-12345",
        ///   "merchant_prepare_id": "abc123def456",
        ///   "merchant_confirm_id": "INV-2024-001",
        ///   "amount": 1500.00,
        ///   "action": 1,
        ///   "sign_time": "2024-05-20T10:31:00Z",
        ///   "sign_string": "f6e5d4c3b2a1...",
        ///   "error": 0
        /// }
        /// ```
        ///
        /// **Пример ответа:**
        /// ```json
        /// {
        ///   "click_trans_id": 20240520001,
        ///   "merchant_trans_id": "MED-12345",
        ///   "merchant_confirm_id": "INV-2024-001",
        ///   "error": 0,
        ///   "error_note": "Success"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Параметры завершения платежа от Click.</param>
        /// <returns>Статус завершения (error=0 — успех).</returns>
        /// <response code="200">Результат завершения платежа.</response>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(ClickCompleteResponse), 200)]
        public async Task<IActionResult> Complete([FromBody] ClickCompleteRequest request)
        {
            _logger.LogInformation("Click Complete: clickTransId={ClickTransId}, error={Error}, amount={Amount}",
                request.ClickTransId, request.Error, request.Amount);

            var response = await _clickService.CompleteAsync(request);
            return Ok(response);
        }
    }
}
