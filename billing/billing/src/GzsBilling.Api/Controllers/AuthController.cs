using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GzsBilling.Domain.Enums;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Api.Authorization;
using GzsBilling.Infrastructure.Persistence;

namespace GzsBilling.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly BillingDbContext _db;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger, BillingDbContext db)
    {
        _configuration = configuration;
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// Login and get JWT token with role-based permissions
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == request.Username && u.IsActive);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return Unauthorized(new { error = "Invalid username or password" });
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var permissions = RolePermissions.GetPermissions(user.Role);
        var token = GenerateJwtToken(user, permissions);

        _logger.LogInformation("User {Username} logged in successfully. Role: {Role}",
            user.Username, user.Role);

        return Ok(new LoginResponse
        {
            Token = token,
            RefreshToken = GenerateRefreshToken(),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8),
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                Permissions = (long)permissions
            }
        });
    }

    /// <summary>
    /// Get current user info from token
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var permissionsStr = User.FindFirst("permissions")?.Value;

        return Ok(new
        {
            userId,
            username,
            role,
            permissions = permissionsStr
        });
    }

    /// <summary>
    /// Get available test accounts (dev only)
    /// </summary>
    [HttpGet("test-accounts")]
    public async Task<IActionResult> GetTestAccounts()
    {
        var users = await _db.Users.ToListAsync();

        return Ok(users.Select(u => new
        {
            username = u.Username,
            password = GetDefaultPasswordForRole(u.Role),
            role = u.Role.ToString(),
            fullName = u.FullName,
            permissions = RolePermissions.GetPermissions(u.Role).ToString()
        }));
    }

    private string GenerateJwtToken(User user, Permission permissions)
    {
        var keyBytes = Encoding.UTF8.GetBytes(
            _configuration["JwtSettings:IssuerSigningKey"] ?? "SuperSecretKeyForDev12345678901234567890");

        if (keyBytes.Length < 32)
        {
            var padded = new byte[32];
            Array.Copy(keyBytes, padded, Math.Min(keyBytes.Length, 32));
            keyBytes = padded;
        }

        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("permissions", ((long)permissions).ToString()),
            new("full_name", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Authority"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTimeOffset.UtcNow.AddHours(8).DateTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 2) return password == storedHash;
            var salt = Convert.FromBase64String(parts[0]);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt)));
            return Convert.ToBase64String(hash) == parts[1];
        }
        catch
        {
            return password == storedHash;
        }
    }

    private static string GetDefaultPasswordForRole(SystemRole role)
    {
        return role switch
        {
            SystemRole.SuperAdmin => "Admin123!",
            SystemRole.Admin => "Admin123!",
            SystemRole.Manager => "Manager123!",
            SystemRole.Operator => "Operator123!",
            SystemRole.Shareholder => "Shareholder123!",
            SystemRole.ReadOnly => "Readonly123!",
            _ => "User123!"
        };
    }
}
