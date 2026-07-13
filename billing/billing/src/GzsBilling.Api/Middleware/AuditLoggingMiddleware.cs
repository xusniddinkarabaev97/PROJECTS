using System.Diagnostics;
using System.Globalization;
using GzsBilling.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace GzsBilling.Api.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly AuditLogSettings _settings;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger,
        IOptions<AuditLogSettings> options)
    {
        _next = next;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var transactionId = Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Transaction-Id"] = transactionId;

        using var _ = LogContext.PushProperty("TransactionId", transactionId);

        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogAuditRecord(context, transactionId, timestamp, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }

        stopwatch.Stop();
        LogAuditRecord(context, transactionId, timestamp, stopwatch.ElapsedMilliseconds, null);
    }

    private void LogAuditRecord(
        HttpContext context,
        string transactionId,
        string timestamp,
        long durationMs,
        Exception? exception)
    {
        var sourceIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;
        var statusCode = context.Response.StatusCode;
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

        var isError = statusCode >= 400;

        if (isError || exception != null)
        {
            _logger.LogWarning(
                exception,
                "Audit: {Timestamp} | {SourceIp} | {Method} {Path} | Status={StatusCode} | Duration={DurationMs}ms | TxId={TransactionId} | UA={UserAgent}",
                timestamp,
                sourceIp,
                method,
                path,
                statusCode,
                durationMs,
                transactionId,
                userAgent);
        }
        else
        {
            _logger.LogInformation(
                "Audit: {Timestamp} | {SourceIp} | {Method} {Path} | Status={StatusCode} | Duration={DurationMs}ms | TxId={TransactionId} | UA={UserAgent}",
                timestamp,
                sourceIp,
                method,
                path,
                statusCode,
                durationMs,
                transactionId,
                userAgent);
        }

        if (IsWebhookPath(path))
        {
            var eventType = context.Request.Headers["X-Webhook-Event-Type"].FirstOrDefault() ?? "unknown";
            _logger.LogInformation(
                "WebhookAudit: TxId={TransactionId} | EventType={EventType} | Status={StatusCode} | Duration={DurationMs}ms",
                transactionId,
                eventType,
                statusCode,
                durationMs);
        }
    }

    private static bool IsWebhookPath(string path)
    {
        return path.StartsWith("/api/webhooks", StringComparison.OrdinalIgnoreCase);
    }
}
