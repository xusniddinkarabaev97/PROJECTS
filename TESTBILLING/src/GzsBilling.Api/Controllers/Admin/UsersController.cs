using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Authorize(Roles = "superadmin")]
[ApiExplorerSettings(GroupName = "Admin")]
public class UsersController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        GzsBillingDbContext dbContext,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Lists all users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    /// <summary>
    /// Gets a single user by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return NotFound(new { error = "NOT_FOUND", message = "User not found." });

        return Ok(user);
    }

    /// <summary>
    /// Creates a new user (superadmin only).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "INVALID_DATA", message = "Username and password are required." });

        if (request.Username.Trim().Length < 3)
            return BadRequest(new { error = "INVALID_USERNAME", message = "Username must be at least 3 characters." });

        if (request.Password.Length < 6)
            return BadRequest(new { error = "INVALID_PASSWORD", message = "Password must be at least 6 characters." });

        if (request.Role != "superadmin" && request.Role != "manager")
            return BadRequest(new { error = "INVALID_ROLE", message = "Role must be 'superadmin' or 'manager'." });

        var exists = await _dbContext.Users
            .AnyAsync(u => u.Username == request.Username.Trim(), ct);

        if (exists)
            return Conflict(new { error = "DUPLICATE", message = "A user with this username already exists." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName?.Trim() ?? string.Empty,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("User '{Username}' ({Role}) created by admin", user.Username, user.Role);

        var dto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Toggles a user's active status (activate/deactivate).
    /// </summary>
    [HttpPut("{id:guid}/toggle")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return NotFound(new { error = "NOT_FOUND", message = "User not found." });

        user.IsActive = !user.IsActive;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("User '{Username}' active status toggled to {Status}", user.Username, user.IsActive);

        var dto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

        return Ok(dto);
    }
}
