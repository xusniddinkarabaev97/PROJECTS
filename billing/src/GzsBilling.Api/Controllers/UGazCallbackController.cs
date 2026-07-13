using System.ComponentModel.DataAnnotations;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using GzsBilling.Infrastructure.Data;
using GzsBilling.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers;

/// <summary>
/// UGaz dan keladigan callback (to'lov tasdiqlash) uchun endpoint.
/// </summary>
[ApiController]
[Route("api/v1/ugaz")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "UGaz")]
public class UGazCallbackController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly IRabbitMqTranzaksiyaPublisher _publisher;
    private readonly ILogger<UGazCallbackController> _logger;

    public UGazCallbackController(
        GzsBillingDbContext dbContext,
        IRabbitMqTranzaksiyaPublisher publisher,
        ILogger<UGazCallbackController> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// UGaz callback — to'lov natijasini qabul qiladi.
    /// </summary>
    [HttpPost("callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UGazCallback(
        [FromBody] UGazCallbackPayload payload,
        CancellationToken ct)
    {
        if (payload.filling_station_id <= 0 || payload.operation_id <= 0)
            return BadRequest(new { error = "INVALID_PAYLOAD", message = "filling_station_id and operation_id are required." });

        _logger.LogInformation(
            "UGaz callback received: station={Station}, opId={OpId}, status={Status}, method={Method}, txnId={TxnId}",
            payload.filling_station_id, payload.operation_id, payload.payment_status, payload.payment_method, payload.transactionId);

        // Operation ID asosida mavjud tranzaksiyani qidirish
        var idempotencyKey = payload.transactionId ?? $"UGZ-{payload.operation_id}";
        var tranzaksiya = await _dbContext.Tranzaktsiyalar
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);

        if (tranzaksiya is null)
        {
            // Yangi tranzaksiya yaratish
            tranzaksiya = new Tranzaksiya
            {
                Id = Guid.NewGuid(),
                TotalSum = 0, // summa UGaz dan keyinroq keladi yoki boshqa endpointdan
                FillingStationId = payload.filling_station_id,
                CardType = "Unknown",
                IdempotencyKey = idempotencyKey,
                PaymentId = 1, // default
                Status = payload.payment_status == "finished" ? TranzaksiyaStatus.Completed : TranzaksiyaStatus.Pending,
                CreatedAt = payload.payment_date != default ? payload.payment_date : DateTimeOffset.UtcNow
            };

            _dbContext.Tranzaktsiyalar.Add(tranzaksiya);
            _logger.LogInformation("New transaction {Id} created from UGaz callback", tranzaksiya.Id);
        }
        else
        {
            // Mavjud tranzaksiyani yangilash
            tranzaksiya.Status = payload.payment_status == "finished" ? TranzaksiyaStatus.Completed
                : payload.payment_status == "failed" ? TranzaksiyaStatus.Failed
                : TranzaksiyaStatus.Pending;
            tranzaksiya.UpdatedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("Transaction {Id} updated from UGaz callback: {Status}", tranzaksiya.Id, tranzaksiya.Status);
        }

        await _dbContext.SaveChangesAsync(ct);

        // Agar to'lov yakunlangan bo'lsa, RabbitMQ ga publish qilish
        if (tranzaksiya.Status == TranzaksiyaStatus.Completed)
        {
            try
            {
                await _publisher.PublishTranzaksiyaEventAsync(tranzaksiya);
                _logger.LogInformation("Transaction {Id} published to RabbitMQ (UGaz callback)", tranzaksiya.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ publish failed for transaction {Id} — saved in DB", tranzaksiya.Id);
            }
        }

        return Ok(new
        {
            success = true,
            transaction_id = tranzaksiya.Id,
            status = tranzaksiya.Status.ToString(),
            message = "Callback processed successfully"
        });
    }
}

/// <summary>
/// UGaz callback payload modeli.
/// </summary>
public class UGazCallbackPayload
{
    [Required]
    public int filling_station_id { get; set; }

    [Required]
    public int operation_id { get; set; }

    public DateTimeOffset payment_date { get; set; }

    public string payment_method { get; set; } = string.Empty;

    public string payment_status { get; set; } = string.Empty;

    public string? transactionId { get; set; }

    public string action { get; set; } = string.Empty;
}
