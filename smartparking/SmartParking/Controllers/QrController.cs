using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SmartParking.Data;

namespace SmartParking.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "uparking")]
public class QrController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public QrController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generate QR by transaction ID — avto.itpanda.uz link with entry/exit data
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQr(int id, [FromQuery] int size = 250)
    {
        var txn = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id);
        if (txn == null)
            return NotFound(new { error = "Transaction not found" });

        // Extract entry/exit from PaymentMethod
        string qrUrl = $"http://avto.itpanda.uz/index.html?txn={id}&amount={txn.TotalSum}";

        if (!string.IsNullOrEmpty(txn.PaymentMethod))
        {
            try
            {
                var json = txn.PaymentMethod.Split('|')[0];
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var r = doc.RootElement;

                if (r.TryGetProperty("plateNo", out var pn))
                    qrUrl += "&plate=" + Uri.EscapeDataString(pn.GetString()!);

                if (r.TryGetProperty("parkingStart", out var ps))
                {
                    var v = ps.GetString();
                    if (!string.IsNullOrEmpty(v) && !v.StartsWith("0001-01-01"))
                        qrUrl += "&entry=" + Uri.EscapeDataString(v);
                }
                else if (r.TryGetProperty("kirish", out var k))
                    qrUrl += "&entry=" + Uri.EscapeDataString(k.GetString()!);
                else if (r.TryGetProperty("Kirish", out var k2))
                    qrUrl += "&entry=" + Uri.EscapeDataString(k2.GetString()!);

                if (r.TryGetProperty("parkingEnd", out var pe))
                {
                    var v = pe.GetString();
                    if (!string.IsNullOrEmpty(v) && !v.StartsWith("0001-01-01"))
                        qrUrl += "&exit=" + Uri.EscapeDataString(v);
                }
                else if (r.TryGetProperty("chiqish", out var c))
                    qrUrl += "&exit=" + Uri.EscapeDataString(c.GetString()!);
                else if (r.TryGetProperty("Chiqish", out var c2))
                    qrUrl += "&exit=" + Uri.EscapeDataString(c2.GetString()!);

                if (r.TryGetProperty("parkingTimeSeconds", out var pts))
                    qrUrl += "&duration=" + pts.GetInt32();
                else if (r.TryGetProperty("Davomiyligi", out var d))
                    qrUrl += "&duration=" + Uri.EscapeDataString(d.GetString()!);
            }
            catch { }
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.M);
        using var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(Math.Clamp(size, 100, 500));

        return Ok(new
        {
            transactionId = id,
            amount = txn.TotalSum,
            qrUrl,
            base64 = Convert.ToBase64String(bytes),
            mimeType = "image/png"
        });
    }
}
