using SmartParking.Data;
using SmartParking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace SmartParking.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/stations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Station>>> GetStations()
        {
            return await _context.Stations.ToListAsync();
        }

        // GET: api/stations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Station>> GetStation(int id)
        {
            var station = await _context.Stations.FindAsync(id);
            if (station == null) return NotFound();
            return station;
        }

        // POST: api/stations
        [HttpPost]
        public async Task<ActionResult<Station>> CreateStation(Station station)
        {
            station.CreatedAt = DateTime.UtcNow;
            station.UpdatedAt = DateTime.UtcNow;
            _context.Stations.Add(station);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStation), new { id = station.Id }, station);
        }

        // PUT: api/stations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStation(int id, Station station)
        {
            if (id != station.Id) return BadRequest();

            var existing = await _context.Stations.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = station.Name;
            existing.Region = station.Region;
            existing.District = station.District;
            existing.Address = station.Address;
            existing.Latitude = station.Latitude;
            existing.Longitude = station.Longitude;
            existing.CompanyId = station.CompanyId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/stations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStation(int id)
        {
            var station = await _context.Stations.FindAsync(id);
            if (station == null) return NotFound();

            _context.Stations.Remove(station);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

}
