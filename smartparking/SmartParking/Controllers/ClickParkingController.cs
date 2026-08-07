using SmartParking.Data;
using SmartParking.Enums;
using SmartParking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SmartParking.Controllers
{
    /// <summary>
    /// Интеграция с платёжной системой Click для оплаты парковки.
    /// </summary>
    [Route("api/Transactions/click")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "click")]
    public class ClickParkingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ClickParkingController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        /// <summary>
        /// Шаг 1: Prepare — Click проверяет возможность оплаты.
        /// </summary>
        [HttpPost("prepare")]
        public async Task<IActionResult> Prepare([FromBody] ClickRequest request)
        {
            // 1. Проверить подпись
            var secretKey = _config["ClickSettings:SecretKey"] ?? "SmartParking-Click-Secret";
            var expectedSign = ComputeSign(request, secretKey, request.ClickTransId);
            if (request.SignString != expectedSign)
                return Ok(new ClickResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    Error = -1,
                    ErrorNote = "Invalid signature"
                });

            // 2. Найти транзакцию
            if (!int.TryParse(request.MerchantTransId, out var txnId))
                return Ok(new ClickResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    Error = -5,
                    ErrorNote = "Transaction not found"
                });

            var txn = await _context.Transactions.FindAsync(txnId);
            if (txn == null)
                return Ok(new ClickResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    Error = -5,
                    ErrorNote = "Transaction not found"
                });

            // 3. Проверить сумму (Click присылает в тийинах / 100)
            var clickAmount = request.Amount / 100m;
            if (clickAmount != txn.TotalSum)
                return Ok(new ClickResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    Error = -2,
                    ErrorNote = "Amount mismatch"
                });

            // 4. Проверить статус
            if (txn.PaymentStatus == PaymentStatus.Completed)
                return Ok(new ClickResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    Error = -4,
                    ErrorNote = "Already paid"
                });

            if (txn.PaymentStatus == PaymentStatus.Cancelled || txn.PaymentStatus == PaymentStatus.Failed)
                return Ok(new ClickResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    Error = -4,
                    ErrorNote = "Transaction cancelled"
                });

            // 5. OK
            txn.PaymentStatus = PaymentStatus.Pending;
            txn.PaymentMethod = "click";
            await _context.SaveChangesAsync();

            return Ok(new ClickResponse
            {
                ClickTransId = request.ClickTransId,
                MerchantTransId = request.MerchantTransId,
                MerchantPrepareId = txn.Id,
                Error = 0,
                ErrorNote = "Success"
            });
        }

        /// <summary>
        /// Шаг 2: Complete — Click подтверждает оплату.
        /// </summary>
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] ClickRequest request)
        {
            // 1. Проверить подпись
            var secretKey = _config["ClickSettings:SecretKey"] ?? "SmartParking-Click-Secret";
            var expectedSign = ComputeSign(request, secretKey, request.ClickTransId);
            if (request.SignString != expectedSign)
                return Ok(new ClickResponse
                {
                    ClickTransId = request.ClickTransId,
                    MerchantTransId = request.MerchantTransId,
                    Error = -1,
                    ErrorNote = "Invalid signature"
                });

            // 2. Найти транзакцию
            if (!int.TryParse(request.MerchantTransId, out var txnId))
                return Ok(new ClickResponse { ClickTransId = request.ClickTransId, MerchantTransId = request.MerchantTransId, Error = -5, ErrorNote = "Transaction not found" });

            var txn = await _context.Transactions.FindAsync(txnId);
            if (txn == null)
                return Ok(new ClickResponse { ClickTransId = request.ClickTransId, MerchantTransId = request.MerchantTransId, Error = -5, ErrorNote = "Transaction not found" });

            // 3. Обработать
            if (request.Error == 0)
            {
                txn.PaymentStatus = PaymentStatus.Completed;
                txn.FilledAt = DateTime.UtcNow;
            }
            else
            {
                txn.PaymentStatus = PaymentStatus.Failed;
            }
            await _context.SaveChangesAsync();

            return Ok(new ClickResponse
            {
                ClickTransId = request.ClickTransId,
                MerchantTransId = request.MerchantTransId,
                MerchantConfirmId = txn.Id,
                Error = 0,
                ErrorNote = "Success"
            });
        }

        private static string ComputeSign(ClickRequest req, string secret, long clickTransId)
        {
            var raw = $"{clickTransId}{req.ServiceId}{secret}{req.MerchantTransId}{req.MerchantPrepareId}{req.Amount}{req.Action}{req.SignTime}";
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash).ToLower();
        }
    }

    public class ClickRequest
    {
        public long ClickTransId { get; set; }
        public int ServiceId { get; set; }
        public long ClickPaydocId { get; set; }
        public string MerchantTransId { get; set; } = "";
        public int MerchantPrepareId { get; set; }
        public decimal Amount { get; set; }
        public int Action { get; set; }
        public int Error { get; set; }
        public string ErrorNote { get; set; } = "";
        public string SignTime { get; set; } = "";
        public string SignString { get; set; } = "";
    }

    public class ClickResponse
    {
        public long ClickTransId { get; set; }
        public string MerchantTransId { get; set; } = "";
        public int MerchantPrepareId { get; set; }
        public int MerchantConfirmId { get; set; }
        public int Error { get; set; }
        public string ErrorNote { get; set; } = "";
    }
}
