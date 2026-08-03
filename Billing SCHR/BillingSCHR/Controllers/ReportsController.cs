using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ODULink.Data;

namespace ODULink.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/reports/summary?year=&month=
        [HttpGet("summary")]
        public async Task<ActionResult> GetSummary(int year, int? month = null)
        {
            var query = _context.Transactions.AsQueryable();

            query = query.Where(t => t.FilledAt.Year == year);
            if (month.HasValue)
                query = query.Where(t => t.FilledAt.Month == month.Value);

            var transactions = await query.ToListAsync();

            var totalRevenue = transactions.Sum(t => t.TotalSum);
            var transactionCount = transactions.Count;

            var byPaymentMethod = transactions
                .Where(t => !string.IsNullOrEmpty(t.PaymentMethod))
                .GroupBy(t => t.PaymentMethod)
                .Select(g => new { paymentMethod = g.Key, count = g.Count(), revenue = g.Sum(t => t.TotalSum) })
                .ToList();

            var byDepartment = transactions
                .Where(t => t.DepartmentId.HasValue)
                .GroupBy(t => t.DepartmentId!.Value)
                .Select(g => new { departmentId = g.Key, count = g.Count(), revenue = g.Sum(t => t.TotalSum) })
                .ToList();

            return Ok(new
            {
                totalRevenue,
                transactionCount,
                byPaymentMethod,
                byDepartment
            });
        }

        // GET: api/reports/by-department?year=&month=
        [HttpGet("by-department")]
        public async Task<ActionResult> GetByDepartment(int year, int? month = null)
        {
            var query = _context.Transactions
                .Include(t => t.Department)
                .AsQueryable();

            query = query.Where(t => t.FilledAt.Year == year && t.DepartmentId.HasValue);
            if (month.HasValue)
                query = query.Where(t => t.FilledAt.Month == month.Value);

            var data = await query
                .GroupBy(t => new { t.DepartmentId, DepartmentName = t.Department!.Name })
                .Select(g => new
                {
                    departmentId = g.Key.DepartmentId,
                    departmentName = g.Key.DepartmentName,
                    transactionCount = g.Count(),
                    revenue = g.Sum(t => t.TotalSum)
                })
                .OrderByDescending(g => g.revenue)
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/reports/by-cashier?year=&month=
        [HttpGet("by-cashier")]
        public async Task<ActionResult> GetByCashier(int year, int? month = null)
        {
            var query = _context.Transactions.AsQueryable();

            query = query.Where(t => t.FilledAt.Year == year);
            if (month.HasValue)
                query = query.Where(t => t.FilledAt.Month == month.Value);

            var data = await query
                .GroupBy(t => t.DoctorName ?? "Unknown")
                .Select(g => new
                {
                    cashier = g.Key,
                    transactionCount = g.Count(),
                    revenue = g.Sum(t => t.TotalSum)
                })
                .OrderByDescending(g => g.revenue)
                .ToListAsync();

            return Ok(data);
        }
    }
}
