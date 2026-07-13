using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GzsBilling.Application.Commands.Disputes;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;

namespace GzsBilling.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class DisputesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DisputesController> _logger;

    public DisputesController(IMediator mediator, ILogger<DisputesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new dispute for a transaction.
    /// Sets status to Open and SLA deadline to 30 calendar days from creation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateDisputeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        var command = new CreateDisputeCommand(
            request.TransactionId ?? string.Empty,
            request.ContragentId ?? string.Empty,
            request.Amount,
            request.Reason ?? string.Empty,
            userId);

        string disputeId = await _mediator.Send(command);

        _logger.LogInformation(
            "Dispute created: DisputeId={DisputeId}, TxnId={TxnId}, Contragent={Contragent}, Amount={Amount}",
            disputeId, request.TransactionId, request.ContragentId, request.Amount);

        var response = new CreateDisputeResponse
        {
            DisputeId = disputeId,
            TransactionId = request.TransactionId ?? string.Empty,
            ContragentId = request.ContragentId ?? string.Empty,
            Amount = request.Amount,
            Status = "Open",
            SlaDeadline = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Retrieve a dispute by ID, including its full history and evidence list.
    /// </summary>
    [HttpGet("{disputeId}")]
    [ProducesResponseType(typeof(DisputeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetDispute(string disputeId)
    {
        Dispute? dispute = CreateDisputeCommandHandler.GetDispute(disputeId);

        if (dispute is null)
        {
            return NotFound(new
            {
                error = "dispute_not_found",
                message = $"No dispute found with ID '{disputeId}'.",
                disputeId
            });
        }

        var response = new DisputeDetailResponse
        {
            DisputeId = dispute.DisputeId,
            TransactionId = dispute.TransactionId,
            ContragentId = dispute.ContragentId,
            Amount = dispute.Amount,
            Reason = dispute.Reason,
            Status = dispute.Status.ToString(),
            CreatedAt = dispute.CreatedAt,
            SlaDeadline = dispute.SlaDeadline,
            ResolvedAt = dispute.ResolvedAt,
            ResolutionNotes = dispute.ResolutionNotes,
            History = dispute.History.Select(h => new DisputeHistoryResponse
            {
                Timestamp = h.Timestamp,
                Action = h.Action,
                ChangedBy = h.ChangedBy,
                Notes = h.Notes,
                PreviousStatus = h.PreviousStatus?.ToString(),
                NewStatus = h.NewStatus?.ToString()
            }).ToList(),
            Evidence = dispute.Evidence.Select(e => new DisputeEvidenceResponse
            {
                EvidenceId = e.EvidenceId,
                FileName = e.FileName,
                ContentType = e.ContentType,
                SizeBytes = e.SizeBytes,
                UploadedAt = e.UploadedAt,
                UploadedBy = e.UploadedBy
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Update the status of a dispute.
    /// Allowed transitions: Open to UnderReview, UnderReview to Resolved/Rejected, Rejected to UnderReview.
    /// </summary>
    [HttpPut("{disputeId}/status")]
    [ProducesResponseType(typeof(DisputeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDisputeStatus(
        string disputeId,
        [FromBody] UpdateDisputeStatusRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        if (!Enum.TryParse<DisputeStatus>(request.NewStatus, ignoreCase: true, out var newStatus))
        {
            return BadRequest(new
            {
                error = "invalid_status",
                message = $"'{request.NewStatus}' is not a valid dispute status.",
                validStatuses = Enum.GetNames(typeof(DisputeStatus))
            });
        }

        try
        {
            var command = new UpdateDisputeStatusCommand(
                disputeId,
                newStatus,
                userId,
                request.Notes);

            Dispute dispute = await _mediator.Send(command);

            _logger.LogInformation(
                "Dispute status updated: DisputeId={DisputeId}, NewStatus={NewStatus}, ChangedBy={ChangedBy}",
                disputeId, newStatus, userId);

            var response = new DisputeDetailResponse
            {
                DisputeId = dispute.DisputeId,
                TransactionId = dispute.TransactionId,
                ContragentId = dispute.ContragentId,
                Amount = dispute.Amount,
                Reason = dispute.Reason,
                Status = dispute.Status.ToString(),
                CreatedAt = dispute.CreatedAt,
                SlaDeadline = dispute.SlaDeadline,
                ResolvedAt = dispute.ResolvedAt,
                ResolutionNotes = dispute.ResolutionNotes,
                History = dispute.History.Select(h => new DisputeHistoryResponse
                {
                    Timestamp = h.Timestamp,
                    Action = h.Action,
                    ChangedBy = h.ChangedBy,
                    Notes = h.Notes,
                    PreviousStatus = h.PreviousStatus?.ToString(),
                    NewStatus = h.NewStatus?.ToString()
                }).ToList(),
                Evidence = dispute.Evidence.Select(e => new DisputeEvidenceResponse
                {
                    EvidenceId = e.EvidenceId,
                    FileName = e.FileName,
                    ContentType = e.ContentType,
                    SizeBytes = e.SizeBytes,
                    UploadedAt = e.UploadedAt,
                    UploadedBy = e.UploadedBy
                }).ToList()
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                error = "dispute_not_found",
                message = ex.Message,
                disputeId
            });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new
            {
                error = "invalid_transition",
                message = ex.Message,
                disputeId
            });
        }
    }

    /// <summary>
    /// List disputes with optional filters for status, contragent, and date range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DisputeListResponse), StatusCodes.Status200OK)]
    public IActionResult GetDisputes(
        [FromQuery] string? status,
        [FromQuery] string? contragentId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var disputes = CreateDisputeCommandHandler.ListDisputes(status, contragentId, from, to);
        var disputeList = disputes.ToList();

        var paged = disputeList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DisputeSummaryResponse
            {
                DisputeId = d.DisputeId,
                TransactionId = d.TransactionId,
                ContragentId = d.ContragentId,
                Amount = d.Amount,
                Reason = d.Reason,
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt,
                SlaDeadline = d.SlaDeadline
            })
            .ToList();

        return Ok(new DisputeListResponse
        {
            Items = paged,
            Total = disputeList.Count,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Upload evidence documents for a dispute.
    /// Accepts metadata about evidence files (actual file upload would use multipart).
    /// </summary>
    [HttpPut("{disputeId}/evidence")]
    [ProducesResponseType(typeof(DisputeEvidenceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UploadEvidence(
        string disputeId,
        [FromBody] UploadEvidenceRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        Dispute? dispute = CreateDisputeCommandHandler.GetDispute(disputeId);

        if (dispute is null)
        {
            return NotFound(new
            {
                error = "dispute_not_found",
                message = $"No dispute found with ID '{disputeId}'.",
                disputeId
            });
        }

        if (request.Files is null || request.Files.Count == 0)
        {
            return BadRequest(new
            {
                error = "no_files",
                message = "At least one evidence file must be provided."
            });
        }

        var addedEvidence = new List<DisputeEvidence>();

        foreach (var file in request.Files)
        {
            var evidence = new DisputeEvidence
            {
                EvidenceId = $"EVD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                FileName = file.FileName ?? "unnamed",
                ContentType = file.ContentType ?? "application/octet-stream",
                SizeBytes = file.SizeBytes,
                UploadedAt = DateTimeOffset.UtcNow,
                UploadedBy = userId
            };

            dispute.Evidence.Add(evidence);
            addedEvidence.Add(evidence);
        }

        dispute.History.Add(new DisputeHistoryEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = "EvidenceUploaded",
            ChangedBy = userId,
            Notes = $"{addedEvidence.Count} evidence file(s) uploaded.",
            PreviousStatus = dispute.Status,
            NewStatus = dispute.Status
        });

        CreateDisputeCommandHandler.UpdateDispute(dispute);

        _logger.LogInformation(
            "Evidence uploaded for dispute {DisputeId}: {Count} file(s) by {User}",
            disputeId, addedEvidence.Count, userId);

        return Ok(new DisputeEvidenceListResponse
        {
            DisputeId = disputeId,
            Evidence = addedEvidence.Select(e => new DisputeEvidenceResponse
            {
                EvidenceId = e.EvidenceId,
                FileName = e.FileName,
                ContentType = e.ContentType,
                SizeBytes = e.SizeBytes,
                UploadedAt = e.UploadedAt,
                UploadedBy = e.UploadedBy
            }).ToList(),
            TotalCount = dispute.Evidence.Count
        });
    }
}

public class CreateDisputeRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string? TransactionId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public string? ContragentId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(0.01, 999999999999.99)]
    public decimal Amount { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? Reason { get; set; }
}

public class UpdateDisputeStatusRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string? NewStatus { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? Notes { get; set; }
}

public class UploadEvidenceRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public List<UploadEvidenceFile>? Files { get; set; }
}

public class UploadEvidenceFile
{
    [System.ComponentModel.DataAnnotations.Required]
    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, long.MaxValue)]
    public long SizeBytes { get; set; }
}

public class CreateDisputeResponse
{
    public string? DisputeId { get; set; }
    public string? TransactionId { get; set; }
    public string? ContragentId { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset SlaDeadline { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class DisputeDetailResponse
{
    public string? DisputeId { get; set; }
    public string? TransactionId { get; set; }
    public string? ContragentId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset SlaDeadline { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public List<DisputeHistoryResponse> History { get; set; } = new();
    public List<DisputeEvidenceResponse> Evidence { get; set; } = new();
}

public class DisputeHistoryResponse
{
    public DateTimeOffset Timestamp { get; set; }
    public string? Action { get; set; }
    public string? ChangedBy { get; set; }
    public string? Notes { get; set; }
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
}

public class DisputeEvidenceResponse
{
    public string? EvidenceId { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
}

public class DisputeSummaryResponse
{
    public string? DisputeId { get; set; }
    public string? TransactionId { get; set; }
    public string? ContragentId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset SlaDeadline { get; set; }
}

public class DisputeListResponse
{
    public List<DisputeSummaryResponse> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class DisputeEvidenceListResponse
{
    public string? DisputeId { get; set; }
    public List<DisputeEvidenceResponse> Evidence { get; set; } = new();
    public int TotalCount { get; set; }
}
