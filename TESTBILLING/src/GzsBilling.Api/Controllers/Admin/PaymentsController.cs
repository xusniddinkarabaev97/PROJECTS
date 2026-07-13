using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Models;
using GzsBilling.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GzsBilling.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/payments")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin")]
public class PaymentsController : ControllerBase
{
    private readonly GzsBillingDbContext _dbContext;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        GzsBillingDbContext dbContext,
        ILogger<PaymentsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var payments = await _dbContext.Payments
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        return Ok(payments);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var payments = await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        return Ok(payments);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync(ct);

        if (payment is null)
            return NotFound(new { error = "NOT_FOUND", message = "Payment provider not found." });

        return Ok(payment);
    }

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "INVALID_NAME", message = "Payment provider name is required." });

        var duplicate = await _dbContext.Payments
            .AnyAsync(p => p.Name == request.Name.Trim(), ct);
        if (duplicate)
            return BadRequest(new { error = "DUPLICATE", message = "A payment provider with this name already exists." });

        var payment = new Payment
        {
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Payment provider {Id} '{Name}' created", payment.Id, payment.Name);

        var dto = new PaymentDto
        {
            Id = payment.Id,
            Name = payment.Name,
            IsActive = payment.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CreatePaymentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "INVALID_NAME", message = "Payment provider name is required." });

        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (payment is null)
            return NotFound(new { error = "NOT_FOUND", message = "Payment provider not found." });

        if (!string.Equals(payment.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _dbContext.Payments
                .AnyAsync(p => p.Id != id && p.Name == request.Name.Trim(), ct);
            if (duplicate)
                return BadRequest(new { error = "DUPLICATE", message = "Another payment provider with this name already exists." });
        }

        payment.Name = request.Name.Trim();

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Payment provider {Id} '{Name}' updated", payment.Id, payment.Name);

        var dto = new PaymentDto
        {
            Id = payment.Id,
            Name = payment.Name,
            IsActive = payment.IsActive
        };

        return Ok(dto);
    }

    [HttpPut("{id:int}/token")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToken(int id, [FromBody] UpdateTokenRequest request, CancellationToken ct)
    {
        var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null)
            return NotFound(new { error = "NOT_FOUND", message = "Payment provider not found." });
        payment.ApiToken = request.Token?.Trim();
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Payment provider {Id} API token updated", payment.Id);
        return Ok(new { message = "API token updated successfully." });
    }

    [HttpPost("{id:int}/certificate")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(typeof(CertificateGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateCertificate(int id, [FromBody] CertificateGenerationRequest request, CancellationToken ct)
    {
        var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null)
            return NotFound(new { error = "NOT_FOUND", message = "Payment provider not found." });
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest($"CN={request.CommonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(request.ValidDays));
        var pfxBytes = cert.Export(X509ContentType.Pfx, "gzs-billing");
        payment.SslCertificateThumbprint = cert.Thumbprint;
        payment.SslCertificatePfxBase64 = Convert.ToBase64String(pfxBytes);
        payment.SslCertificateExpiresAt = cert.NotAfter;
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("SSL certificate generated for payment provider {Id}", payment.Id);
        return Ok(new CertificateGenerationResponse { Thumbprint = cert.Thumbprint, PfxBase64 = Convert.ToBase64String(pfxBytes), ExpiresAt = cert.NotAfter });
    }

    [HttpPut("{id:int}/whiteips")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWhiteIps(int id, [FromBody] UpdateWhiteIpsRequest request, CancellationToken ct)
    {
        var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null)
            return NotFound(new { error = "NOT_FOUND", message = "Payment provider not found." });
        payment.WhiteIpAddresses = request.IpAddresses?.Trim();
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("White IPs updated for payment provider {Id}", payment.Id);
        return Ok(new { message = "White IP addresses updated successfully." });
    }

    [HttpGet("{id:int}/details")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(typeof(PaymentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(int id, CancellationToken ct)
    {
        var payment = await _dbContext.Payments.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new PaymentDetailDto
            {
                Id = p.Id, Name = p.Name, IsActive = p.IsActive, CreatedAt = p.CreatedAt,
                ApiToken = p.ApiToken, SslCertificateThumbprint = p.SslCertificateThumbprint,
                SslCertificatePfxBase64 = p.SslCertificatePfxBase64, SslCertificateExpiresAt = p.SslCertificateExpiresAt,
                WhiteIpAddresses = p.WhiteIpAddresses
            }).FirstOrDefaultAsync(ct);
        if (payment is null)
            return NotFound(new { error = "NOT_FOUND", message = "Payment provider not found." });
        return Ok(payment);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "superadmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (payment is null)
            return NotFound(new { error = "NOT_FOUND", message = "Payment provider not found." });

        payment.IsActive = false;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Payment provider {Id} '{Name}' deactivated", payment.Id, payment.Name);

        return NoContent();
    }
}
