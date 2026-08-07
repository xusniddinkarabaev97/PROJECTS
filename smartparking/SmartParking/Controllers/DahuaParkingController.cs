using SmartParking.Data;
using SmartParking.Enums;
using SmartParking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Text;
using System.Text.Json;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/billing")]
    [ApiExplorerSettings(GroupName = "dahua")]
    public class DahuaParkingController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _http;

        public DahuaParkingController(ApplicationDbContext ctx, IConfiguration config, IHttpClientFactory http)
        {
            _ctx = ctx;
            _config = config;
            _http = http;
        }

        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] UParkingRequest req,
            [FromHeader(Name = "X-Billing-Secret")] string? secret)
        {
            var expectedSecret = _config["Billing:SharedSecret"];
            if (!string.IsNullOrEmpty(expectedSecret) && secret != expectedSecret)
                return Unauthorized(new { message = "Invalid billing secret" });

            var client = await _ctx.Clients.FirstOrDefaultAsync(c => c.ExternalId == req.PlateNo);
            if (client == null)
            {
                client = new Client { ExternalId = req.PlateNo, FullName = req.PlateNo, Source = "uparking", Status = "active" };
                _ctx.Clients.Add(client);
                await _ctx.SaveChangesAsync();
            }

            var txn = new Transaction
            {
                ClientId = client.Id,
                TotalSum = req.Amount,
                PaymentStatus = PaymentStatus.New,
                PaymentMethod = JsonSerializer.Serialize(new { sessionId = req.SessionId, plateNo = req.PlateNo }),
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

            return Ok(new
            {
                billingReferenceId = txn.Id.ToString(),
                qrPayload = qrContent,
                qrCodeBase64 = qrBase64
            });
        }
    }

    public class UParkingRequest
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
}
