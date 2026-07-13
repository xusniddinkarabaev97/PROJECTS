namespace GzsBilling.Infrastructure.Configuration;

public class MtlsSettings
{
    public string ClientCertificatePath { get; set; } = string.Empty;
    public string ClientCertificatePassword { get; set; } = string.Empty;
    public ServerCertificateValidationSettings ServerCertificateValidation { get; set; } = new();
}

public class ServerCertificateValidationSettings
{
    public bool AllowUntrustedRoot { get; set; }
    public bool CheckCertificateRevocation { get; set; } = true;
    public List<string> RequiredSubjectPatterns { get; set; } = new();
}
