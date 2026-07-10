using SmartParking.Data;
using SmartParking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace SmartParking.Controllers
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.SharePercents.Include(sp => sp.Plan).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var sp = await _context.SharePercents.Include(s => s.Plan).FirstOrDefaultAsync(s => s.Id == id);
            if (sp == null) return NotFound();
            return Ok(sp);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SharePercent sharePercent)
        {
            if (id != sharePercent.Id) return BadRequest();
            var existing = await _context.SharePercents.FindAsync(id);
            if (existing == null) return NotFound();

            existing.ShareholderName = sharePercent.ShareholderName;
            existing.Percent = sharePercent.Percent;
            existing.PlanId = sharePercent.PlanId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sp = await _context.SharePercents.FindAsync(id);
            if (sp == null) return NotFound();
            _context.SharePercents.Remove(sp);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
