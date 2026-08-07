using SmartParking.Data;
using SmartParking.Enums;
using SmartParking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "dahua")]
    public class DahuaParkingController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IConfiguration _config;

        public DahuaParkingController(ApplicationDbContext ctx, IConfiguration config)
        {
            _ctx = ctx;
            _config = config;
        }

        /// <summary>
        /// Dahua sends exit data, we return QR for Click payment
        /// </summary>
        [AllowAnonymous]
        [HttpPost("exit")]
        public async Task<IActionResult> Exit([FromBody] DahuaExitRequest req)
        {
            var client = await _ctx.Clients.FirstOrDefaultAsync(c => c.ExternalId == req.PlateNo);
            if (client == null)
            {
                client = new Client { ExternalId = req.PlateNo, FullName = req.PlateNo, Source = "dahua", Status = "active" };
                _ctx.Clients.Add(client);
                await _ctx.SaveChangesAsync();
            }

            var txn = new Transaction
            {
                ClientId = client.Id,
                TotalSum = req.Amount,
                PaymentStatus = PaymentStatus.New,
                PaymentMethod = System.Text.Json.JsonSerializer.Serialize(req),
                Status = "parking",
                FilledAt = DateTime.UtcNow
            };
            _ctx.Transactions.Add(txn);
            await _ctx.SaveChangesAsync();

            var mid = _config["ClickSettings:MerchantId"] ?? "19876";
            var sid = _config["ClickSettings:ServiceId"] ?? "2005";
            var amt = (long)(txn.TotalSum * 100);
            var qrContent = $"service_id={sid}&merchant_id={mid}&amount={amt}&transaction_param={txn.Id}";

            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.M);
            using var qr = new PngByteQRCode(data);
            var qrBase64 = Convert.ToBase64String(qr.GetGraphic(5));

            return Ok(new DahuaExitResponse
            {
                TransactionId = txn.Id,
                SessionId = req.SessionId,
                PlateNumber = req.PlateNo,
                Amount = req.Amount,
                QrCodeBase64 = qrBase64,
                QrContent = qrContent,
                Status = "qr_generated"
            });
        }
    }

    public class DahuaExitRequest
    {
        public string SessionId { get; set; } = "";
        public DateTime ParkingStart { get; set; }
        public DateTime ParkingEnd { get; set; }
        public int ParkingTimeSeconds { get; set; }
        public string PlateNo { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "UZS";
        public string Purpose { get; set; } = "Parking";
    }

    public class DahuaExitResponse
    {
        public int TransactionId { get; set; }
        public string SessionId { get; set; } = "";
        public string PlateNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public string QrCodeBase64 { get; set; } = "";
        public string QrContent { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
