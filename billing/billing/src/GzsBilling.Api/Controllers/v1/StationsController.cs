using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using GzsBilling.Api.Authorization;
using GzsBilling.Infrastructure.Persistence;

namespace GzsBilling.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class StationsController : ControllerBase
{
    private readonly ILogger<StationsController> _logger;
    private readonly BillingDbContext _db;

    public StationsController(ILogger<StationsController> logger, BillingDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// List all gas stations
    /// </summary>
    [HttpGet]
    [RequirePermission(Permission.StationsView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var stations = await _db.Stations.Include(s => s.Columns).ToListAsync();

        var result = stations.Select(s => new
        {
            s.Id,
            s.Name,
            s.Location,
            s.Address,
            s.ContactPhone,
            s.IsActive,
            s.CreatedAt,
            s.UpdatedAt,
            columnCount = s.Columns.Count
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get station by ID with columns
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.StationsView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var station = await _db.Stations
            .Include(s => s.Columns)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (station == null)
        {
            return NotFound(new { error = "station_not_found", message = $"No station found with ID '{id}'." });
        }

        return Ok(new
        {
            station.Id,
            station.Name,
            station.Location,
            station.Address,
            station.ContactPhone,
            station.IsActive,
            station.CreatedAt,
            station.UpdatedAt,
            columns = station.Columns.Select(c => new
            {
                c.Id,
                c.StationId,
                c.ColumnNumber,
                c.Name,
                c.FuelType,
                c.PricePerLiter,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt,
                qrCodeUrl = $"/pay/{c.Id}"
            }).ToList()
        });
    }

    /// <summary>
    /// Get all columns for a station
    /// </summary>
    [HttpGet("{id:guid}/columns")]
    [RequirePermission(Permission.ColumnsView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetColumns(Guid id)
    {
        var station = await _db.Stations
            .Include(s => s.Columns)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (station == null)
            return NotFound(new { error = "station_not_found", message = $"Station with ID '{id}' not found." });

        var columns = station.Columns.Select(c => new
        {
            c.Id,
            c.StationId,
            c.ColumnNumber,
            c.Name,
            c.FuelType,
            c.PricePerLiter,
            c.IsActive,
            c.CreatedAt,
            c.UpdatedAt,
            qrCodeUrl = $"/pay/{c.Id}"
        }).ToList();

        return Ok(columns);
    }

    /// <summary>
    /// Create a new gas station (Admin only)
    /// </summary>
    [HttpPost]
    [RequirePermission(Permission.StationsCreate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "validation_error", message = "Station name is required." });
        }

        var station = new Station
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Location = request.Location ?? string.Empty,
            Address = request.Address ?? string.Empty,
            ContactPhone = request.ContactPhone ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Stations.Add(station);
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Station created: Id={StationId}, Name={Name}, By={UserId}", station.Id, station.Name, userId);

        return StatusCode(StatusCodes.Status201Created, new
        {
            station.Id,
            station.Name,
            station.Location,
            station.Address,
            station.ContactPhone,
            station.IsActive,
            station.CreatedAt
        });
    }

    /// <summary>
    /// Update an existing gas station
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.StationsEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStationRequest request)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == id);
        if (station == null)
        {
            return NotFound(new { error = "station_not_found", message = $"No station found with ID '{id}'." });
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            station.Name = request.Name;
        if (request.Location != null)
            station.Location = request.Location;
        if (request.Address != null)
            station.Address = request.Address;
        if (request.ContactPhone != null)
            station.ContactPhone = request.ContactPhone;
        if (request.IsActive.HasValue)
            station.IsActive = request.IsActive.Value;

        station.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Station updated: Id={StationId}, Name={Name}, By={UserId}", station.Id, station.Name, userId);

        return Ok(new
        {
            station.Id,
            station.Name,
            station.Location,
            station.Address,
            station.ContactPhone,
            station.IsActive,
            station.CreatedAt,
            station.UpdatedAt
        });
    }

    /// <summary>
    /// Delete a gas station (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.StationsDelete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == id);
        if (station == null)
        {
            return NotFound(new { error = "station_not_found", message = $"No station found with ID '{id}'." });
        }

        _db.Stations.Remove(station);
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Station deleted: Id={StationId}, Name={Name}, By={UserId}", id, station.Name, userId);

        return Ok(new { message = $"Station '{station.Name}' deleted successfully." });
    }

    /// <summary>
    /// Add a column to a station
    /// </summary>
    [HttpPost("{id:guid}/columns")]
    [RequirePermission(Permission.ColumnsCreate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddColumn(Guid id, [FromBody] CreateColumnRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ColumnNumber))
            {
                return BadRequest(new { error = "validation_error", message = "Column number is required." });
            }

            var station = await _db.Stations.FirstOrDefaultAsync(s => s.Id == id);

            if (station == null)
            {
                return NotFound(new { error = "station_not_found", message = $"No station found with ID '{id}'." });
            }

            var column = new Column
            {
                Id = Guid.NewGuid(),
                StationId = id,
                Name = request.Name ?? request.ColumnNumber,
                ColumnNumber = request.ColumnNumber,
                FuelType = request.FuelType ?? string.Empty,
                PricePerLiter = request.PricePerLiter,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system"
            };

            _db.Columns.Add(column);
            await _db.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("Column added: StationId={StationId}, ColumnId={ColumnId}, Number={ColumnNumber}, By={UserId}",
                id, column.Id, column.ColumnNumber, userId);

            return StatusCode(StatusCodes.Status201Created, new
            {
                column.Id,
                column.StationId,
                column.Name,
                column.ColumnNumber,
                column.FuelType,
                column.PricePerLiter,
                column.IsActive,
                column.CreatedAt,
                qrCodeUrl = $"/pay/{column.Id}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add column to station {StationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = ex.GetType().Name,
                message = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Update a column
    /// </summary>
    [HttpPut("{id:guid}/columns/{columnId:guid}")]
    [RequirePermission(Permission.ColumnsEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateColumn(Guid id, Guid columnId, [FromBody] UpdateColumnRequest request)
    {
        var station = await _db.Stations
            .Include(s => s.Columns)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (station == null)
        {
            return NotFound(new { error = "station_not_found", message = $"No station found with ID '{id}'." });
        }

        var column = station.Columns.FirstOrDefault(c => c.Id == columnId);
        if (column == null)
        {
            return NotFound(new { error = "column_not_found", message = $"No column found with ID '{columnId}'." });
        }

        if (!string.IsNullOrWhiteSpace(request.ColumnNumber))
            column.ColumnNumber = request.ColumnNumber;
        if (request.FuelType != null)
            column.FuelType = request.FuelType;
        if (request.PricePerLiter.HasValue)
            column.PricePerLiter = request.PricePerLiter.Value;
        if (request.IsActive.HasValue)
            column.IsActive = request.IsActive.Value;

        column.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Column updated: StationId={StationId}, ColumnId={ColumnId}, By={UserId}",
            id, columnId, userId);

        return Ok(new
        {
            column.Id,
            column.StationId,
            column.ColumnNumber,
            column.FuelType,
            column.PricePerLiter,
            column.IsActive,
            column.CreatedAt,
            column.UpdatedAt
        });
    }

    /// <summary>
    /// Delete a column
    /// </summary>
    [HttpDelete("{id:guid}/columns/{columnId:guid}")]
    [RequirePermission(Permission.ColumnsDelete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteColumn(Guid id, Guid columnId)
    {
        var station = await _db.Stations
            .Include(s => s.Columns)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (station == null)
        {
            return NotFound(new { error = "station_not_found", message = $"No station found with ID '{id}'." });
        }

        var column = station.Columns.FirstOrDefault(c => c.Id == columnId);
        if (column == null)
        {
            return NotFound(new { error = "column_not_found", message = $"No column found with ID '{columnId}'." });
        }

        station.Columns.Remove(column);
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Column deleted: StationId={StationId}, ColumnId={ColumnId}, By={UserId}",
            id, columnId, userId);

        return Ok(new { message = $"Column '{column.ColumnNumber}' deleted successfully." });
    }
}

public class CreateStationRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
}

public class UpdateStationRequest
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateColumnRequest
{
    public string? Name { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public string ColumnNumber { get; set; } = string.Empty;
    public string? FuelType { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue)]
    public decimal PricePerLiter { get; set; }
}

public class UpdateColumnRequest
{
    public string? ColumnNumber { get; set; }
    public string? FuelType { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue)]
    public decimal? PricePerLiter { get; set; }
    public bool? IsActive { get; set; }
}
