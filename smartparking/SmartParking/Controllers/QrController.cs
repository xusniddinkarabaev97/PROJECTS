using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SmartParking.Data;

namespace SmartParking.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public QrController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    /// <summary>
    /// Generate Click-compatible QR code for parking payment
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQr(int id, [FromQuery] int size = 250)
    {
        var txn = await _context.Transactions.FindAsync(id);
        if (txn == null)
            return NotFound(new { error = "Transaction not found" });

        var merchantId = _config["ClickSettings:MerchantId"] ?? "19876";
        var serviceId = _config["ClickSettings:ServiceId"] ?? "2005";

        // Click app QR format: service_id, merchant_id, amount in tiyins, transaction_param
        var amountTiyins = (long)(txn.TotalSum * 100);
        var qrData = $"service_id={serviceId}&merchant_id={merchantId}&amount={amountTiyins}&transaction_param={id}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.M);
        using var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(Math.Clamp(size, 100, 500));

        return Ok(new
        {
            transactionId = id,
            amount = txn.TotalSum,
            qrContent = qrData,
            base64 = Convert.ToBase64String(bytes),
            mimeType = "image/png"
        });
    }
}
