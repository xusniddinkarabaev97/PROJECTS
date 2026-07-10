using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

using SmartParking.Models;
using SmartParking.DTOs;
using SmartParking.Services;
using SmartParking.Data;

namespace SmartParking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public CompaniesController(ApplicationDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenDto>> Login([FromBody] LoginDto loginDto)
        {
            var company = await _context.Companies.SingleOrDefaultAsync(c => c.Email == loginDto.Email);

            if (company == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            if (string.IsNullOrEmpty(company.JwtAuthToken) || company.JwtAuthToken != loginDto.Password)
            {
                return Unauthorized("Invalid credentials.");
            }

            var tokenDto = _tokenService.GenerateTokens(company);
            return Ok(new
            {
                tokenDto.AccessToken,
                tokenDto.RefreshToken,
                tokenDto.AccessTokenExpiration,
                companyId = company.Id,
                email = company.Email,
                name = company.Name
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenDto>> Refresh([FromBody] TokenDto tokenDto)
        {
            if (string.IsNullOrEmpty(tokenDto.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }

            try
            {
                var newTokens = await _tokenService.RefreshTokens(tokenDto.RefreshToken);
                return Ok(newTokens);
            }
            catch (SecurityTokenException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            return await _context.Companies.ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Company>> GetCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);

            if (company == null)
            {
                return NotFound();
            }

            var companyIdClaim = User.FindFirst("companyId")?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (companyIdClaim != id.ToString() && !isAdmin)
            {
                return Forbid();
            }

            return company;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Company>> PostCompany(Company company)
        {
            company.CreatedAt = DateTime.UtcNow;
            company.UpdatedAt = DateTime.UtcNow;
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCompany", new { id = company.Id }, company);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutCompany(int id, Company company)
        {
            if (id != company.Id)
            {
                return BadRequest();
            }

            var currentCompanyId = User.FindFirst("companyId")?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (currentCompanyId != id.ToString() && !isAdmin)
            {
                return Forbid();
            }

            var existing = await _context.Companies.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Name = company.Name;
            existing.Inn = company.Inn;
            existing.Email = company.Email;
            existing.Phone = company.Phone;
            existing.Address = company.Address;
            if (!string.IsNullOrEmpty(company.JwtAuthToken))
                existing.JwtAuthToken = company.JwtAuthToken;
            existing.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CompanyExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private bool CompanyExists(int id)
        {
            return _context.Companies.Any(e => e.Id == id);
        }
    }
}
