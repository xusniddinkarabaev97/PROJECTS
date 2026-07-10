using SmartParking.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SmartParking.Pages.Billing;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public int TotalTransactions { get; set; }
    public int TotalCompanies { get; set; }
    public int TotalStations { get; set; }
    public int TotalClients { get; set; }
    public decimal TodayRevenue { get; set; }
    public List<LastTransaction> RecentTransactions { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalTransactions = await _db.Transactions.CountAsync();
        TotalCompanies = await _db.Companies.CountAsync();
        TotalStations = await _db.Stations.CountAsync();
        TotalClients = await _db.Clients.CountAsync();

        var today = DateTime.UtcNow.Date;
        TodayRevenue = await _db.Transactions
            .Where(t => t.FilledAt >= today)
            .SumAsync(t => t.TotalSum);

        RecentTransactions = await _db.Transactions
            .Include(t => t.Client)
            .OrderByDescending(t => t.FilledAt)
            .Take(10)
            .Select(t => new LastTransaction
            {
                Id = t.Id,
                ClientName = t.Client.FullName ?? t.Client.Phone ?? t.Client.ExternalId,
                TotalSum = t.TotalSum,
                PaymentStatus = t.PaymentStatus.ToString(),
                FilledAt = t.FilledAt,
                Status = t.Status
            })
            .ToListAsync();
    }

    public class LastTransaction
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = "";
        public decimal TotalSum { get; set; }
        public string PaymentStatus { get; set; } = "";
        public DateTime FilledAt { get; set; }
        public string Status { get; set; } = "";
    }
}
