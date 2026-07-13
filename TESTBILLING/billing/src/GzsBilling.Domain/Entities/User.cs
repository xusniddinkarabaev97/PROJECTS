using GzsBilling.Domain.Enums;

namespace GzsBilling.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public SystemRole Role { get; set; } = SystemRole.ReadOnly;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
