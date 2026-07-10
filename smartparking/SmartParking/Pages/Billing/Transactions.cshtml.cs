using SmartParking.Data;
using SmartParking.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SmartParking.Pages.Billing;

public class TransactionsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public TransactionsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<TransactionRow> Transactions { get; set; } = new();

    public async Task OnGetAsync()
    {
        Transactions = await _db.Transactions
            .Include(t => t.Client)
            .OrderByDescending(t => t.FilledAt)
            .Select(t => new TransactionRow
            {
                Id = t.Id,
                ClientName = t.Client.FullName ?? t.Client.Phone ?? t.Client.ExternalId,
                TotalSum = t.TotalSum,
                PaymentMethod = t.PaymentMethod,
                PaymentStatus = t.PaymentStatus,
                Status = t.Status,
                FilledAt = t.FilledAt
            })
            .ToListAsync();
    }

    public class TransactionRow
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = "";
        public decimal TotalSum { get; set; }
        public string? PaymentMethod { get; set; }
        public Enums.PaymentStatus PaymentStatus { get; set; }
        public string Status { get; set; } = "";
        public DateTime FilledAt { get; set; }
    }
}
