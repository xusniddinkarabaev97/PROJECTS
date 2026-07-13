using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using GzsBilling.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers.Admin;

/// <summary>
/// Admin API controller for managing system settings as key-value pairs.
/// </summary>
[ApiController]
[Route("api/admin/settings")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly ISystemSettingService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        GzsBillingDbContext dbContext,
        ISystemSettingService settingsService,
        ILogger<SettingsController> logger)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all system settings.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SystemSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var settings = await _dbContext.SystemSettings
            .AsNoTracking()
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .Select(s => new SystemSettingDto
            {
                Id = s.Id,
                Key = s.Key,
                Value = s.Value,
                Category = s.Category,
                Description = s.Description,
                UpdatedAt = s.UpdatedAt,
                UpdatedBy = s.UpdatedBy
            })
            .ToListAsync(ct);

        return Ok(settings);
    }

    /// <summary>
    /// Gets a single system setting by its key.
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(SystemSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
    {
        var setting = await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        if (setting is null)
            return NotFound(new { error = "NOT_FOUND", message = $"Setting '{key}' not found." });

        var dto = new SystemSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            Category = setting.Category,
            Description = setting.Description,
            UpdatedAt = setting.UpdatedAt,
            UpdatedBy = setting.UpdatedBy
        };

        return Ok(dto);
    }

    /// <summary>
    /// Updates (or creates) a system setting value.
    /// </summary>
    [HttpPut("{key}")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(typeof(SystemSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        string key,
        [FromBody] UpdateSettingRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return BadRequest(new { error = "INVALID_VALUE", message = "Setting value is required." });

        await _settingsService.SetSettingValueAsync(
            key,
            request.Value,
            string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category,
            request.Description ?? string.Empty,
            "admin",
            ct);

        _logger.LogInformation("Setting '{Key}' updated", key);

        var setting = await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        var dto = new SystemSettingDto
        {
            Id = setting?.Id ?? Guid.Empty,
            Key = key,
            Value = request.Value,
            Category = request.Category,
            Description = request.Description ?? string.Empty,
            UpdatedAt = setting?.UpdatedAt ?? DateTimeOffset.UtcNow,
            UpdatedBy = setting?.UpdatedBy ?? "admin"
        };

        return Ok(dto);
    }

    /// <summary>
    /// Lists all distinct setting categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _dbContext.SystemSettings
            .AsNoTracking()
            .Select(s => s.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

        return Ok(categories);
    }
}
