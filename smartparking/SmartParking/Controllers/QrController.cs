using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace SmartParking.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrController : ControllerBase
{
    /// <summary>
    /// Generate QR code as Base64 PNG for a transaction payment link
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="size">Size in pixels (default 250)</param>
    /// <returns>Base64-encoded PNG image</returns>
    [AllowAnonymous]
    [HttpGet("{id}")]
    public IActionResult GetQr(int id, [FromQuery] int size = 250)
    {
        var url = "http://avto.itpanda.uz";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(Math.Clamp(size, 100, 500));

        return Ok(new
        {
            transactionId = id,
            url,
            base64 = Convert.ToBase64String(bytes),
            mimeType = "image/png"
        });
    }
}
