using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly GzsBillingDbContext _db;

    public TransactionsController(GzsBillingDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var query = _db.Tranzaktsiyalar
            .Include(t => t.Payment)
            .Include(t => t.Dispenser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.IdempotencyKey.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                id = t.Id,
                totalSum = t.TotalSum,
                fillingStationId = t.FillingStationId,
                dispenserName = t.Dispenser != null ? t.Dispenser.Name : "",
                cardType = t.CardType,
                paymentName = t.Payment != null ? t.Payment.Name : "",
                status = t.Status.ToString(),
                idempotencyKey = t.IdempotencyKey,
                createdAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) });
    }
}
