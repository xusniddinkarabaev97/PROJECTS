namespace GzsBilling.Domain.Models;

// ── Stakeholder DTOs ──
public class StakeholderDto
{
    public Guid Id { get; set; }
    public int FillingStationId { get; set; }
    public string FillingStationName { get; set; } = string.Empty;
    public int PaymentId { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public decimal SharePercent { get; set; }
    public string FullName { get; set; } = string.Empty;
}

public class CreateStakeholderRequest
{
    public int FillingStationId { get; set; }
    public int PaymentId { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public decimal SharePercent { get; set; }
    public string FullName { get; set; } = string.Empty;
}

// ── Filling Station DTOs ──
public class FillingStationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int StakeholderCount { get; set; }
}

public class CreateFillingStationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

// ── Settings DTOs ──
public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UpdateSettingRequest
{
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// ── Payment DTOs ──
public class PaymentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreatePaymentRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

public class UpdateWhiteIpsRequest
{
    public string IpAddresses { get; set; } = string.Empty;
}

public class PaymentDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? ApiToken { get; set; }
    public string? SslCertificateThumbprint { get; set; }
    public string? SslCertificatePfxBase64 { get; set; }
    public string? WhiteIpAddresses { get; set; }
    public DateTimeOffset? SslCertificateExpiresAt { get; set; }
}

// ── Dashboard Stats ──
public class DispenserDto
{
    public int Id { get; set; }
    public int FillingStationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateDispenserRequest
{
    public string Name { get; set; } = string.Empty;
    public string FuelType { get; set; } = "AI-92";
}

public class DashboardStats
{
    public int TotalFillingStations { get; set; }
    public int TotalStakeholders { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TodayTotalAmount { get; set; }
    public int PendingDisbursements { get; set; }
}
