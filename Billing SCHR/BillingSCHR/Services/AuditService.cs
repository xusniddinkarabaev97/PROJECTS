using ODULink.Data;
using ODULink.Models;

namespace ODULink.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userLogin, string action, string details, string ipAddress)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserLogin = userLogin,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
