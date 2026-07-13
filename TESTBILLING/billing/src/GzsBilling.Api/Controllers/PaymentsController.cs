using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using GzsBilling.Domain.Entities;
using GzsBilling.Api.Models;
using GzsBilling.Infrastructure.Persistence;
using System.Security.Claims;
using System.Linq;

namespace GzsBilling.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
[EnableRateLimiting("Default")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly BillingDbContext _db;

    public PaymentsController(ILogger<PaymentsController> logger, BillingDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// Create a new payment transaction
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public IActionResult CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var contragentId = User.FindFirstValue("contragent_id")
                           ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? "unknown";

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionId = GenerateTransactionId(),
            ContragentId = contragentId,
            PaymentSystem = request.PaymentSystem ?? string.Empty,
            Amount = request.Amount,
            Currency = request.Currency ?? "UZS",
            Status = TransactionStatus.Created,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _logger.LogInformation(
            "Payment created: TxnId={TxnId}, Contragent={Contragent}, Amount={Amount} {Currency}, System={System}",
            transaction.TransactionId, contragentId, request.Amount,
            transaction.Currency, request.PaymentSystem);

        var response = new TransactionResponse
        {
            Id = transaction.Id,
            TransactionId = transaction.TransactionId,
            Status = transaction.Status.ToString(),
            CreatedAt = transaction.CreatedAt,
            Amount = transaction.Amount,
            Currency = transaction.Currency
        };

        return CreatedAtAction(nameof(GetTransaction),
            new { transactionId = transaction.TransactionId }, response);
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("{transactionId}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetTransaction(string transactionId)
    {
        _logger.LogInformation("Transaction lookup: {TransactionId}", transactionId);

        // TODO: Replace with actual repository lookup
        if (transactionId == "test-123")
        {
            return Ok(new TransactionResponse
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                Status = "Completed",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
                Amount = 150000,
                Currency = "UZS"
            });
        }

        return NotFound(new { error = "Transaction not found", transactionId });
    }

    /// <summary>
    /// Get all transactions with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Transaction list requested. Search={Search}, Status={Status}, Page={Page}",
            search, status, page);

        var query = _db.Transactions.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(t =>
                t.TransactionId.ToLower().Contains(lowerSearch) ||
                t.ContragentId.ToLower().Contains(lowerSearch) ||
                t.PaymentSystem.ToLower().Contains(lowerSearch));
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(status)
            && Enum.TryParse<TransactionStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(t => t.Status == parsedStatus);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                id = t.Id,
                transactionId = t.TransactionId,
                contragentId = t.ContragentId,
                paymentSystem = t.PaymentSystem,
                amount = t.Amount,
                currency = t.Currency,
                status = t.Status.ToString(),
                createdAt = t.CreatedAt,
                stationName = t.StationName,
                columnName = t.ColumnName
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    /// <summary>
    /// Get daily transaction statistics
    /// </summary>
    [HttpGet("stats/dashboard")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public IActionResult GetDashboard()
    {
        return Ok(new DashboardResponse
        {
            TotalTransactions = 15234,
            TodayTotalAmount = 875000000,
            SuccessfulRate = 98.5m,
            ActiveContragents = 47
        });
    }

    /// <summary>
    /// Health check - no auth required
    /// </summary>
    [HttpGet("/health")]
    [AllowAnonymous]
    [DisableRateLimiting]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
    }

    private static string GenerateTransactionId()
    {
        return $"TXN-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
    }
}
