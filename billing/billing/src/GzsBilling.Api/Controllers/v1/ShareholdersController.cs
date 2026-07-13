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
public class ShareholdersController : ControllerBase
{
    private readonly ILogger<ShareholdersController> _logger;
    private readonly BillingDbContext _db;

    public ShareholdersController(ILogger<ShareholdersController> logger, BillingDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// List all shareholders (Admin only)
    /// </summary>
    [HttpGet]
    [RequirePermission(Permission.ShareholdersView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var shareholders = await _db.Shareholders.ToListAsync();

        var result = shareholders.Select(s => new
        {
            s.Id,
            s.FullName,
            s.Phone,
            s.Email,
            s.TaxId,
            s.OwnershipPercentage,
            s.Address,
            s.IsActive,
            s.CreatedAt,
            s.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get shareholder by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ShareholdersView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var shareholder = await _db.Shareholders.FirstOrDefaultAsync(s => s.Id == id);
        if (shareholder == null)
        {
            return NotFound(new { error = "shareholder_not_found", message = $"No shareholder found with ID '{id}'." });
        }

        return Ok(new
        {
            shareholder.Id,
            shareholder.FullName,
            shareholder.Phone,
            shareholder.Email,
            shareholder.TaxId,
            shareholder.OwnershipPercentage,
            shareholder.Address,
            shareholder.IsActive,
            shareholder.CreatedAt,
            shareholder.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new shareholder (Admin only)
    /// </summary>
    [HttpPost]
    [RequirePermission(Permission.ShareholdersCreate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateShareholderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { error = "validation_error", message = "Full name is required." });
        }

        if (request.OwnershipPercentage < 0 || request.OwnershipPercentage > 100)
        {
            return BadRequest(new { error = "validation_error", message = "Ownership percentage must be between 0 and 100." });
        }

        var shareholder = new Shareholder
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone ?? string.Empty,
            Email = request.Email ?? string.Empty,
            TaxId = request.TaxId ?? string.Empty,
            OwnershipPercentage = request.OwnershipPercentage,
            Address = request.Address ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Shareholders.Add(shareholder);
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Shareholder created: Id={ShareholderId}, Name={FullName}, By={UserId}",
            shareholder.Id, shareholder.FullName, userId);

        return StatusCode(StatusCodes.Status201Created, new
        {
            shareholder.Id,
            shareholder.FullName,
            shareholder.Phone,
            shareholder.Email,
            shareholder.TaxId,
            shareholder.OwnershipPercentage,
            shareholder.Address,
            shareholder.IsActive,
            shareholder.CreatedAt
        });
    }

    /// <summary>
    /// Update an existing shareholder (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.ShareholdersEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShareholderRequest request)
    {
        if (request.OwnershipPercentage.HasValue &&
            (request.OwnershipPercentage.Value < 0 || request.OwnershipPercentage.Value > 100))
        {
            return BadRequest(new { error = "validation_error", message = "Ownership percentage must be between 0 and 100." });
        }

        var shareholder = await _db.Shareholders.FirstOrDefaultAsync(s => s.Id == id);
        if (shareholder == null)
        {
            return NotFound(new { error = "shareholder_not_found", message = $"No shareholder found with ID '{id}'." });
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
            shareholder.FullName = request.FullName;
        if (request.Phone != null)
            shareholder.Phone = request.Phone;
        if (request.Email != null)
            shareholder.Email = request.Email;
        if (request.TaxId != null)
            shareholder.TaxId = request.TaxId;
        if (request.OwnershipPercentage.HasValue)
            shareholder.OwnershipPercentage = request.OwnershipPercentage.Value;
        if (request.Address != null)
            shareholder.Address = request.Address;
        if (request.IsActive.HasValue)
            shareholder.IsActive = request.IsActive.Value;

        shareholder.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Shareholder updated: Id={ShareholderId}, Name={FullName}, By={UserId}",
            shareholder.Id, shareholder.FullName, userId);

        return Ok(new
        {
            shareholder.Id,
            shareholder.FullName,
            shareholder.Phone,
            shareholder.Email,
            shareholder.TaxId,
            shareholder.OwnershipPercentage,
            shareholder.Address,
            shareholder.IsActive,
            shareholder.CreatedAt,
            shareholder.UpdatedAt
        });
    }

    /// <summary>
    /// Delete a shareholder (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ShareholdersDelete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var shareholder = await _db.Shareholders.FirstOrDefaultAsync(s => s.Id == id);
        if (shareholder == null)
        {
            return NotFound(new { error = "shareholder_not_found", message = $"No shareholder found with ID '{id}'." });
        }

        _db.Shareholders.Remove(shareholder);
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Shareholder deleted: Id={ShareholderId}, Name={FullName}, By={UserId}",
            id, shareholder.FullName, userId);

        return Ok(new { message = $"Shareholder '{shareholder.FullName}' deleted successfully." });
    }
}

public class CreateShareholderRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string FullName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0, 100)]
    public decimal OwnershipPercentage { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxId { get; set; }
    public string? Address { get; set; }
}

public class UpdateShareholderRequest
{
    public string? FullName { get; set; }
    public decimal? OwnershipPercentage { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxId { get; set; }
    public string? Address { get; set; }
    public bool? IsActive { get; set; }
}
