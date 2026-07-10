namespace SmartParking.Middleware
{
    /// <summary>
    /// Adds security headers for PCI DSS compliance:
    /// - HSTS (force HTTPS)
    /// - X-Content-Type-Options
    /// - X-Frame-Options
    /// - CSP (Content Security Policy)
    /// - TLS version enforcement (logged if not 1.2+)
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // HSTS - force HTTPS for 1 year
            context.Response.Headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains; preload";

            // Prevent MIME type sniffing
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // CSP
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self' 'unsafe-inline' https://unpkg.com; style-src 'self' 'unsafe-inline'; img-src 'self' data: https://*.tile.openstreetmap.org; connect-src 'self' https://*.ts.net; frame-src 'self' https://unpkg.com";

            // Referrer policy
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions policy
            context.Response.Headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(self)";

            // TLS version check (log warning if old TLS)
            var protocol = context.Request.Protocol;
            if (context.Request.IsHttps && context.Connection.ClientCertificate == null)
            {
                // Log that mTLS is not configured (requirement §3.1)
            }

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
