using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GzsBilling.Infrastructure.Configuration;

namespace GzsBilling.Infrastructure.Services;

public interface IMtlsHttpClientFactory
{
    HttpClient CreateClient(string name, string baseUrl);
}

public class MtlsHttpClientFactory : IMtlsHttpClientFactory
{
    private readonly MtlsSettings _settings;
    private readonly ILogger<MtlsHttpClientFactory> _logger;
    private readonly X509Certificate2? _clientCertificate;

    public MtlsHttpClientFactory(IOptions<MtlsSettings> options, ILogger<MtlsHttpClientFactory> logger)
    {
        _settings = options.Value;
        _logger = logger;

        // Load client certificate
        if (!string.IsNullOrEmpty(_settings.ClientCertificatePath) && File.Exists(_settings.ClientCertificatePath))
        {
            _clientCertificate = string.IsNullOrEmpty(_settings.ClientCertificatePassword)
                ? new X509Certificate2(_settings.ClientCertificatePath)
                : new X509Certificate2(_settings.ClientCertificatePath, _settings.ClientCertificatePassword);

            _logger.LogInformation("mTLS client certificate loaded from {Path}, thumbprint: {Thumbprint}",
                _settings.ClientCertificatePath, _clientCertificate.Thumbprint);
        }
    }

    public HttpClient CreateClient(string name, string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls13,
            CheckCertificateRevocationList = _settings.ServerCertificateValidation.CheckCertificateRevocation,
            ServerCertificateCustomValidationCallback = ValidateServerCertificate
        };

        if (_clientCertificate != null)
        {
            handler.ClientCertificates.Add(_clientCertificate);
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.Add("User-Agent", "GzsBilling/1.0");

        _logger.LogInformation("Created mTLS HttpClient '{Name}' for {BaseUrl}", name, baseUrl);

        return client;
    }

    private bool ValidateServerCertificate(HttpRequestMessage request, X509Certificate2? cert,
        X509Chain? chain, SslPolicyErrors sslErrors)
    {
        if (cert == null)
        {
            _logger.LogError("mTLS: Server presented no certificate for {Url}", request.RequestUri);
            return false;
        }

        var subject = cert.Subject;

        if (_settings.ServerCertificateValidation.RequiredSubjectPatterns.Count > 0)
        {
            var matches = _settings.ServerCertificateValidation.RequiredSubjectPatterns.Any(pattern =>
                MatchSubjectPattern(subject, pattern));

            if (!matches)
            {
                _logger.LogError("mTLS: Server certificate subject '{Subject}' doesn't match required patterns", subject);
                return false;
            }
        }

        if (_settings.ServerCertificateValidation.AllowUntrustedRoot)
        {
            _logger.LogWarning("mTLS: Allowing untrusted root certificate for {Url} - DEVEL ONLY", request.RequestUri);
            return true;
        }

        if (sslErrors != SslPolicyErrors.None)
        {
            _logger.LogError("mTLS: SSL validation failed for {Url}: {Errors}", request.RequestUri, sslErrors);
            return false;
        }

        _logger.LogDebug("mTLS: Server certificate '{Subject}' validated successfully", subject);
        return true;
    }

    private static bool MatchSubjectPattern(string subject, string pattern)
    {
        if (pattern.StartsWith("CN="))
        {
            var cn = pattern[3..];
            // Extract CN from subject (format: "CN=name, O=org, ...")
            var cnMatch = subject.Split(',').FirstOrDefault(s => s.Trim().StartsWith("CN="));
            if (cnMatch == null) return false;
            var subjectCn = cnMatch.Trim()[3..];

            // Simple wildcard matching
            if (cn.StartsWith("*."))
            {
                return subjectCn.EndsWith(cn[1..], StringComparison.OrdinalIgnoreCase);
            }
            return string.Equals(subjectCn, cn, StringComparison.OrdinalIgnoreCase);
        }

        return subject.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
