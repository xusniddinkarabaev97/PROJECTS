using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ODULink.Models;
using ODULink.DTOs;
using ODULink.Services;
using ODULink.Data;

namespace ODULink.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IAuditService _auditService;

        public CompaniesController(ApplicationDbContext context, ITokenService tokenService, IAuditService auditService)
        {
            _context = context;
            _tokenService = tokenService;
            _auditService = auditService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenDto>> Login([FromBody] LoginDto loginDto)
        {
            var company = await _context.Companies.SingleOrDefaultAsync(c => c.Login == loginDto.Login);

            if (company == null)
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            if (string.IsNullOrEmpty(company.JwtAuthToken) || company.JwtAuthToken != loginDto.Password)
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            var tokenDto = _tokenService.GenerateTokens(company);

            await _auditService.LogAsync(
                company.Login,
                "Login",
                "Successful login",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            return Ok(tokenDto);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenDto>> Refresh([FromBody] TokenDto tokenDto)
        {
            if (string.IsNullOrEmpty(tokenDto.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required." });
            }

            try
            {
                var newTokens = await _tokenService.RefreshTokens(tokenDto.RefreshToken);
                return Ok(newTokens);
            }
            catch (SecurityTokenException ex)
            {
                return Unauthorized(new { message = ex.Message });
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
        [Authorize]
        public async Task<ActionResult<Company>> PostCompany(Company company)
        {
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

            _context.Entry(company).State = EntityState.Modified;

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

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CompanyExists(int id)
        {
            return _context.Companies.Any(e => e.Id == id);
        }
    }
}
