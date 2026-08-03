using ODULink.Data;
using ODULink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ODULink.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SharePercentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SharePercentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("by-plan/{planId}")]
        public async Task<IActionResult> GetByPlan(int planId)
        {
            var list = await _context.SharePercents.Where(sp => sp.PlanId == planId).ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SharePercent sharePercent)
        {
            _context.SharePercents.Add(sharePercent);
            await _context.SaveChangesAsync();
            return Ok(sharePercent);
        }
    }
}
