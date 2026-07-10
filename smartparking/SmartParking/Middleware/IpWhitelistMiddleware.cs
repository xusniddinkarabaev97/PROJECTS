using System.Collections.Concurrent;

namespace SmartParking.Middleware
{
    /// <summary>
    /// IP Whitelist middleware for Dahua webhook endpoint.
    /// Only allows requests from configured Dahua DSS server IPs.
    /// Implements requirement §1.3 Allowlist / IP Whitelisting
    /// </summary>
    public class IpWhitelistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpWhitelistMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, bool> _allowedIps = new();

        public IpWhitelistMiddleware(RequestDelegate next, ILogger<IpWhitelistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public static void SetAllowedIps(IEnumerable<string> ips)
        {
            _allowedIps.Clear();
            foreach (var ip in ips)
                _allowedIps[ip.Trim()] = true;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api/DahuaIntegration/events",
                StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (_allowedIps.IsEmpty)
            {
                await _next(context);
                return;
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "";
            if (remoteIp == "::1") remoteIp = "127.0.0.1";

            if (_allowedIps.ContainsKey(remoteIp) || remoteIp == "127.0.0.1")
            {
                await _next(context);
                return;
            }

            _logger.LogWarning("Blocked Dahua webhook from unauthorized IP: {Ip}", remoteIp);
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"ip_not_allowed\"}");
        }
    }

    public static class IpWhitelistMiddlewareExtensions
    {
        public static IApplicationBuilder UseIpWhitelist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpWhitelistMiddleware>();
        }
    }
}
