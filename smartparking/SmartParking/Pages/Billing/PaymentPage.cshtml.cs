using SmartParking.Data;
using SmartParking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SmartParking.Pages.Billing;

public class PaymentPageModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public PaymentPageModel(ApplicationDbContext context) { _context = context; }
    public Transaction? Transaction { get; set; }
    public int TransactionId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        TransactionId = id;
        Transaction = await _context.Transactions.Include(t => t.Client).FirstOrDefaultAsync(t => t.Id == id);
        return Page();
    }
}
