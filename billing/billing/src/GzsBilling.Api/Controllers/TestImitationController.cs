using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GzsBilling.Api.Models;

namespace GzsBilling.Api.Controllers;

[ApiController]
[Route("api/v1/test")]
[AllowAnonymous]
public class TestImitationController : ControllerBase
{
    private readonly ILogger<TestImitationController> _logger;

    private static readonly string[] ValidProviders =
    {
        "uzcard", "humo", "click", "payme", "apelsin"
    };

    public TestImitationController(ILogger<TestImitationController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint: create a simulated transaction for testing purposes.
    /// No authentication required. No real payment processing happens.
    /// </summary>
    [HttpPost("test-imitation")]
    [ProducesResponseType(typeof(TestTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateTestTransaction([FromBody] TestImitationRequest request)
    {
        // --- validation ---
        if (request.Amount < 100 || request.Amount > 999999999)
        {
            return BadRequest(new { error = "Amount must be between 100 and 999,999,999 UZS" });
        }

        if (string.IsNullOrWhiteSpace(request.PaymentSystem))
        {
            return BadRequest(new { error = "Payment system is required" });
        }

        if (!ValidProviders.Contains(request.PaymentSystem.ToLowerInvariant()))
        {
            return BadRequest(new
            {
                error = $"Invalid payment provider. Must be one of: {string.Join(", ", ValidProviders)}"
            });
        }

        // --- build response ---
        var transactionId =
            $"TXN-TEST-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var now = DateTimeOffset.UtcNow;

        var response = new TestTransactionResponse
        {
            TransactionId = transactionId,
            PaymentSystem = request.PaymentSystem,
            Amount = request.Amount,
            Currency = request.Currency ?? "UZS",
            CarNumber = request.CarNumber ?? "N/A",
            Status = "Created",
            CreatedAt = now,
            IsTestMode = true
        };

        _logger.LogInformation(
            "TEST TRANSACTION: TxnId={TxnId}, Provider={Provider}, Amount={Amount}, Car={CarNumber}",
            transactionId, request.PaymentSystem, request.Amount, request.CarNumber);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
