using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ODULink.Models;
using ODULink.DTOs;
using ODULink.Data;

namespace ODULink.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            return await _context.Companies
                .Select(c => new UserDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Login = c.Login,
                    Email = c.Email,
                    Phone = c.Phone,
                    Role = c.Role,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Companies
                .Where(c => c.Id == id)
                .Select(c => new UserDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Login = c.Login,
                    Email = c.Email,
                    Phone = c.Phone,
                    Role = c.Role,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return user;
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(Company company)
        {
            company.CreatedAt = DateTime.UtcNow;
            company.UpdatedAt = DateTime.UtcNow;
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = company.Id,
                Name = company.Name,
                Login = company.Login,
                Email = company.Email,
                Phone = company.Phone,
                Role = company.Role,
                CreatedAt = company.CreatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = company.Id }, userDto);
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, Company company)
        {
            if (id != company.Id)
                return BadRequest();

            var currentCompanyId = User.FindFirst("companyId")?.Value;
            if (currentCompanyId == id.ToString())
                return BadRequest("Cannot update your own account through this endpoint.");

            var existing = await _context.Companies.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Name = company.Name;
            existing.Login = company.Login;
            existing.Email = company.Email;
            existing.Phone = company.Phone;
            existing.Role = company.Role;
            existing.JwtAuthToken = company.JwtAuthToken;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentCompanyId = User.FindFirst("companyId")?.Value;
            if (currentCompanyId == id.ToString())
                return BadRequest("Cannot delete your own account.");

            var company = await _context.Companies.FindAsync(id);
            if (company == null)
                return NotFound();

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
