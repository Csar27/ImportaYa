using ImportaYa.Application.Abstractions;
using ImportaYa.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ImportaYa.Infrastructure.Security;

/// <summary>Hash PBKDF2 compatible con ASP.NET Core Identity, sin acoplar el resto de capas a Identity.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<Account> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
