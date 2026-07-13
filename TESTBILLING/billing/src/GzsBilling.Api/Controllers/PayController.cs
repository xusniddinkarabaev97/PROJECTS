using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GzsBilling.Infrastructure.Persistence;
using GzsBilling.Domain.Entities;

namespace GzsBilling.Api.Controllers;

[ApiController]
[Route("api/pay")]
[AllowAnonymous]
public class PayController : ControllerBase
{
    private readonly BillingDbContext _db;
    private readonly ILogger<PayController> _logger;

    public PayController(BillingDbContext db, ILogger<PayController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get column info for payment page
    /// </summary>
    [HttpGet("{columnId:guid}")]
    public async Task<IActionResult> GetColumnInfo(Guid columnId)
    {
        var column = await _db.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId);

        if (column == null)
            return NotFound(new { error = "column_not_found" });

        var station = await _db.Stations.FindAsync(column.StationId);

        return Ok(new
        {
            columnId = column.Id,
            columnName = column.Name,
            columnNumber = column.ColumnNumber,
            fuelType = column.FuelType,
            pricePerLiter = column.PricePerLiter,
            stationName = station?.Name ?? "Unknown",
            stationAddress = station?.Address ?? "",
            qrCodeUrl = $"/pay/{column.Id}"
        });
    }

    /// <summary>
    /// Process payment for a column (simulated)
    /// </summary>
    [HttpPost("{columnId:guid}")]
    public async Task<IActionResult> ProcessPayment(Guid columnId, [FromBody] PayRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be greater than 0" });

        if (string.IsNullOrWhiteSpace(request.PaymentSystem))
            return BadRequest(new { error = "Payment system is required" });

        var validProviders = new[] { "uzcard", "humo", "click", "payme", "apelsin" };
        if (!validProviders.Contains(request.PaymentSystem.ToLowerInvariant()))
            return BadRequest(new { error = $"Invalid provider. Use: {string.Join(", ", validProviders)}" });

        var column = await _db.Columns.FindAsync(columnId);
        if (column == null)
            return NotFound(new { error = "column_not_found" });

        var station = await _db.Stations.FindAsync(column.StationId);

        var liters = column.PricePerLiter > 0
            ? Math.Round(request.Amount / column.PricePerLiter, 2)
            : 0;

        var transactionId = $"PAY-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            ContragentId = columnId.ToString(),
            PaymentSystem = request.PaymentSystem,
            Amount = request.Amount,
            Currency = "UZS",
            Status = TransactionStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            ProcessedAt = DateTimeOffset.UtcNow,
            ExternalReference = column.Name,
            StationId = station?.Id,
            ColumnId = column.Id,
            StationName = station?.Name,
            ColumnName = column.Name
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Payment processed: TxnId={TxnId}, Column={Column}, Amount={Amount}, Provider={Provider}",
            transactionId, column.Name, request.Amount, request.PaymentSystem);

        return Ok(new
        {
            transactionId,
            status = "completed",
            amount = request.Amount,
            currency = "UZS",
            liters,
            pricePerLiter = column.PricePerLiter,
            fuelType = column.FuelType,
            columnName = column.Name,
            stationName = station?.Name ?? "Unknown",
            paymentSystem = request.PaymentSystem,
            processedAt = DateTimeOffset.UtcNow
        });
    }
}

public class PayRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(1, 999999999)]
    public decimal Amount { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string? PaymentSystem { get; set; }

    public string? CarNumber { get; set; }
}
