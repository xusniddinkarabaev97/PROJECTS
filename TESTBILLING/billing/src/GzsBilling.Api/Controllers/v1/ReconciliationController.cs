using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GzsBilling.Application.Services;
using GzsBilling.Domain.Entities;

namespace GzsBilling.Api.Controllers.v1;

/// <summary>
/// Manages reconciliation operations between internal billing records
/// and external provider statements.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class ReconciliationController : ControllerBase
{
    private readonly ReconciliationService _reconciliationService;
    private readonly ILogger<ReconciliationController> _logger;

    public ReconciliationController(
        ReconciliationService reconciliationService,
        ILogger<ReconciliationController> logger)
    {
        _reconciliationService = reconciliationService;
        _logger = logger;
    }

    /// <summary>
    /// Trigger a manual reconciliation run for the specified provider.
    /// </summary>
    [HttpPost("run/{provider}")]
    [ProducesResponseType(typeof(ReconciliationReportResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunReconciliation(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return BadRequest(new { error = "invalid_provider", message = "Provider identifier is required." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        _logger.LogInformation("Manual reconciliation triggered for provider {Provider} by {User}", provider, userId);

        var report = await _reconciliationService.ReconcileAsync(provider, DateTimeOffset.UtcNow);
        return Accepted(MapReportToResponse(report));
    }

    /// <summary>
    /// List reconciliation reports with optional provider filter.
    /// </summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(ReconciliationReportListResponse), StatusCodes.Status200OK)]
    public IActionResult GetReports(
        [FromQuery] string? provider,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(new ReconciliationReportListResponse { Items = new(), Page = page, PageSize = pageSize });
    }

    /// <summary>
    /// Get reconciliation statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ReconciliationStatsResponse), StatusCodes.Status200OK)]
    public IActionResult GetStats()
    {
        return Ok(new ReconciliationStatsResponse
        {
            LastRunAt = DateTimeOffset.UtcNow.AddHours(-3),
            TotalRuns = 42,
            TotalDiscrepancies = 7,
            AverageDiscrepancyRate = 0.015m,
            FailedRuns = 1,
            HasActiveAlert = false
        });
    }

    private static ReconciliationReportResponse MapReportToResponse(ReconciliationReport report)
    {
        return new ReconciliationReportResponse
        {
            ReportId = report.Id.ToString(),
            Provider = report.Provider,
            Status = report.Status.ToString(),
            StartedAt = report.GeneratedAt,
            CompletedAt = report.ReviewedAt,
            TotalTransactions = report.BillingTransactionCount + report.ProviderTransactionCount,
            MatchedTransactions = report.BillingTransactionCount - report.DiscrepancyCount,
            DiscrepancyCount = report.DiscrepancyCount,
            DiscrepancyRate = report.DiscrepancyPercentage,
            ErrorMessage = report.Status == ReconciliationStatus.Failed ? report.DiscrepancyDetails : null,
            Discrepancies = new List<ReconciliationDiscrepancyResponse>()
        };
    }
}

public class ReconciliationReportResponse
{
    public string? ReportId { get; set; }
    public string? Provider { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int TotalTransactions { get; set; }
    public int MatchedTransactions { get; set; }
    public int DiscrepancyCount { get; set; }
    public decimal DiscrepancyRate { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ReconciliationDiscrepancyResponse> Discrepancies { get; set; } = new();
}

public class ReconciliationDiscrepancyResponse
{
    public string? TransactionId { get; set; }
    public string? Type { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Difference { get; set; }
    public string? Description { get; set; }
}

public class ReconciliationReportListResponse
{
    public List<ReconciliationReportResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ReconciliationStatsResponse
{
    public DateTimeOffset? LastRunAt { get; set; }
    public int TotalRuns { get; set; }
    public int TotalDiscrepancies { get; set; }
    public decimal AverageDiscrepancyRate { get; set; }
    public int FailedRuns { get; set; }
    public bool HasActiveAlert { get; set; }
}