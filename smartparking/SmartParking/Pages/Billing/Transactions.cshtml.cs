using SmartParking.Data;
using SmartParking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace SmartParking.Pages.Billing;

public class TransactionsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public TransactionsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<TransactionRow> Transactions { get; set; } = new();
    public string Period { get; set; } = "day";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal TotalAmount { get; set; }

    public async Task OnGetAsync(string period = "day")
    {
        Period = period;
        var now = DateTime.UtcNow.Date;

        (DateFrom, DateTo) = period switch
        {
            "week"  => (now.AddDays(-7), now.AddDays(1)),
            "month" => (now.AddDays(-30), now.AddDays(1)),
            _       => (now, now.AddDays(1))  // day
        };

        var query = _db.Transactions
            .Include(t => t.Client)
            .Where(t => t.FilledAt >= DateFrom && t.FilledAt < DateTo)
            .OrderByDescending(t => t.FilledAt);

        Transactions = await query
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

        TotalAmount = Transactions.Sum(t => t.TotalSum);
    }

    public async Task<IActionResult> OnGetExportAsync(string period = "day")
    {
        var now = DateTime.UtcNow.Date;
        var (from, to) = period switch
        {
            "week"  => (now.AddDays(-7), now.AddDays(1)),
            "month" => (now.AddDays(-30), now.AddDays(1)),
            _       => (now, now.AddDays(1))
        };

        var txns = await _db.Transactions
            .Include(t => t.Client)
            .Where(t => t.FilledAt >= from && t.FilledAt < to)
            .OrderByDescending(t => t.FilledAt)
            .Select(t => new {
                t.Id,
                Client = t.Client.FullName ?? t.Client.Phone ?? t.Client.ExternalId,
                t.TotalSum,
                t.PaymentStatus,
                t.Status,
                t.FilledAt
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID;Клиент;Сумма;Статус;Тип;Дата");
        foreach (var t in txns)
        {
            sb.AppendLine($"{t.Id};{t.Client};{t.TotalSum};{t.PaymentStatus};{t.Status};{t.FilledAt.ToLocalTime():dd.MM.yyyy HH:mm}");
        }

        var name = $"transactions_{period}_{now:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", name);
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
