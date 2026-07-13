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
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly BillingDbContext _db;

    private static readonly List<User> DevUsers = new()
    {
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Username = "superadmin",
            Email = "superadmin@billing.uz",
            PasswordHash = HashPassword("Admin123!"),
            FullName = "Super Administrator",
            Role = SystemRole.SuperAdmin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Username = "admin",
            Email = "admin@billing.uz",
            PasswordHash = HashPassword("Admin123!"),
            FullName = "Administrator",
            Role = SystemRole.Admin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Username = "manager",
            Email = "manager@billing.uz",
            PasswordHash = HashPassword("Manager123!"),
            FullName = "Manager User",
            Role = SystemRole.Manager,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            Username = "operator",
            Email = "operator@billing.uz",
            PasswordHash = HashPassword("Operator123!"),
            FullName = "Operator User",
            Role = SystemRole.Operator,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
            Username = "shareholder",
            Email = "shareholder@billing.uz",
            PasswordHash = HashPassword("Shareholder123!"),
            FullName = "Shareholder User",
            Role = SystemRole.Shareholder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
            Username = "readonly",
            Email = "readonly@billing.uz",
            PasswordHash = HashPassword("Readonly123!"),
            FullName = "Read Only User",
            Role = SystemRole.ReadOnly,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
            Username = "meningshaxrim",
            Email = "operator2@billing.uz",
            PasswordHash = HashPassword("123456789$$ff__"),
            FullName = "Operator Shahrim",
            Role = SystemRole.Operator,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        }
    };

    public UsersController(ILogger<UsersController> logger, BillingDbContext db)
    {
        _logger = logger;
        _db = db;

        // Seed dev users if the database is empty
        if (!_db.Users.Any())
        {
            _db.Users.AddRange(DevUsers);
            _db.SaveChanges();
        }
    }

    /// <summary>
    /// List all users (Admin only)
    /// </summary>
    [HttpGet]
    [RequirePermission(Permission.UsersView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users.ToListAsync();

        var result = users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.FullName,
            role = u.Role.ToString(),
            u.IsActive,
            u.CreatedAt,
            u.LastLoginAt,
            u.DeactivatedAt
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.UsersView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { error = "user_not_found", message = $"No user found with ID '{id}'." });
        }

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            user.DeactivatedAt
        });
    }

    /// <summary>
    /// Create a new user. Manager+ can create Operators/ReadOnly. Admin creates all roles.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permission.UsersCreate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { error = "validation_error", message = "Username is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "validation_error", message = "Password is required." });
        }

        var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<SystemRole>(currentUserRole, out var callerRole))
        {
            return Forbid();
        }

        if (!Enum.TryParse<SystemRole>(request.Role, out var newUserRole))
        {
            return BadRequest(new { error = "validation_error", message = $"Invalid role: '{request.Role}'." });
        }

        if (callerRole == SystemRole.Manager)
        {
            if (newUserRole != SystemRole.Operator && newUserRole != SystemRole.ReadOnly)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "forbidden", message = "Managers can only create Operator or ReadOnly users." });
            }
        }
        else if (callerRole != SystemRole.SuperAdmin && callerRole != SystemRole.Admin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "forbidden", message = "Only Admin and above can create users." });
        }

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
        {
            return Conflict(new { error = "duplicate", message = $"Username '{request.Username}' already exists." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email ?? string.Empty,
            PasswordHash = HashPassword(request.Password),
            FullName = request.FullName ?? string.Empty,
            Role = newUserRole,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User created: Id={UserId}, Username={Username}, Role={Role}, By={By}",
            user.Id, user.Username, user.Role, userId);

        return StatusCode(StatusCodes.Status201Created, new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt
        });
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.UsersEdit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { error = "user_not_found", message = $"No user found with ID '{id}'." });
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
            user.Username = request.Username;
        if (request.Email != null)
            user.Email = request.Email;
        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = HashPassword(request.Password);
        if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<SystemRole>(request.Role, out var newRole))
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (Enum.TryParse<SystemRole>(currentUserRole, out var callerRole))
            {
                if (callerRole == SystemRole.Manager && newRole != SystemRole.Operator && newRole != SystemRole.ReadOnly)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { error = "forbidden", message = "Managers can only set Operator or ReadOnly roles." });
                }
            }
            user.Role = newRole;
        }

        await _db.SaveChangesAsync();

        var updaterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User updated: Id={UserId}, Username={Username}, By={By}",
            user.Id, user.Username, updaterId);

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            user.DeactivatedAt
        });
    }

    /// <summary>
    /// Deactivate a user (soft delete)
    /// </summary>
    [HttpPut("{id:guid}/deactivate")]
    [RequirePermission(Permission.UsersDeactivate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { error = "user_not_found", message = $"No user found with ID '{id}'." });
        }

        if (user.Role == SystemRole.SuperAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "forbidden", message = "Cannot deactivate the SuperAdmin account." });
        }

        user.IsActive = false;
        user.DeactivatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User deactivated: Id={UserId}, Username={Username}, By={By}",
            user.Id, user.Username, userId);

        return Ok(new
        {
            user.Id,
            user.Username,
            user.IsActive,
            user.DeactivatedAt,
            message = $"User '{user.Username}' deactivated successfully."
        });
    }

    private static string HashPassword(string password)
    {
        var salt = new byte[16];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt)));
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }
}

public class CreateUserRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Username { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Password { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Role { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? FullName { get; set; }
}

public class UpdateUserRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
}
