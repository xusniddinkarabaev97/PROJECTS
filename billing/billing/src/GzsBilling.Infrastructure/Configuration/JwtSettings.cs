namespace GzsBilling.Infrastructure.Configuration;

public class JwtSettings
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string IssuerSigningKey { get; set; } = string.Empty;
    public TokenValidationParametersConfig TokenValidationParameters { get; set; } = new();
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationHours { get; set; } = 24;
}

public class TokenValidationParametersConfig
{
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public string ClockSkew { get; set; } = "00:00:30";
}
