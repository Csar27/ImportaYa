using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Options;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;
using ImportaYa.Domain.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Crea el administrador inicial solo si se configuran email y contraseña por entorno.</summary>
public sealed class AccountSeeder(
    IAccountRepository accounts,
    IPasswordHasher hasher,
    IUnitOfWork unitOfWork,
    IOptions<AdminSeedOptions> options,
    ILogger<AccountSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seed = options.Value;

        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogWarning(
                "No se creó el administrador inicial. Defina AdminSeed__Email y AdminSeed__Password.");
            return;
        }

        if (!EmailRules.IsValid(seed.Email) || seed.Password.Length < 8)
        {
            throw new InvalidOperationException(
                "AdminSeed: el email debe ser válido y la contraseña tener al menos 8 caracteres.");
        }

        var normalized = seed.Email.Trim().ToUpperInvariant();
        if (await accounts.GetByNormalizedEmailAsync(normalized, cancellationToken) is not null)
        {
            return;
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            DisplayName = string.IsNullOrWhiteSpace(seed.DisplayName) ? "Administrador" : seed.DisplayName.Trim(),
            PasswordHash = hasher.Hash(seed.Password),
            Role = AppRole.Admin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        account.SetEmail(seed.Email);

        await accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Administrador inicial creado: {Email}", account.Email);
    }
}
