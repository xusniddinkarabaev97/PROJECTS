using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ODULink.Data;
using ODULink.Enums;

namespace ODULink.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var transactions = _context.Transactions.AsQueryable();

            var todayRevenue = await transactions
                .Where(t => t.PaymentStatus == PaymentStatus.Completed && t.FilledAt >= today)
                .SumAsync(t => (decimal?)t.TotalSum) ?? 0;

            var weekRevenue = await transactions
                .Where(t => t.PaymentStatus == PaymentStatus.Completed && t.FilledAt >= startOfWeek)
                .SumAsync(t => (decimal?)t.TotalSum) ?? 0;

            var monthRevenue = await transactions
                .Where(t => t.PaymentStatus == PaymentStatus.Completed && t.FilledAt >= startOfMonth)
                .SumAsync(t => (decimal?)t.TotalSum) ?? 0;

            var totalPatients = await _context.Patients.CountAsync();
            var totalTransactions = await transactions.CountAsync();

            var paymentMethods = new[] { "cash", "uzcard", "humo", "payme", "click", "transfer" };
            var paymentDistribution = new Dictionary<string, object>();
            foreach (var method in paymentMethods)
            {
                var count = await transactions.CountAsync(t => t.PaymentMethod == method);
                var amount = await transactions
                    .Where(t => t.PaymentMethod == method)
                    .SumAsync(t => (decimal?)t.TotalSum) ?? 0;
                paymentDistribution[method] = new { count, amount };
            }

            var departmentRevenue = await _context.Transactions
                .Where(t => t.DepartmentId != null && t.Department != null)
                .GroupBy(t => new { t.DepartmentId, t.Department!.Name })
                .Select(g => new { departmentName = g.Key.Name, amount = g.Sum(t => t.TotalSum) })
                .ToListAsync();

            return Ok(new
            {
                todayRevenue,
                weekRevenue,
                monthRevenue,
                totalPatients,
                totalTransactions,
                paymentDistribution,
                departmentRevenue,
            });
        }
    }
}
