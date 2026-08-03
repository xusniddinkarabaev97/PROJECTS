using ODULink.Data;
using ODULink.Enums;
using ODULink.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ODULink.Controllers
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            return await _context.Transactions
                .Include(t => t.Patient)
                .Include(t => t.Department)
                .ToListAsync();
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportExcel([FromQuery] int year, [FromQuery] int? month = null)
        {
            var query = _context.Transactions
                .Include(t => t.Patient)
                .Where(t => t.FilledAt.Year == year);

            string periodLabel;
            if (month.HasValue)
            {
                query = query.Where(t => t.FilledAt.Month == month.Value);
                periodLabel = $"{month:D2}.{year}";
            }
            else
            {
                periodLabel = $"{year}";
            }

            var transactions = await query
                .OrderByDescending(t => t.FilledAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add($"Otchet {periodLabel}");

            // Title
            ws.Cell(1, 1).Value = $"Gospital MoD - Otchet za {periodLabel}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 6).Merge();

            ws.Cell(2, 1).Value = $"Sana: {DateTime.Now:dd.MM.yyyy HH:mm}";
            ws.Range(2, 1, 2, 6).Merge();

            // Headers
            var headers = new[] { "Tranzaksiya ID", "ID Bemor", "F.I.O Bemor", "Summa", "Holat", "Sana" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1f6feb");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Data
            int row = 5;
            decimal total = 0;
            foreach (var t in transactions)
            {
                var statusText = t.PaymentStatus == PaymentStatus.Completed
                    ? "Podtverjden"
                    : "Otmenen";

                ws.Cell(row, 1).Value = t.Id;
                ws.Cell(row, 2).Value = t.PatientId;
                ws.Cell(row, 3).Value = t.Patient?.FullName ?? "-";
                ws.Cell(row, 4).Value = t.TotalSum;
                ws.Cell(row, 5).Value = statusText;
                ws.Cell(row, 6).Value = t.FilledAt.ToString("dd.MM.yyyy HH:mm");

                if (statusText == "Podtverjden")
                    ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#2e7d32");
                else
                    ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#c62828");

                total += t.TotalSum;
                row++;
            }

            // Total
            ws.Cell(row, 3).Value = "JAMI:";
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 4).Value = total;
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 6).Style.Border.TopBorder = XLBorderStyleValues.Double;

            ws.Columns().AdjustToContents(5, 40);

            var fileName = $"Otchet_{periodLabel}.xlsx";
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Patient)
                .Include(t => t.Department)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return NotFound();

            return transaction;
        }

        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTransaction(int id, Transaction transaction)
        {
            if (id != transaction.Id) return BadRequest();
            _context.Entry(transaction).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Transactions.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound();
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
