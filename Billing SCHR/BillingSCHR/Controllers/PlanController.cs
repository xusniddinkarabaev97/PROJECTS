using ODULink.Data;
using ODULink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ODULink.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PlanController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _context.Plans.ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(Plan plan)
        {
            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return Ok(plan);
        }
    }
}
