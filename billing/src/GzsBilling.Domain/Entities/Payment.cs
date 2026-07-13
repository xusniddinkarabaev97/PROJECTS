namespace GzsBilling.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ApiToken { get; set; }
    public string? SslCertificateThumbprint { get; set; }
    public string? SslCertificatePfxBase64 { get; set; }
    public string? WhiteIpAddresses { get; set; } // comma-separated or JSON array
    public DateTimeOffset? SslCertificateExpiresAt { get; set; }
    public ICollection<Tranzaksiya> Tranzaktsiyalar { get; set; } = new List<Tranzaksiya>();
}
