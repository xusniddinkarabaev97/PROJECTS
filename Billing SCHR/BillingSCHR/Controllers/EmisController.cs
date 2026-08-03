using ODULink.Data;
using ODULink.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ODULink.Controllers
{
    [Route("api/v1/billing")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "integration")]
    public class EmisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmisController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("check")]
        public async Task<IActionResult> Check([FromBody] EmisCheckRequest request)
        {
            int.TryParse(request.PatientId, out var id);
            var patient = await _context.Patients.FindAsync(id);

            if (patient == null)
            {
                return Ok(new EmisCheckResponse
                {
                    RequestId = request.RequestId,
                    Status = "NOT_FOUND",
                    Message = "Patient not found"
                });
            }

            var invoiceId = $"INV-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
            var amount = Random.Shared.Next(25000, 500000);

            return Ok(new EmisCheckResponse
            {
                RequestId = request.RequestId,
                Status = "FOUND",
                Patient = new EmisPatientInfo
                {
                    Id = patient.Id.ToString(),
                    FirstName = patient.FullName?.Split(' ').ElementAtOrDefault(1) ?? "",
                    LastName = patient.FullName?.Split(' ').ElementAtOrDefault(0) ?? "-",
                    MiddleName = patient.FullName?.Split(' ').ElementAtOrDefault(2),
                    Rank = patient.MilitaryRank
                },
                Billing = new EmisBillingInfo
                {
                    InvoiceId = invoiceId,
                    TotalAmount = amount,
                    Currency = "UZS",
                    Purpose = "Medical service (EMIS mock)"
                }
            });
        }

        [HttpPost("pay")]
        public IActionResult Pay([FromBody] EmisPayRequest request)
        {
            return Ok(new EmisPayResponse
            {
                RequestId = request.RequestId,
                Status = "SUCCESS",
                ReceiptNumber = $"RCPT-EMIS-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
            });
        }
    }
}
