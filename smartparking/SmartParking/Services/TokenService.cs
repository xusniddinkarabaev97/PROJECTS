using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using SmartParking.Models;
using SmartParking.DTOs;
using SmartParking.Data;

namespace SmartParking.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public TokenService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public TokenDto GenerateTokens(Company company)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
            var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured.");
            var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured.");
            var accessTokenExpirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");
            var refreshTokenExpirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");

            var key = Encoding.ASCII.GetBytes(secret);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, company.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("companyId", company.Id.ToString()),
                new Claim("companyName", company.Name)
            };

            // Admin company (from seed data) gets the Admin role
            if (company.Email == "admin@smartparking.uz")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var accessTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
            var encodedAccessToken = tokenHandler.WriteToken(accessToken);

            var refreshToken = GenerateRefreshToken();
            company.RefreshToken = refreshToken;
            company.TokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
            _context.SaveChanges();

            return new TokenDto
            {
                AccessToken = encodedAccessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = accessTokenDescriptor.Expires.Value
            };
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
            var key = Encoding.ASCII.GetBytes(secret);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        public async Task<TokenDto> RefreshTokens(string refreshToken)
        {
            var company = await _context.Companies
                .SingleOrDefaultAsync(c => c.RefreshToken == refreshToken && c.TokenExpiry > DateTime.UtcNow);

            if (company == null)
            {
                throw new SecurityTokenException("Invalid refresh token or token expired.");
            }

            company.RefreshToken = string.Empty;
            company.TokenExpiry = null;
            await _context.SaveChangesAsync();

            return GenerateTokens(company);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}
