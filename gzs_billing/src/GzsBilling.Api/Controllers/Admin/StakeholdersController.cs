using System.ComponentModel.DataAnnotations;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/stakeholders")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin")]
public class StakeholdersController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly ILogger<StakeholdersController> _logger;

    public StakeholdersController(
        GzsBillingDbContext dbContext,
        ILogger<StakeholdersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "superadmin,manager")]
    [ProducesResponseType(typeof(IEnumerable<StakeholderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var stakeholders = await _dbContext.Stakeholders
            .AsNoTracking()
            .Join(_dbContext.FillingStations,
                s => s.FillingStationId,
                f => f.Id,
                (s, f) => new StakeholderDto
                {
                    Id = s.Id,
                    FillingStationId = s.FillingStationId,
                    FillingStationName = f.Name,
                    PaymentId = s.PaymentId,
                    BankAccount = s.BankAccount,
                    SharePercent = s.SharePercent,
                    FullName = s.FullName
                })
            .OrderBy(s => s.FullName)
            .ToListAsync(ct);

        return Ok(stakeholders);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "superadmin,manager")]
    [ProducesResponseType(typeof(StakeholderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var stakeholder = await _dbContext.Stakeholders
            .AsNoTracking()
            .Join(_dbContext.FillingStations,
                s => s.FillingStationId,
                f => f.Id,
                (s, f) => new StakeholderDto
                {
                    Id = s.Id,
                    FillingStationId = s.FillingStationId,
                    FillingStationName = f.Name,
                    PaymentId = s.PaymentId,
                    BankAccount = s.BankAccount,
                    SharePercent = s.SharePercent,
                    FullName = s.FullName
                })
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (stakeholder is null)
            return NotFound(new { error = "NOT_FOUND", message = "Stakeholder not found." });

        return Ok(stakeholder);
    }

    [HttpGet("by-station/{stationId:int}")]
    [Authorize(Roles = "superadmin,manager")]
    [ProducesResponseType(typeof(IEnumerable<StakeholderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStation(int stationId, CancellationToken ct)
    {
        var stakeholders = await _dbContext.Stakeholders
            .AsNoTracking()
            .Join(_dbContext.FillingStations,
                s => s.FillingStationId,
                f => f.Id,
                (s, f) => new StakeholderDto
                {
                    Id = s.Id,
                    FillingStationId = s.FillingStationId,
                    FillingStationName = f.Name,
                    PaymentId = s.PaymentId,
                    BankAccount = s.BankAccount,
                    SharePercent = s.SharePercent,
                    FullName = s.FullName
                })
            .Where(s => s.FillingStationId == stationId)
            .OrderBy(s => s.FullName)
            .ToListAsync(ct);

        return Ok(stakeholders);
    }

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(typeof(StakeholderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStakeholderRequest request,
        CancellationToken ct)
    {
        if (request.SharePercent <= 0 || request.SharePercent > 100)
            return BadRequest(new { error = "INVALID_SHARE", message = "SharePercent must be between 0 and 100." });

        var stationExists = await _dbContext.FillingStations
            .AnyAsync(f => f.Id == request.FillingStationId, ct);
        if (!stationExists)
            return NotFound(new { error = "STATION_NOT_FOUND", message = "Filling station not found." });

        var paymentExists = await _dbContext.Payments
            .AnyAsync(p => p.Id == request.PaymentId, ct);
        if (!paymentExists)
            return NotFound(new { error = "PAYMENT_NOT_FOUND", message = "Payment provider not found." });

        var duplicate = await _dbContext.Stakeholders
            .AnyAsync(s => s.FillingStationId == request.FillingStationId && s.PaymentId == request.PaymentId, ct);
        if (duplicate)
            return BadRequest(new { error = "DUPLICATE", message = "A stakeholder already exists for this station and payment type." });

        var stakeholder = new Stakeholder
        {
            Id = Guid.NewGuid(),
            FillingStationId = request.FillingStationId,
            PaymentId = request.PaymentId,
            BankAccount = request.BankAccount,
            SharePercent = request.SharePercent,
            FullName = request.FullName
        };

        _dbContext.Stakeholders.Add(stakeholder);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Stakeholder {Id} created for station {Station}", stakeholder.Id, stakeholder.FillingStationId);

        var stationName = await _dbContext.FillingStations
            .Where(f => f.Id == request.FillingStationId)
            .Select(f => f.Name)
            .FirstOrDefaultAsync(ct);

        var dto = new StakeholderDto
        {
            Id = stakeholder.Id,
            FillingStationId = stakeholder.FillingStationId,
            FillingStationName = stationName ?? string.Empty,
            PaymentId = stakeholder.PaymentId,
            BankAccount = stakeholder.BankAccount,
            SharePercent = stakeholder.SharePercent,
            FullName = stakeholder.FullName
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(typeof(StakeholderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CreateStakeholderRequest request,
        CancellationToken ct)
    {
        if (request.SharePercent <= 0 || request.SharePercent > 100)
            return BadRequest(new { error = "INVALID_SHARE", message = "SharePercent must be between 0 and 100." });

        var stakeholder = await _dbContext.Stakeholders
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (stakeholder is null)
            return NotFound(new { error = "NOT_FOUND", message = "Stakeholder not found." });

        if (stakeholder.FillingStationId != request.FillingStationId || stakeholder.PaymentId != request.PaymentId)
        {
            var paymentExists = await _dbContext.Payments
                .AnyAsync(p => p.Id == request.PaymentId, ct);
            if (!paymentExists)
                return NotFound(new { error = "PAYMENT_NOT_FOUND", message = "Payment provider not found." });

            var stationExists = await _dbContext.FillingStations
                .AnyAsync(f => f.Id == request.FillingStationId, ct);
            if (!stationExists)
                return NotFound(new { error = "STATION_NOT_FOUND", message = "Filling station not found." });

            var duplicate = await _dbContext.Stakeholders
                .AnyAsync(s => s.Id != id && s.FillingStationId == request.FillingStationId && s.PaymentId == request.PaymentId, ct);
            if (duplicate)
                return BadRequest(new { error = "DUPLICATE", message = "Another stakeholder already exists for this station and payment type." });
        }

        stakeholder.FillingStationId = request.FillingStationId;
        stakeholder.PaymentId = request.PaymentId;
        stakeholder.BankAccount = request.BankAccount;
        stakeholder.SharePercent = request.SharePercent;
        stakeholder.FullName = request.FullName;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Stakeholder {Id} updated", id);

        var stationName = await _dbContext.FillingStations
            .Where(f => f.Id == request.FillingStationId)
            .Select(f => f.Name)
            .FirstOrDefaultAsync(ct);

        var dto = new StakeholderDto
        {
            Id = stakeholder.Id,
            FillingStationId = stakeholder.FillingStationId,
            FillingStationName = stationName ?? string.Empty,
            PaymentId = stakeholder.PaymentId,
            BankAccount = stakeholder.BankAccount,
            SharePercent = stakeholder.SharePercent,
            FullName = stakeholder.FullName
        };

        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var stakeholder = await _dbContext.Stakeholders
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (stakeholder is null)
            return NotFound(new { error = "NOT_FOUND", message = "Stakeholder not found." });

        _dbContext.Stakeholders.Remove(stakeholder);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Stakeholder {Id} deleted", id);

        return NoContent();
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<StakeholderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<StakeholderDto>());

        var query = q.Trim().ToLower();

        var stakeholders = await _dbContext.Stakeholders
            .AsNoTracking()
            .Where(s => s.FullName.ToLower().Contains(query))
            .Join(_dbContext.FillingStations,
                s => s.FillingStationId,
                f => f.Id,
                (s, f) => new StakeholderDto
                {
                    Id = s.Id,
                    FillingStationId = s.FillingStationId,
                    FillingStationName = f.Name,
                    PaymentId = s.PaymentId,
                    BankAccount = s.BankAccount,
                    SharePercent = s.SharePercent,
                    FullName = s.FullName
                })
            .OrderBy(s => s.FullName)
            .ToListAsync(ct);

        return Ok(stakeholders);
    }
}
