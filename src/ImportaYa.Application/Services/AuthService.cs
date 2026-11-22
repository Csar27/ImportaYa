using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Dtos;
using ImportaYa.Domain.Repositories;

namespace ImportaYa.Application.Services;

public sealed class AuthService(
    IAccountRepository accounts,
    IPasswordHasher hasher,
    ITokenService tokenService)
{
    public async Task<LoginResult?> AuthenticateAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var account = await accounts.GetByNormalizedEmailAsync(email.Trim().ToUpperInvariant(), cancellationToken);

        if (account is null || !account.IsActive || !hasher.Verify(account.PasswordHash, password))
        {
            return null;
        }

        return tokenService.CreateToken(account);
    }
}
