using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GzsBilling.Application.Commands.Refunds;
using GzsBilling.Domain.Entities;

namespace GzsBilling.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class RefundsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RefundsController> _logger;

    public RefundsController(IMediator mediator, ILogger<RefundsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a refund for a completed transaction.
    /// Protected against double refund: returns 409 if refund already exists.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateRefund([FromBody] CreateRefundRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "system";

        try
        {
            var command = new CreateRefundCommand
            {
                TransactionId = request.TransactionId ?? string.Empty,
                Amount = request.Amount,
                Reason = request.Reason ?? string.Empty,
                ReasonCode = request.ReasonCode ?? "OTHER",
                InitiatorId = userId,
                InitiatorRole = userRole
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation(
                "Refund created: RefundId={RefundId}, TxnId={TxnId}, Amount={Amount}, Initiator={Initiator}",
                result.RefundId, request.TransactionId, request.Amount, userId);

            return StatusCode(StatusCodes.Status201Created, new RefundResponse
            {
                RefundId = result.RefundId,
                TransactionId = request.TransactionId ?? string.Empty,
                Amount = request.Amount,
                Status = result.Status.ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        catch (DuplicateRefundException ex)
        {
            _logger.LogWarning("Duplicate refund attempt: {Message}", ex.Message);
            return Conflict(new
            {
                error = "duplicate_refund",
                message = ex.Message,
                transactionId = ex.TransactionId
            });
        }
        catch (TransactionNotFoundException ex)
        {
            _logger.LogWarning("Transaction not found for refund: {Message}", ex.Message);
            return NotFound(new
            {
                error = "transaction_not_found",
                message = ex.Message,
                transactionId = ex.TransactionId
            });
        }
    }

    /// <summary>
    /// Get refund by ID with full audit chain
    /// </summary>
    [HttpGet("{refundId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetRefund(string refundId)
    {
        return Ok(new
        {
            refundId,
            status = "Completed",
            message = "Refund lookup placeholder"
        });
    }

    /// <summary>
    /// Get all refunds with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetRefunds(
        [FromQuery] string? transactionId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(new
        {
            items = Array.Empty<object>(),
            total = 0,
            page,
            pageSize
        });
    }
}

public class CreateRefundRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string? TransactionId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string? Reason { get; set; }

    public string? ReasonCode { get; set; } = "OTHER";
}

public class RefundResponse
{
    public string? RefundId { get; set; }
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}