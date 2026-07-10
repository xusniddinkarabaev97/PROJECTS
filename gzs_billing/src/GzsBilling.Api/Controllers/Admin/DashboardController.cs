using GzsBilling.Domain.Enums;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers.Admin;

/// <summary>
/// Admin API controller for dashboard summary statistics.
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;

    public DashboardController(GzsBillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns aggregate dashboard statistics for the admin panel.
    /// Includes total filling stations, stakeholders, transactions,
    /// today's total amount, and pending disbursements.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "superadmin,manager")]
    [ProducesResponseType(typeof(DashboardStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        var totalStations = await _dbContext.FillingStations
            .CountAsync(f => f.IsActive, ct);

        var totalStakeholders = await _dbContext.Stakeholders
            .CountAsync(ct);

        var totalTransactions = await _dbContext.Tranzaktsiyalar
            .CountAsync(ct);

        var todayTotal = await _dbContext.Tranzaktsiyalar
            .Where(t => t.CreatedAt >= todayStart && t.Status == TranzaksiyaStatus.Completed)
            .SumAsync(t => (decimal?)t.TotalSum, ct) ?? 0m;

        var pendingDisbursements = await _dbContext.DisbursementTarixi
            .CountAsync(d => d.Status == DisbursementStatus.Pending, ct);

        var stats = new DashboardStats
        {
            TotalFillingStations = totalStations,
            TotalStakeholders = totalStakeholders,
            TotalTransactions = totalTransactions,
            TodayTotalAmount = todayTotal,
            PendingDisbursements = pendingDisbursements
        };

        return Ok(stats);
    }
}
