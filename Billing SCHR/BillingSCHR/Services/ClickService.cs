using System.Security.Cryptography;
using System.Text;
using ODULink.Data;
using ODULink.DTOs;
using ODULink.Models;
using Microsoft.EntityFrameworkCore;

namespace ODULink.Services
{
    public class ClickService : IClickService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmisService _emisService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClickService> _logger;

        public ClickService(
            ApplicationDbContext context,
            IEmisService emisService,
            IConfiguration configuration,
            ILogger<ClickService> logger)
        {
            _context = context;
            _emisService = emisService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Шаг 1: Prepare — Click запрашивает валидацию пациента и сумму.
        /// Спецификация: https://docs.click.uz/ SHOP API
        /// </summary>
        public async Task<ClickPrepareResponse> PrepareAsync(ClickPrepareRequest request)
        {
            // 1. Проверяем подпись
            if (!VerifySignature(request))
            {
                return Error((int)request.ClickTransId, request.MerchantTransId, -1, "Sign check failed");
            }

            // 2. Проверяем action
            if (request.Action != 0)
            {
                return Error((int)request.ClickTransId, request.MerchantTransId, -3, "Action not found");
            }

            // 3. Идемпотентность: проверяем, нет ли уже такого платежа
            var existing = await _context.ClickPayments
                .FirstOrDefaultAsync(p => p.ClickTransId == request.ClickTransId);

            if (existing != null)
            {
                if (existing.Status == "COMPLETE")
                    return new ClickPrepareResponse
                    {
                        ClickTransId = request.ClickTransId,
                        MerchantTransId = request.MerchantTransId,
                        MerchantPrepareId = existing.Id,
                        MerchantConfirmId = existing.Id,
                        Error = -4,
                        ErrorNote = "Already paid"
                    };
            }

            // 4. Запрашиваем EMIS: проверка задолженности
            var patientId = request.PatientId ?? request.MerchantTransId;
            if (string.IsNullOrEmpty(patientId))
            {
                return Error((int)request.ClickTransId, request.MerchantTransId, -5, "User does not exist");
            }

            EmisCheckResponse emisResponse;
            try
            {
                emisResponse = await _emisService.CheckPatientAsync(new EmisCheckRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    PatientId = patientId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EMIS check failed for patient {PatientId}", patientId);
                return Error((int)request.ClickTransId, request.MerchantTransId, -9, "EMIS service unavailable");
            }

            // 5. Обрабатываем ответ EMIS
            if (emisResponse.Status == "NOT_FOUND")
                return Error((int)request.ClickTransId, request.MerchantTransId, -5, emisResponse.Message ?? "User does not exist");

            if (emisResponse.Status == "ALREADY_PAID")
                return Error((int)request.ClickTransId, request.MerchantTransId, -4, emisResponse.Message ?? "Already paid");

            if (emisResponse.Status != "FOUND" || emisResponse.Billing == null)
                return Error((int)request.ClickTransId, request.MerchantTransId, -9, "Unknown error from EMIS");

            // 6. Сверяем сумму (если Click прислал сумму)
            var emisAmount = emisResponse.Billing.TotalAmount;
            if (request.Amount > 0 && request.Amount != emisAmount)
            {
                return Error((int)request.ClickTransId, request.MerchantTransId, -2, $"Incorrect parameter amount. Expected: {emisAmount}");
            }

            // 7. Сохраняем платёж в БД
            var payment = new ClickPayment
            {
                ClickTransId = request.ClickTransId,
                MerchantTransId = request.MerchantTransId,
                EmisInvoiceId = emisResponse.Billing.InvoiceId,
                PatientId = patientId,
                Amount = emisAmount,
                Status = "PREPARE",
                CreatedAt = DateTime.UtcNow
            };

            _context.ClickPayments.Add(payment);
            await _context.SaveChangesAsync();

            // 8. merchant_prepare_id и merchant_confirm_id = Id записи (int — требование Click)
            _logger.LogInformation(
                "Click Prepare OK: clickTransId={ClickTransId}, paymentId={PaymentId}, patient={Patient}, amount={Amount}",
                request.ClickTransId, payment.Id, emisResponse.Patient?.FullName, emisAmount);

            return new ClickPrepareResponse
            {
                ClickTransId = request.ClickTransId,
                MerchantTransId = request.MerchantTransId,
                MerchantPrepareId = payment.Id,
                MerchantConfirmId = payment.Id,
                Error = 0,
                ErrorNote = "Success"
            };
        }

        /// <summary>
        /// Шаг 2: Complete — Click сообщает о результате оплаты.
        /// </summary>
        public async Task<ClickCompleteResponse> CompleteAsync(ClickCompleteRequest request)
        {
            // 1. Проверяем подпись
            if (!VerifySignature(request))
            {
                return CompleteError(request, -1, "Sign check failed");
            }

            // 2. Проверяем action
            if (request.Action != 1)
            {
                return CompleteError(request, -3, "Action not found");
            }

            // 3. Если Click сообщил об ошибке — отменяем
            if (request.Error != 0)
            {
                var payment = await _context.ClickPayments
                    .FirstOrDefaultAsync(p => p.ClickTransId == request.ClickTransId);
                if (payment != null)
                {
                    payment.Status = "CANCELLED";
                    payment.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return CompleteError(request, -9, "Transaction cancelled");
            }

            // 4. Идемпотентность
            var existingPayment = await _context.ClickPayments
                .FirstOrDefaultAsync(p => p.ClickTransId == request.ClickTransId);

            if (existingPayment == null)
            {
                return CompleteError(request, -6, "Transaction does not exist");
            }

            if (existingPayment.Status == "COMPLETE")
            {
                return new ClickCompleteResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    MerchantConfirmId = existingPayment.Id,
                    Error = 0,
                    ErrorNote = "Success"
                };
            }

            // 5. Сверяем сумму
            if (request.Amount != existingPayment.Amount)
            {
                existingPayment.Status = "FAILED";
                existingPayment.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return CompleteError(request, -2, "Incorrect parameter amount");
            }

            // 6. Отправляем подтверждение в EMIS
            var emisPayRequest = new EmisPayRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                InvoiceId = existingPayment.EmisInvoiceId,
                PatientId = existingPayment.PatientId,
                Transaction = new EmisPayTransactionInfo
                {
                    PaymentId = request.ClickTransId.ToString(),
                    PaymentMethod = "CLICK",
                    Amount = existingPayment.Amount,
                    Timestamp = DateTime.UtcNow
                }
            };

            try
            {
                var emisResponse = await _emisService.ConfirmPaymentAsync(emisPayRequest);

                if (emisResponse.Status != "SUCCESS")
                {
                    _logger.LogWarning("EMIS pay failed: status={Status}, msg={Message}",
                        emisResponse.Status, emisResponse.Message);

                    existingPayment.Status = "FAILED";
                    existingPayment.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return CompleteError(request, -8, "Error in request from EMIS");
                }

                existingPayment.Status = "COMPLETE";
                existingPayment.EmisReceiptNumber = emisResponse.ReceiptNumber;
                existingPayment.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Click Complete OK: clickTransId={ClickTransId}, emisReceipt={Receipt}",
                    request.ClickTransId, emisResponse.ReceiptNumber);

                return new ClickCompleteResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    MerchantConfirmId = existingPayment.Id,
                    Error = 0,
                    ErrorNote = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EMIS pay exception for clickTransId={ClickTransId}", request.ClickTransId);
                existingPayment.Status = "FAILED";
                existingPayment.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return CompleteError(request, -9, "EMIS service unavailable");
            }
        }

        #region Signature Verification

        /// <summary>
        /// Проверка MD5-подписи для Prepare.
        /// Формула: MD5(click_trans_id + service_id + secret_key + merchant_trans_id + amount + action + sign_time)
        /// </summary>
        public bool VerifySignature(ClickPrepareRequest request)
        {
            var secretKey = _configuration["ClickSettings:SecretKey"] ?? "";
            var signString = $"{request.ClickTransId}{request.ServiceId}" +
                             $"{secretKey}{request.MerchantTransId}" +
                             $"{(int)request.Amount}{request.Action}{request.SignTime}";

            var expected = ComputeMd5(signString);
            return string.Equals(expected, request.SignString, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Проверка MD5-подписи для Complete.
        /// Формула: MD5(click_trans_id + service_id + secret_key + merchant_trans_id + merchant_prepare_id + amount + action + sign_time)
        /// </summary>
        public bool VerifySignature(ClickCompleteRequest request)
        {
            var secretKey = _configuration["ClickSettings:SecretKey"] ?? "";
            var signString = $"{request.ClickTransId}{request.ServiceId}" +
                             $"{secretKey}{request.MerchantTransId}" +
                             $"{request.MerchantPrepareId}" +
                             $"{(int)request.Amount}{request.Action}{request.SignTime}";

            var expected = ComputeMd5(signString);
            return string.Equals(expected, request.SignString, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeMd5(string input)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }

        #endregion

        private ClickPrepareResponse Error(int clickTransId, string merchantTransId, int errorCode, string message)
        {
            _logger.LogWarning("Click Prepare error {ErrorCode}: {Message}", errorCode, message);
            return new ClickPrepareResponse
            {
                ClickTransId = clickTransId,
                MerchantTransId = merchantTransId,
                Error = errorCode,
                ErrorNote = message
            };
        }

        private ClickCompleteResponse CompleteError(ClickCompleteRequest request, int errorCode, string message)
        {
            _logger.LogWarning("Click Complete error {ErrorCode}: {Message} for clickTransId={ClickTransId}",
                errorCode, message, request.ClickTransId);
            return new ClickCompleteResponse
            {
                ClickTransId = request.ClickTransId,
                MerchantTransId = request.MerchantTransId,
                Error = errorCode,
                ErrorNote = message
            };
        }
    }
}
