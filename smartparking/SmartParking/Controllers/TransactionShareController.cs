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
    public class TransactionShareController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransactionShareController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("by-transaction/{transactionId}")]
        public async Task<IActionResult> GetByTransaction(int transactionId)
        {
            var list = await _context.TransactionShares
                .Where(ts => ts.TransactionId == transactionId)
                .Include(ts => ts.SharePercent)
                .Include(ts => ts.Plan)
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TransactionShare transactionShare)
        {
            _context.TransactionShares.Add(transactionShare);
            await _context.SaveChangesAsync();
            return Ok(transactionShare);
        }
    }

}
