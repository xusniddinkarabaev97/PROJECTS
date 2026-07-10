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
    public class PlanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PlanController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _context.Plans.ToListAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Plan plan)
        {
            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return Ok(plan);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Plan plan)
        {
            if (id != plan.Id) return BadRequest();
            var existing = await _context.Plans.FindAsync(id);
            if (existing == null) return NotFound();
            existing.Name = plan.Name;
            existing.Price = plan.Price;
            existing.DurationHours = plan.DurationHours;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null) return NotFound();
            _context.Plans.Remove(plan);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

}
