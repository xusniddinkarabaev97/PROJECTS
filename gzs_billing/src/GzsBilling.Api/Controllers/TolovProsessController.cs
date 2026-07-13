using System.ComponentModel.DataAnnotations;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Clients;
using GzsBilling.Infrastructure.Data;
using GzsBilling.Infrastructure.Messaging;
using GzsBilling.Infrastructure.Settings;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers;

/// <summary>
/// API controller for managing payment initiation and callback processing
/// from payment providers (Click, Paynet) in the GZS Billing System.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "UGaz")]
public class TolovProsessController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly IUGazInfrastrukturaClient _ugazClient;
    private readonly IRabbitMqTranzaksiyaPublisher _publisher;
    private readonly ISystemSettingService _settingsService;
    private readonly ILogger<TolovProsessController> _logger;

    public TolovProsessController(
        GzsBillingDbContext dbContext,
        IUGazInfrastrukturaClient ugazClient,
        IRabbitMqTranzaksiyaPublisher publisher,
        ISystemSettingService settingsService,
        ILogger<TolovProsessController> logger)
    {
        _dbContext = dbContext;
        _ugazClient = ugazClient;
        _publisher = publisher;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a new payment transaction for a refueling session.
    /// Accepts filling station ID, dispenser ID, and payment provider ID.
    /// Validates idempotency, pulls live metrics from UGaz API, and returns
    /// a newly generated transaction ID.
    /// </summary>
    /// <param name="request">Payment initiation payload containing station, dispenser, and payment identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 201 Created with the generated <c>transaction_id</c> on success.
    /// 409 Conflict if an idempotency key collision is detected.
    /// 502 Bad Gateway if the UGaz API is unreachable.
    /// </returns>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> InitsializatsiyaTolovAsync(
        [FromBody] TolovInitiateRequest request,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"{request.filling_station_id}_{request.dispenser_id}_{DateTimeOffset.UtcNow:yyyyMMddHH}";

        var existing = await _dbContext.Tranzaktsiyalar
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            _logger.LogWarning("Idempotency conflict for key {Key}", idempotencyKey);
            return Conflict(new { error = "DUPLICATE_REQUEST", message = "This transaction was already initiated.", transaction_id = existing.Id });
        }

        UGazSeansResponse? seans;
        try
        {
            seans = await _ugazClient.GetZapravkaSeansAsync(request.filling_station_id, request.dispenser_id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach UGaz API for station {Station}, dispenser {Dispenser}",
                request.filling_station_id, request.dispenser_id);
            return StatusCode(502, new { error = "UGAZ_UNREACHABLE", message = "Cannot fetch refueling metrics from UGaz." });
        }

        if (seans is null)
        {
            return BadRequest(new { error = "NO_ACTIVE_SEANS", message = "No active refueling session found." });
        }

        var tranzaksiya = new Tranzaksiya
        {
            Id = Guid.NewGuid(),
            TotalSum = seans.amount,
            FillingStationId = seans.filling_station_id,
            DispenserId = seans.dispenser_id,
            CardType = "Unknown",
            IdempotencyKey = idempotencyKey,
            PaymentId = 1,
            Status = (status?.ToLower()) switch { "cancelled" => TranzaksiyaStatus.Canceled, "pending" => TranzaksiyaStatus.Pending, _ => TranzaksiyaStatus.Completed },
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Tranzaktsiyalar.Add(tranzaksiya);
        await _dbContext.SaveChangesAsync(cancellationToken);



        _logger.LogInformation("Transaction {Id} initiated for station {Station}, amount {Amount}",
            tranzaksiya.Id, tranzaksiya.FillingStationId, tranzaksiya.TotalSum);

        return CreatedAtAction(nameof(InitsializatsiyaTolovAsync), new { id = tranzaksiya.Id }, new
        {
            transaction_id = tranzaksiya.Id,
            status = "Completed",
            total_sum = tranzaksiya.TotalSum,
            car_number = seans.car_number,
            amount = seans.amount,
            volume = seans.volume,
            operation_id = seans.operation_id,
            created_at = seans.created_at
        });
    }

    /// <summary>
    /// Processes a payment callback from the payment provider (Click/Paynet).
    /// Requires the <c>X-Idempotency-Key</c> header for safe retry handling.
    /// On successful completion, the transaction is immediately published
    /// to RabbitMQ for the 24-hour reconciliation buffer.
    /// </summary>
    /// <param name="callback">Callback payload containing transaction_id, status, and confirmed total_sum.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK with the updated transaction state.
    /// 400 Bad Request if validation fails.
    /// 404 Not Found if the transaction does not exist.
    /// </returns>
    [HttpPost("callback")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CallbackTolovTasdiqlashAsync(
        [FromBody] TolovCallbackRequest callback,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey))
        {
            return BadRequest(new { error = "MISSING_HEADER", message = "X-Idempotency-Key header is required." });
        }

        var tranzaksiya = await _dbContext.Tranzaktsiyalar
            .FirstOrDefaultAsync(t => t.Id == callback.transaction_id, cancellationToken);

        if (tranzaksiya is null)
        {
            return NotFound(new { error = "NOT_FOUND", message = "Transaction not found." });
        }

        if (tranzaksiya.Status == TranzaksiyaStatus.Completed ||
            tranzaksiya.Status == TranzaksiyaStatus.Failed ||
            tranzaksiya.Status == TranzaksiyaStatus.Canceled)
        {
            return Ok(new { transaction_id = tranzaksiya.Id, status = tranzaksiya.Status.ToString(), message = "Already processed." });
        }

        tranzaksiya.Status = callback.status.ToLower() switch
        {
            "completed" => TranzaksiyaStatus.Completed,
            "failed" => TranzaksiyaStatus.Failed,
            "canceled" => TranzaksiyaStatus.Canceled,
            _ => throw new ArgumentException($"Unknown status: {callback.status}")
        };

        tranzaksiya.TotalSum = callback.total_sum;
        tranzaksiya.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (tranzaksiya.Status == TranzaksiyaStatus.Completed)
        {
            try
            {
                await _publisher.PublishTranzaksiyaEventAsync(tranzaksiya);
                _logger.LogInformation("Transaction {Id} published to RabbitMQ after completion.", tranzaksiya.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish transaction {Id} to RabbitMQ. Data saved in DB.", tranzaksiya.Id);
            }


        }

        return Ok(new
        {
            transaction_id = tranzaksiya.Id,
            status = tranzaksiya.Status.ToString(),
            total_sum = tranzaksiya.TotalSum,
            updated_at = tranzaksiya.UpdatedAt
        });
    }

    private async Task<string> DetectCardTypeAsync(int paymentId)
    {
        var cardTypeMap = await _settingsService.GetPaymentIdCardTypeMapAsync();
        return cardTypeMap.GetValueOrDefault(paymentId) ?? await _settingsService.GetDefaultCardTypeAsync();
    }
}

/// <summary>
/// Request payload for initiating a payment transaction.
/// </summary>
public class TolovInitiateRequest
{
    [Required]
    public int filling_station_id { get; set; }

    [Required]
    public int dispenser_id { get; set; }
}

/// <summary>
/// Request payload for payment provider callback confirmation.
/// </summary>
public class TolovCallbackRequest
{
    /// <summary>
    /// The transaction ID generated during initiation.
    /// </summary>
    [Required]
    public Guid transaction_id { get; set; }

    /// <summary>
    /// The final status: "completed", "failed", or "canceled".
    /// </summary>
    [Required]
    public string status { get; set; } = string.Empty;

    /// <summary>
    /// The confirmed total amount from the payment provider.
    /// </summary>
    [Required]
    public decimal total_sum { get; set; }
}
