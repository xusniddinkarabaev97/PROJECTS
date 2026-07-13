using System.Security.Cryptography;
using GzsBilling.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GzsBilling.Api.Middleware;

public class WebhookSignatureMiddleware
{
    private const string WebhookPathPrefix = "/api/webhooks";
    private const string ContragentHeader = "X-Webhook-Contragent";

    private readonly RequestDelegate _next;
    private readonly ILogger<WebhookSignatureMiddleware> _logger;
    private readonly WebhookSettings _settings;

    public WebhookSignatureMiddleware(
        RequestDelegate next,
        IOptions<WebhookSettings> options,
        ILogger<WebhookSignatureMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(WebhookPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var signature = context.Request.Headers[_settings.SignatureHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning(
                "WebhookSignature: Missing signature header '{HeaderName}'. RemoteIp={RemoteIp} Path={Path}",
                _settings.SignatureHeaderName,
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                $"{{\"error\":\"Missing required header '{_settings.SignatureHeaderName}'\"}}");
            return;
        }

        context.Request.EnableBuffering();

        byte[] bodyBytes;
        using (var memoryStream = new MemoryStream())
        {
            await context.Request.Body.CopyToAsync(memoryStream);
            bodyBytes = memoryStream.ToArray();
        }

        context.Request.Body.Position = 0;

        var contragent = context.Request.Headers[ContragentHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(contragent))
        {
            _logger.LogWarning(
                "WebhookSignature: Missing contragent header. RemoteIp={RemoteIp} Path={Path}",
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                $"{{\"error\":\"Missing required header '{ContragentHeader}'\"}}");
            return;
        }

        if (!_settings.ContragentSecrets.TryGetValue(contragent, out var secretBase64) || string.IsNullOrWhiteSpace(secretBase64))
        {
            _logger.LogWarning(
                "WebhookSignature: Unknown contragent '{Contragent}'. RemoteIp={RemoteIp} Path={Path}",
                contragent,
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Unknown contragent.\"}");
            return;
        }

        byte[] secretKey;
        try
        {
            secretKey = Convert.FromBase64String(secretBase64);
        }
        catch (FormatException ex)
        {
            _logger.LogError(
                ex,
                "WebhookSignature: Invalid base64 secret for contragent '{Contragent}'",
                contragent);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Internal server error.\"}");
            return;
        }

        byte[] computedSignature;
        using (var hmac = new HMACSHA256(secretKey))
        {
            computedSignature = hmac.ComputeHash(bodyBytes);
        }

        var computedSignatureHex = Convert.ToHexString(computedSignature);

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            _logger.LogWarning(
                "WebhookSignature: Invalid signature hex format. Contragent={Contragent} RemoteIp={RemoteIp} Path={Path}",
                contragent,
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Invalid signature format.\"}");
            return;
        }

        if (!CryptographicOperations.FixedTimeEquals(computedSignature, signatureBytes))
        {
            _logger.LogWarning(
                "WebhookSignature: Invalid signature. Contragent={Contragent} RemoteIp={RemoteIp} Path={Path}",
                contragent,
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                "{\"error\":\"Invalid webhook signature.\"}");
            return;
        }

        var timestampHeader = context.Request.Headers[_settings.TimestampHeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(timestampHeader))
        {
            if (long.TryParse(timestampHeader, out var unixTimestampSeconds))
            {
                var requestTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestampSeconds);
                var currentTime = DateTimeOffset.UtcNow;
                var age = currentTime - requestTime;

                if (Math.Abs(age.TotalSeconds) > _settings.ToleranceSeconds)
                {
                    _logger.LogWarning(
                        "WebhookSignature: Timestamp outside tolerance. Age={Age:F1}s Tolerance={Tolerance}s. Contragent={Contragent} RemoteIp={RemoteIp} Path={Path}",
                        Math.Abs(age.TotalSeconds),
                        _settings.ToleranceSeconds,
                        contragent,
                        context.Connection.RemoteIpAddress,
                        context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsync(
                        "{\"error\":\"Webhook timestamp is outside the allowed tolerance window.\"}");
                    return;
                }
            }
            else
            {
                _logger.LogWarning(
                    "WebhookSignature: Invalid timestamp format '{Timestamp}'. Contragent={Contragent} RemoteIp={RemoteIp} Path={Path}",
                    timestampHeader,
                    contragent,
                    context.Connection.RemoteIpAddress,
                    context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(
                    "{\"error\":\"Invalid webhook timestamp format.\"}");
                return;
            }
        }

        _logger.LogInformation(
            "WebhookSignature: Verification succeeded. Contragent={Contragent} RemoteIp={RemoteIp} Path={Path} BodyLength={BodyLength}",
            contragent,
            context.Connection.RemoteIpAddress,
            context.Request.Path,
            bodyBytes.Length);

        await _next(context);
    }
}
