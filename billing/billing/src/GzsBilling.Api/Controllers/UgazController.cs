using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GzsBilling.Api.Services;
using GzsBilling.Domain.Entities;
using GzsBilling.Infrastructure.Persistence;

namespace GzsBilling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class UgazController : ControllerBase
{
    private readonly IUGazPaymentService _paymentService;
    private readonly BillingDbContext _db;
    private readonly ILogger<UgazController> _logger;

    public UgazController(IUGazPaymentService paymentService, BillingDbContext db, ILogger<UgazController> logger)
    {
        _paymentService = paymentService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get payment info from UGaz and create a pending transaction
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetInfo([FromQuery] int stationId = 264, [FromQuery] int dispenserId = 1)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != "Operator" && role != "SuperAdmin" && role != "Admin")
            return Unauthorized(new { error = "Faqat Operator yoki Admin huquqi kerak" });

        // Call UGaz API
        var result = await _paymentService.StartPaymentAsync(stationId, dispenserId);

        // Create transaction in DB
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionId = $"UGZ-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            ContragentId = stationId.ToString(),
            PaymentSystem = "ugaz",
            Amount = result.Data?.amount ?? 0,
            Currency = "UZS",
            Status = TransactionStatus.Created,
            CreatedAt = DateTimeOffset.UtcNow,
            StationId = null,
            ColumnId = null,
            StationName = $"Station #{stationId}",
            ColumnName = $"Dispenser #{dispenserId}",
            ExternalReference = result.Data?.car_number ?? ""
        };
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        // Return info + transactionId
        if (result.Success && result.Data != null)
        {
            return Ok(new
            {
                success = true,
                message = "Ma'lumot UGaz'dan olindi",
                transactionId = transaction.TransactionId,
                data = new
                {
                    result.Data.filling_station_id,
                    result.Data.dispenser_id,
                    result.Data.operation_id,
                    amount = result.Data.amount,
                    volume = result.Data.volume,
                    created_at = result.Data.created_at,
                    car_number = result.Data.car_number
                },
                debug = new
                {
                    request = result.RequestInfo,
                    httpCode = result.HttpCode
                }
            });
        }

        // Fallback: sample data
        var sampleData = new
        {
            filling_station_id = stationId,
            dispenser_id = dispenserId,
            operation_id = 99,
            amount = 150000m,
            volume = 36.6m,
            created_at = DateTimeOffset.UtcNow,
            car_number = "01A123BC"
        };

        return Ok(new
        {
            success = false,
            message = $"UGaz API xatolik (HTTP {result.HttpCode}): {result.ErrorMessage ?? "Noma'lum xatolik"}",
            transactionId = transaction.TransactionId,
            data = sampleData,
            debug = new
            {
                request = new
                {
                    result.RequestInfo?.Url,
                    result.RequestInfo?.Method,
                    result.RequestInfo?.RequestBody
                },
                response = new
                {
                    httpCode = result.HttpCode,
                    error = result.ErrorMessage,
                    responseBody = result.RequestInfo?.ResponseBody?.Substring(0, Math.Min(result.RequestInfo.ResponseBody?.Length ?? 0, 500))
                }
            }
        });
    }

    /// <summary>
    /// Process payment: pay or cancel using real UGaz API fields.
    /// Extracts action from payment_status if action is not provided:
    ///   "finished" → pay, "cancel_transaction" → cancel, "new" → pay
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] UgazProcessRequest request)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != "Operator" && role != "SuperAdmin" && role != "Admin")
            return Unauthorized(new { error = "Faqat Operator yoki Admin huquqi kerak" });

        // Resolve action: use explicit action if provided, otherwise derive from payment_status
        var action = request.action;
        if (string.IsNullOrWhiteSpace(action))
        {
            action = request.payment_status switch
            {
                "finished" => "pay",
                "cancel_transaction" => "cancel",
                "new" => "pay",
                _ => null
            };
        }

        if (string.IsNullOrWhiteSpace(action) || (action != "pay" && action != "cancel"))
            return BadRequest(new { error = "action must be 'pay' or 'cancel' (derived from payment_status if not provided)" });

        var paymentMethod = request.payment_method ?? "payme";
        var paymentStatus = request.payment_status ?? (action == "pay" ? "finished" : "cancel_transaction");

        // Look up local transaction by transactionId if provided
        Transaction? transaction = null;
        if (!string.IsNullOrWhiteSpace(request.transactionId))
        {
            transaction = await _db.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == request.transactionId);

            if (transaction == null)
                return NotFound(new { error = "transaction_not_found" });

            if (transaction.Status != TransactionStatus.Created)
                return BadRequest(new { error = "transaction_already_processed", status = transaction.Status.ToString() });
        }

        // Use real UGaz fields from the request body directly
        var stationId = request.filling_station_id;
        var operationId = request.operation_id;

        if (action == "pay")
        {
            var result = await _paymentService.ProcessPaymentAsync(stationId, operationId, paymentMethod, paymentStatus);

            if (result.Success)
            {
                if (transaction != null)
                {
                    transaction.Status = TransactionStatus.Completed;
                    transaction.ProcessedAt = DateTimeOffset.UtcNow;
                    await _db.SaveChangesAsync();
                }

                _logger.LogInformation(
                    "UGaz payment completed: StationId={StationId}, OperationId={OperationId}, TxnId={TxnId}",
                    stationId, operationId, request.transactionId);

                return Ok(new
                {
                    success = true,
                    message = "To'lov muvaffaqiyatli amalga oshirildi",
                    transactionId = request.transactionId,
                    status = "Completed",
                    debug = new { request = result.RequestInfo, httpCode = result.HttpCode }
                });
            }

            return Ok(new
            {
                success = false,
                message = "To'lovda xatolik",
                transactionId = request.transactionId,
                debug = new { request = result.RequestInfo, httpCode = result.HttpCode, error = result.ErrorMessage }
            });
        }
        else // cancel
        {
            var result = await _paymentService.ProcessPaymentAsync(stationId, operationId, paymentMethod, paymentStatus);

            if (transaction != null)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.ProcessedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
            }

            _logger.LogInformation(
                "UGaz payment cancelled: StationId={StationId}, OperationId={OperationId}, TxnId={TxnId}",
                stationId, operationId, request.transactionId);

            return Ok(new
            {
                success = true,
                message = "To'lov bekor qilindi",
                transactionId = request.transactionId,
                status = "Cancelled",
                debug = new { request = result.RequestInfo, httpCode = result.HttpCode }
            });
        }
    }
}

public class UgazProcessRequest
{
    public int filling_station_id { get; set; }
    public int operation_id { get; set; }
    public DateTime payment_date { get; set; } = DateTime.UtcNow;
    public string? payment_method { get; set; } // "payme", "uzcard", etc
    public string? payment_status { get; set; } // "new", "finished", "cancel_transaction"
    public string? transactionId { get; set; }
    public string? action { get; set; } // "pay", "cancel" - convenience field
}
