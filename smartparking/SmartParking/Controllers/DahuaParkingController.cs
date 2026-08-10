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
    [ApiExplorerSettings(GroupName = "uparking")]
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
                PaymentMethod = JsonSerializer.Serialize(new {
                    sessionId = req.SessionId,
                    plateNo = req.PlateNo,
                    parkingStart = req.ParkingStart,
                    parkingEnd = req.ParkingEnd,
                    parkingTimeSeconds = req.ParkingTimeSeconds
                }),
                Status = "parking",
                FilledAt = DateTime.UtcNow
            };
            _ctx.Transactions.Add(txn);
            await _ctx.SaveChangesAsync();

            // QR → avto.itpanda.uz с реальными данными въезда/выезда
            var qrContent = $"http://avto.itpanda.uz/pay.html"
                + $"?txn={txn.Id}"
                + $"&amount={req.Amount}"
                + $"&entry={req.ParkingStart:o}"
                + $"&exit={req.ParkingEnd:o}"
                + $"&duration={req.ParkingTimeSeconds}";

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
