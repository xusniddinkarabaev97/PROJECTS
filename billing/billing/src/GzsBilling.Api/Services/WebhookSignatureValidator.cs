using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GzsBilling.Infrastructure.Configuration;

namespace GzsBilling.Api.Services;

public interface IWebhookSignatureValidator
{
    (bool IsValid, string Error) Validate(string contragentId, byte[] body, string signature);
}

public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    private readonly WebhookSettings _settings;
    private readonly ILogger<WebhookSignatureValidator> _logger;

    public WebhookSignatureValidator(IOptions<WebhookSettings> options, ILogger<WebhookSignatureValidator> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public (bool IsValid, string Error) Validate(string contragentId, byte[] body, string signature)
    {
        if (!_settings.ContragentSecrets.TryGetValue(contragentId, out var secretBase64))
        {
            _logger.LogWarning("Webhook signature validation: Unknown contragent {ContragentId}", contragentId);
            return (false, "Unknown contragent");
        }

        var secretBytes = Convert.FromBase64String(secretBase64);
        byte[] computedSignature;

        if (_settings.SignatureAlgorithm.Equals("HMACSHA256", StringComparison.OrdinalIgnoreCase))
        {
            using var hmac = new HMACSHA256(secretBytes);
            computedSignature = hmac.ComputeHash(body);
        }
        else if (_settings.SignatureAlgorithm.Equals("HMACSHA512", StringComparison.OrdinalIgnoreCase))
        {
            using var hmac = new HMACSHA512(secretBytes);
            computedSignature = hmac.ComputeHash(body);
        }
        else
        {
            _logger.LogError("Webhook: Unsupported signature algorithm {Algorithm}", _settings.SignatureAlgorithm);
            return (false, "Unsupported signature algorithm");
        }

        var computedHex = Convert.ToHexString(computedSignature).ToLowerInvariant();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHex),
                Encoding.UTF8.GetBytes(signature.ToLowerInvariant())))
        {
            _logger.LogWarning("Webhook signature mismatch for contragent {ContragentId}. Expected: {Expected}, Got: {Got}",
                contragentId, computedHex, signature);
            return (false, "Invalid signature");
        }

        _logger.LogInformation("Webhook signature validated successfully for contragent {ContragentId}", contragentId);
        return (true, string.Empty);
    }
}
