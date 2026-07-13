using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GzsBilling.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Auth")]
public class AuthController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        GzsBillingDbContext dbContext,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "INVALID_CREDENTIALS", message = "Username and password are required." });

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username.Trim() && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user '{Username}'", request.Username);
            return Unauthorized(new { error = "INVALID_CREDENTIALS", message = "Invalid username or password." });
        }

        var token = GenerateJwtToken(user);

        _logger.LogInformation("User '{Username}' logged in successfully", user.Username);

        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        });
    }

    /// <summary>
    /// Creates a new user. Only superadmins can register new users.
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
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

        _logger.LogInformation("User '{Username}' ({Role}) created", user.Username, user.Role);

        return CreatedAtAction(nameof(Register), new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "gzs-billing-super-secret-key-2024-min-32-chars!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
        };
        var token = new JwtSecurityToken(
            issuer: "gzs-billing",
            audience: "gzs-billing-admin",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
