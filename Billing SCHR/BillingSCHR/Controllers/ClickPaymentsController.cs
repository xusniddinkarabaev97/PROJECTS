using ODULink.Data;
using ODULink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ODULink.Controllers
{
    /// <summary>
    /// Просмотр истории Click-платежей для административной панели.
    /// </summary>
    /// <remarks>
    /// Позволяет отслеживать статусы платежей:
    /// - `PREPARE` — платёж зарегистрирован (после prepare)
    /// - `COMPLETE` — платёж успешно завершён, EMIS уведомлён
    /// - `FAILED` — платёж провален (EMIS вернул ошибку, несовпадение суммы)
    /// - `CANCELLED` — платёж отменён (Click: error ≠ 0)
    /// </remarks>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "integration")]
    public class ClickPaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClickPaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить список Click-платежей с пагинацией и фильтром по статусу.
        /// </summary>
        /// <param name="status">Фильтр: PREPARE, COMPLETE, FAILED, CANCELLED.</param>
        /// <param name="page">Номер страницы (по умолчанию 1).</param>
        /// <param name="pageSize">Размер страницы (по умолчанию 20).</param>
        /// <returns>Список платежей с общим количеством.</returns>
        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<ActionResult<IEnumerable<ClickPayment>>> GetClickPayments(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.ClickPayments.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { total, page, pageSize, data });
        }

        /// <summary>
        /// Получить конкретный платёж Click по ID.
        /// </summary>
        /// <param name="id">ID записи в БД.</param>
        /// <returns>Детали платежа: статус, invoiceId, receiptNumber из EMIS.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClickPayment), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ClickPayment>> GetClickPayment(int id)
        {
            var payment = await _context.ClickPayments.FindAsync(id);
            if (payment == null) return NotFound();
            return payment;
        }
    }
}
