using System.Security.Cryptography;
using System.Text;
using SmartParking.Data;
using SmartParking.Models;
using Microsoft.EntityFrameworkCore;

namespace SmartParking.Services
{
    public interface IAuditService
    {
        Task LogAsync(string category, string action, string? entityId = null, string? actor = null,
            string? details = null, string outcome = "success");
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IHttpContextAccessor _http;
        private static string? _lastHash;
        private static readonly object _lock = new();

        public AuditService(ApplicationDbContext ctx, IHttpContextAccessor http)
        {
            _ctx = ctx;
            _http = http;
        }

        public async Task LogAsync(string category, string action, string? entityId = null,
            string? actor = null, string? details = null, string outcome = "success")
        {
            var ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var entry = new AuditLog
            {
                Category = category,
                Action = action,
                EntityId = entityId,
                IpAddress = ip,
                Actor = actor,
                Details = details,
                Outcome = outcome,
                CreatedAt = DateTime.UtcNow
            };

            // Hash-chain integrity: SHA-256(prevHash + this entry's data)
            var dataToHash = $"{_lastHash ?? ""}|{category}|{action}|{entityId}|{ip}|{actor}|{outcome}|{entry.CreatedAt:O}";
            entry.IntegrityHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(dataToHash)));

            lock (_lock)
            {
                _lastHash = entry.IntegrityHash;
            }

            _ctx.AuditLogs.Add(entry);
            await _ctx.SaveChangesAsync();
        }
    }
}
