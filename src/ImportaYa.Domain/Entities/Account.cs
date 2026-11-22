using ImportaYa.Domain.Enums;

namespace ImportaYa.Domain.Entities;

/// <summary>Cuenta de acceso a la API (no confundir con User, que es un registro importado).</summary>
public sealed class Account
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AppRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public void SetEmail(string email)
    {
        Email = email.Trim();
        NormalizedEmail = Email.ToUpperInvariant();
    }
}
