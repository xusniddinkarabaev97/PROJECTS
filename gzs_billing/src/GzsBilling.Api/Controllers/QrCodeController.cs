using System.ComponentModel.DataAnnotations;
using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace GzsBilling.Api.Controllers;

/// <summary>
/// QR-kod yaratish uchun API.
/// Yoqilg'i quyish shoxobchalarida to'lov QR kodlari generatsiya qiladi.
/// </summary>
[ApiController]
[Route("api/v1/qr")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "QR")]
public class QrCodeController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly ILogger<QrCodeController> _logger;

    public QrCodeController(
        GzsBillingDbContext dbContext,
        ILogger<QrCodeController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// QR-kod generatsiya qiladi va base64 PNG formatda qaytaradi.
    /// QR ichida to'lov URL: gzs-billing://pay?station=ID&amp;dispenser=ID&amp;payment=ID
    /// </summary>
    /// <param name="stationId">Shoxobcha ID (filling_station_id)</param>
    /// <param name="dispenserId">Kolonna raqami (dispenser_id)</param>
    /// <param name="paymentId">To'lov provayderi ID (1=Uzcard, 2=Humo)</param>
    [HttpGet]
    [ProducesResponseType(typeof(QrCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateQr(
        [FromQuery, Required] int stationId,
        [FromQuery, Required] int dispenserId,
        CancellationToken ct)
    {
        if (stationId <= 0 || dispenserId <= 0)
            return BadRequest(new { error = "INVALID_PARAMS", message = "All IDs must be positive." });

        var station = await _dbContext.FillingStations
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == stationId && f.IsActive, ct);

        if (station is null)
            return NotFound(new { error = "NOT_FOUND", message = $"Filling station {stationId} not found." });

        var dispenser = await _dbContext.Dispensers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dispenserId && d.FillingStationId == stationId && d.IsActive, ct);

        if (dispenser is null)
            return NotFound(new { error = "NOT_FOUND", message = $"Dispenser {dispenserId} not found at station {stationId}." });

        var qrContent = $"gzs-billing://pay?station={stationId}&dispenser={dispenserId}";
        var webFallback = $"https://whirl.uz/swagger/UGaz";

        // QR kod PNG rasm generatsiya
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrBytes = qrCode.GetGraphic(10);
        var qrBase64 = Convert.ToBase64String(qrBytes);

        _logger.LogInformation(
            "QR generated: station={Station}({SId}), dispenser={Dispenser}({DId})",
            station.Name, stationId, dispenser.Name, dispenserId);

        return Ok(new QrCodeResponse
        {
            StationId = stationId,
            StationName = station.Name,
            DispenserId = dispenserId,
            DispenserName = dispenser.Name,
            FuelType = dispenser.FuelType,
            QrContent = qrContent,
            WebFallbackUrl = webFallback,
            InitiatePayload = new
            {
                filling_station_id = stationId,
                dispenser_id = dispenserId
            },
            QrCodeBase64 = $"data:image/png;base64,{qrBase64}",
            GeneratedAt = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Barcha faol to'lov provayderlari ro'yxati.
    /// </summary>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivePayments(CancellationToken ct)
    {
        var payments = await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        return Ok(payments);
    }

    /// <summary>
    /// Barcha faol shoxobchalar ro'yxati (QR generatsiya uchun).
    /// </summary>
    [HttpGet("stations")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveStations(CancellationToken ct)
    {
        var stations = await _dbContext.FillingStations
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Region })
            .ToListAsync(ct);

        return Ok(stations);
    }

    /// <summary>
    /// Shoxobchadagi faol kolonkalar ro'yxati.
    /// </summary>
    [HttpGet("stations/{stationId}/dispensers")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDispensers(int stationId, CancellationToken ct)
    {
        var dispensers = await _dbContext.Dispensers
            .AsNoTracking()
            .Where(d => d.FillingStationId == stationId && d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, d.FuelType })
            .ToListAsync(ct);

        return Ok(dispensers);
    }
}

/// <summary>
/// QR-kod javobi.
/// </summary>
public class QrCodeResponse
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int DispenserId { get; set; }
    public string DispenserName { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public int PaymentId { get; set; }
    public string PaymentName { get; set; } = string.Empty;
    public string QrContent { get; set; } = string.Empty;
    public string WebFallbackUrl { get; set; } = string.Empty;
    public object InitiatePayload { get; set; } = null!;
    public string QrCodeBase64 { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
}
