using SmartParking.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SmartParking.Pages.Billing;

public class QrCodeModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public QrCodeModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public int SuggestedTransactionId { get; set; }
    public List<TransactionInfo> RecentTransactions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var lastTxn = await _db.Transactions
            .OrderByDescending(t => t.Id)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        SuggestedTransactionId = lastTxn + 1;

        RecentTransactions = await _db.Transactions
            .Include(t => t.Client)
            .OrderByDescending(t => t.FilledAt)
            .Take(10)
            .Select(t => new TransactionInfo
            {
                Id = t.Id,
                ClientName = t.Client.FullName ?? t.Client.Phone ?? t.Client.ExternalId,
                TotalSum = t.TotalSum,
                PaymentStatus = t.PaymentStatus.ToString(),
                Status = t.Status
            })
            .ToListAsync();
    }

    public class TransactionInfo
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = "";
        public decimal TotalSum { get; set; }
        public string PaymentStatus { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
