using SmartParking.Data;
using SmartParking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using System.Text.Json;

namespace SmartParking.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _http;

        public TransactionsController(ApplicationDbContext context, IConfiguration config, IHttpClientFactory http)
        {
            _context = context;
            _config = config;
            _http = http;
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

                    await SendPaymentCallback(txn, paid: true);

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

                    await SendPaymentCallback(txn, paid: false);

                    return Ok(new { id, status = "Transaction failed" });
                }

                // Direction B2: SmartParking → UParking payment callback
                private async Task SendPaymentCallback(Transaction txn, bool paid)
                {
                    try
                    {
                        var uparkingUrl = _config["Billing:UparkingCallbackUrl"];
                        if (string.IsNullOrEmpty(uparkingUrl)) return;

                        var secret = _config["Billing:SharedSecret"];
                        var sessionId = "";
                        try
                        {
                            var pm = JsonSerializer.Deserialize<JsonElement>(txn.PaymentMethod ?? "{}");
                            if (pm.TryGetProperty("sessionId", out var s)) sessionId = s.GetString();
                        }
                        catch { }

                        var cbPayload = JsonSerializer.Serialize(new
                        {
                            sessionId,
                            billingReferenceId = txn.Id.ToString(),
                            paid,
                            paidAt = DateTime.UtcNow.ToString("o")
                        });

                        var cbClient = _http.CreateClient();
                        var cbContent = new StringContent(cbPayload, Encoding.UTF8, "application/json");
                        if (!string.IsNullOrEmpty(secret))
                            cbClient.DefaultRequestHeaders.Add("X-Billing-Secret", secret);

                        await cbClient.PostAsync($"{uparkingUrl}/api/billing/payment", cbContent);
                    }
                    catch { }
                }

                // GET: api/Transactions/parking/last (last parking transaction)
                [AllowAnonymous]
                [HttpGet("parking/last")]
                public async Task<IActionResult> LastParking()
                {
                    // Try parking first, fallback to any transaction
                    var txn = await _context.Transactions
                        .Include(t => t.Client)
                        .Where(t => t.Status == "parking")
                        .OrderByDescending(t => t.FilledAt)
                        .FirstOrDefaultAsync();

                    if (txn == null)
                    {
                        txn = await _context.Transactions
                            .Include(t => t.Client)
                            .OrderByDescending(t => t.FilledAt)
                            .FirstOrDefaultAsync();
                    }

                    if (txn == null)
                        return Ok(new { id = 0, clientName = "—", totalSum = 0, 
                            entryTime = (string?)null, exitTime = (string?)null, 
                            duration = (string?)null, status = "empty", filledAt = (DateTime?)null });

                    // Extract entry/exit/duration from PaymentMethod (stored as JSON before first '|')
                    string entryTime = null, exitTime = null, duration = null;
                    if (!string.IsNullOrEmpty(txn.PaymentMethod))
                    {
                        try
                        {
                            var json = txn.PaymentMethod.Split('|')[0]; // strip |click|fiscal:... suffix
                            var dto = System.Text.Json.JsonSerializer.Deserialize<ParkingDto>(json);
                            if (dto != null)
                            {
                                entryTime = dto.Kirish?.ToString("o");
                                exitTime = dto.Chiqish?.ToString("o");
                                duration = dto.Davomiyligi;
                            }
                        }
                        catch { }
                    }

                    return Ok(new
                    {
                        id = txn.Id,
                        clientName = txn.Client?.FullName ?? txn.Client?.ExternalId,
                        totalSum = txn.TotalSum,
                        entryTime,
                        exitTime,
                        duration,
                        status = txn.PaymentStatus.ToString(),
                        filledAt = txn.FilledAt
                    });
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

                    // Дедупликация: одна машина + одно время въезда/выезда = одна транзакция
                    var dedupKey = $"{dto.AvtoRaqam}|{dto.Kirish?.ToString("yyyy-MM-ddTHH:mm:ss")}|{dto.Chiqish?.ToString("yyyy-MM-ddTHH:mm:ss")}";
                    var existing = await _context.Transactions
                        .FirstOrDefaultAsync(t => t.ExternalRef == dedupKey);
                    if (existing != null)
                        return Ok(new { id = existing.Id, chekId = dto.ChekId, status = "exists", duplicate = true });

                    var txn = new Transaction
                    {
                        ClientId = client.Id,
                        TotalSum = dto.JamiTolov,
                        PaymentStatus = Enums.PaymentStatus.New,
                        PaymentMethod = System.Text.Json.JsonSerializer.Serialize(dto),
                        ExternalRef = dedupKey,
                        Status = "parking",
                        FilledAt = DateTime.UtcNow
                    };
                    _context.Transactions.Add(txn);
                    await _context.SaveChangesAsync();

                    return Ok(new { id = txn.Id, chekId = dto.ChekId, status = "created", duplicate = false });
                }
        }

    // DTO for parking data from avto.itpanda.uz
    public class ParkingDto
    {
        public string ChekId { get; set; } = string.Empty;
        public string AvtoRaqam { get; set; } = string.Empty;
        public DateTime? Kirish { get; set; }
        public DateTime? Chiqish { get; set; }
        public string Davomiyligi { get; set; } = string.Empty;
        public decimal JamiTolov { get; set; }
    }
}
