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
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            return await _context.Transactions
                .Include(t => t.Client)
                .ToListAsync();
        }

        // GET: api/Transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Client)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return NotFound();

            return transaction;
        }

        // POST: api/Transactions
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        // PUT: api/Transactions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, Transaction transaction)
        {
            if (id != transaction.Id)
                return BadRequest();

            _context.Entry(transaction).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Transactions.Any(e => e.Id == id))
                    return NotFound();

                throw;
            }

            return NoContent();
        }

        // DELETE: api/Transactions/5
                [HttpDelete("{id}")]
                public async Task<IActionResult> DeleteTransaction(int id)
                {
                    var transaction = await _context.Transactions.FindAsync(id);
                    if (transaction == null)
                        return NotFound();

                    _context.Transactions.Remove(transaction);
                    await _context.SaveChangesAsync();

                    return NoContent();
                }

                // POST: api/Transactions/5/complete
                [AllowAnonymous]
                [HttpPost("{id}/complete")]
                public async Task<IActionResult> CompleteTransaction(int id)
                {
                    var txn = await _context.Transactions.FindAsync(id);
                    if (txn == null) return NotFound($"Transaction #{id} not found");

                    txn.PaymentStatus = Enums.PaymentStatus.Completed;
                    txn.PaymentMethod ??= "qr";
                    await _context.SaveChangesAsync();

                    return Ok(new { id, status = "Transaction completed" });
                }

                // POST: api/Transactions/5/fail
                [AllowAnonymous]
                [HttpPost("{id}/fail")]
                public async Task<IActionResult> FailTransaction(int id)
                {
                    var txn = await _context.Transactions.FindAsync(id);
                    if (txn == null) return NotFound($"Transaction #{id} not found");
                    txn.PaymentStatus = Enums.PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    return Ok(new { id, status = "Transaction failed" });
                }

                // POST: api/Transactions/parking (from avto.itpanda.uz)
                [AllowAnonymous]
                [HttpPost("parking")]
                public async Task<ActionResult<Transaction>> CreateParking([FromBody] ParkingDto dto)
                {
                    // Find or create client by car plate
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.ExternalId == dto.AvtoRaqam);
                    if (client == null)
                    {
                        client = new Client
                        {
                            ExternalId = dto.AvtoRaqam,
                            FullName = dto.AvtoRaqam,
                            Source = "parking",
                            Status = "active"
                        };
                        _context.Clients.Add(client);
                        await _context.SaveChangesAsync();
                    }

                    var txn = new Transaction
                    {
                        ClientId = client.Id,
                        TotalSum = dto.JamiTolov,
                        PaymentStatus = Enums.PaymentStatus.New,
                        PaymentMethod = System.Text.Json.JsonSerializer.Serialize(dto),
                        Status = "parking",
                        FilledAt = DateTime.UtcNow
                    };
                    _context.Transactions.Add(txn);
                    await _context.SaveChangesAsync();

                    return Ok(new { id = txn.Id, chekId = dto.ChekId, status = "created" });
                }
        }

    // DTO for parking data from avto.itpanda.uz
    public class ParkingDto
    {
        public string ChekId { get; set; } = string.Empty;
        public string AvtoRaqam { get; set; } = string.Empty;
        public DateTime Kirish { get; set; }
        public DateTime Chiqish { get; set; }
        public string Davomiyligi { get; set; } = string.Empty;
        public decimal JamiTolov { get; set; }
    }
}
