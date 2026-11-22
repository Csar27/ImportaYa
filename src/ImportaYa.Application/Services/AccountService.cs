using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Dtos;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;
using ImportaYa.Domain.Rules;

namespace ImportaYa.Application.Services;

public sealed class AccountService(
    IAccountRepository accounts,
    IPasswordHasher hasher,
    IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<AccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await accounts.ListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<AccountDto> CreateAsync(
        CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (!EmailRules.IsValid(request.Email))
        {
            throw new InvalidOperationException("El email no tiene un formato válido.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new InvalidOperationException("La contraseña debe tener al menos 8 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new InvalidOperationException("El nombre visible es obligatorio.");
        }

        if (!Enum.IsDefined(request.Role))
        {
            throw new InvalidOperationException("El rol debe ser Admin o Usuario.");
        }

        var email = request.Email.Trim();
        var normalized = email.ToUpperInvariant();

        if (await accounts.GetByNormalizedEmailAsync(normalized, cancellationToken) is not null)
        {
            throw new InvalidOperationException($"Ya existe una cuenta con el email '{email}'.");
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = hasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        account.SetEmail(email);

        await accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(account);
    }

    private static AccountDto Map(Account account) => new(
        account.Id, account.Email, account.DisplayName, account.Role.ToString(),
        account.IsActive, account.CreatedAt);
}
