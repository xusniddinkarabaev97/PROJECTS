using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers.Admin;

/// <summary>
/// Admin API controller for full CRUD management of filling stations.
/// </summary>
[ApiController]
[Route("api/admin/stations")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin")]
public class FillingStationsController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly ILogger<FillingStationsController> _logger;

    public FillingStationsController(
        GzsBillingDbContext dbContext,
        ILogger<FillingStationsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Lists all filling stations with their stakeholder counts.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "superadmin,manager")]
    [ProducesResponseType(typeof(IEnumerable<FillingStationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var stations = await _dbContext.FillingStations
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new FillingStationDto
            {
                Id = f.Id,
                Name = f.Name,
                Address = f.Address,
                Region = f.Region,
                IsActive = f.IsActive,
                CreatedAt = f.CreatedAt,
                StakeholderCount = _dbContext.Stakeholders.Count(s => s.FillingStationId == f.Id)
            })
            .ToListAsync(ct);

        return Ok(stations);
    }

    /// <summary>
    /// Gets a single filling station by ID, including stakeholder count.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "superadmin,manager")]
    [ProducesResponseType(typeof(FillingStationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var station = await _dbContext.FillingStations
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new FillingStationDto
            {
                Id = f.Id,
                Name = f.Name,
                Address = f.Address,
                Region = f.Region,
                IsActive = f.IsActive,
                CreatedAt = f.CreatedAt,
                StakeholderCount = _dbContext.Stakeholders.Count(s => s.FillingStationId == f.Id)
            })
            .FirstOrDefaultAsync(ct);

        if (station is null)
            return NotFound(new { error = "NOT_FOUND", message = "Filling station not found." });

        return Ok(station);
    }

    /// <summary>
    /// Creates a new filling station.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FillingStationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFillingStationRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "INVALID_NAME", message = "Station name is required." });

        var station = new FillingStation
        {
            Name = request.Name.Trim(),
            Address = request.Address?.Trim() ?? string.Empty,
            Region = request.Region?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.FillingStations.Add(station);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Filling station {Id} '{Name}' created", station.Id, station.Name);

        var dto = new FillingStationDto
        {
            Id = station.Id,
            Name = station.Name,
            Address = station.Address,
            Region = station.Region,
            IsActive = station.IsActive,
            CreatedAt = station.CreatedAt,
            StakeholderCount = 0
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Updates an existing filling station.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FillingStationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CreateFillingStationRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "INVALID_NAME", message = "Station name is required." });

        var station = await _dbContext.FillingStations
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (station is null)
            return NotFound(new { error = "NOT_FOUND", message = "Filling station not found." });

        station.Name = request.Name.Trim();
        station.Address = request.Address?.Trim() ?? string.Empty;
        station.Region = request.Region?.Trim() ?? string.Empty;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Filling station {Id} '{Name}' updated", station.Id, station.Name);

        var stakeholderCount = await _dbContext.Stakeholders
            .CountAsync(s => s.FillingStationId == station.Id, ct);

        var dto = new FillingStationDto
        {
            Id = station.Id,
            Name = station.Name,
            Address = station.Address,
            Region = station.Region,
            IsActive = station.IsActive,
            CreatedAt = station.CreatedAt,
            StakeholderCount = stakeholderCount
        };

        return Ok(dto);
    }

    /// <summary>
    /// Soft-deletes a filling station by setting IsActive to false.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var station = await _dbContext.FillingStations
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (station is null)
            return NotFound(new { error = "NOT_FOUND", message = "Filling station not found." });

        station.IsActive = false;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Filling station {Id} '{Name}' deactivated", station.Id, station.Name);

        return NoContent();
    }

    /// <summary>
    /// Reactivates a soft-deleted filling station.
    /// </summary>
    [HttpPost("{id:int}/activate")]
    [ProducesResponseType(typeof(FillingStationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        var station = await _dbContext.FillingStations
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (station is null)
            return NotFound(new { error = "NOT_FOUND", message = "Filling station not found." });

        station.IsActive = true;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Filling station {Id} '{Name}' reactivated", station.Id, station.Name);

        var stakeholderCount = await _dbContext.Stakeholders
            .CountAsync(s => s.FillingStationId == station.Id, ct);

        var dto = new FillingStationDto
        {
            Id = station.Id,
            Name = station.Name,
            Address = station.Address,
            Region = station.Region,
            IsActive = station.IsActive,
            CreatedAt = station.CreatedAt,
            StakeholderCount = stakeholderCount
        };

        return Ok(dto);
    }
}
