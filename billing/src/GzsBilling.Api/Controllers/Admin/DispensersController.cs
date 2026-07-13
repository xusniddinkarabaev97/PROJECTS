using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/stations/{stationId}/dispensers")]
[Authorize(Roles = "superadmin")]
[ApiExplorerSettings(GroupName = "Admin")]
public class DispensersController : ControllerBase
{
    private readonly GzsBillingDbContext _db;

    public DispensersController(GzsBillingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int stationId)
    {
        var station = await _db.FillingStations.FindAsync(stationId);
        if (station is null)
            return NotFound(new { message = "Station not found" });

        var dispensers = await _db.Dispensers
            .Where(d => d.FillingStationId == stationId)
            .OrderBy(d => d.Id)
            .Select(d => new DispenserDto
            {
                Id = d.Id,
                FillingStationId = d.FillingStationId,
                Name = d.Name,
                FuelType = d.FuelType,
                IsActive = d.IsActive,
            })
            .ToListAsync();

        return Ok(dispensers);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int stationId, [FromBody] CreateDispenserRequest request)
    {
        var station = await _db.FillingStations.FindAsync(stationId);
        if (station is null)
            return NotFound(new { message = "Station not found" });

        var dispenser = new Dispenser
        {
            FillingStationId = stationId,
            Name = request.Name,
            FuelType = request.FuelType,
            IsActive = true,
        };

        _db.Dispensers.Add(dispenser);
        await _db.SaveChangesAsync();

        var dto = new DispenserDto
        {
            Id = dispenser.Id,
            FillingStationId = dispenser.FillingStationId,
            Name = dispenser.Name,
            FuelType = dispenser.FuelType,
            IsActive = dispenser.IsActive,
        };

        return CreatedAtAction(nameof(GetAll), new { stationId }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int stationId, int id, [FromBody] CreateDispenserRequest request)
    {
        var dispenser = await _db.Dispensers
            .FirstOrDefaultAsync(d => d.Id == id && d.FillingStationId == stationId);

        if (dispenser is null)
            return NotFound(new { message = "Dispenser not found" });

        dispenser.Name = request.Name;
        dispenser.FuelType = request.FuelType;

        await _db.SaveChangesAsync();

        return Ok(new DispenserDto
        {
            Id = dispenser.Id,
            FillingStationId = dispenser.FillingStationId,
            Name = dispenser.Name,
            FuelType = dispenser.FuelType,
            IsActive = dispenser.IsActive,
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int stationId, int id)
    {
        var dispenser = await _db.Dispensers
            .FirstOrDefaultAsync(d => d.Id == id && d.FillingStationId == stationId);

        if (dispenser is null)
            return NotFound(new { message = "Dispenser not found" });

        dispenser.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
