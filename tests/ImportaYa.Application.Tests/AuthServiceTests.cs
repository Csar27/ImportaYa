using FluentAssertions;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Dtos;
using ImportaYa.Application.Services;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace ImportaYa.Application.Tests;

public sealed class AuthServiceTests
{
    private static Account Admin(bool active = true)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            DisplayName = "Admin",
            PasswordHash = "hash",
            Role = AppRole.Admin,
            IsActive = active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        account.SetEmail("admin@demo.com");
        return account;
    }

    private static (AuthService Service, IPasswordHasher Hasher) Create(Account? account)
    {
        var accounts = Substitute.For<IAccountRepository>();
        accounts.GetByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(account);

        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("hash", "correcta").Returns(true);

        var tokens = Substitute.For<ITokenService>();
        tokens.CreateToken(Arg.Any<Account>()).Returns(call =>
        {
            var value = call.Arg<Account>();
            return new LoginResult("token-falso", DateTimeOffset.UtcNow.AddHours(1),
                value.Email, value.DisplayName, value.Role.ToString());
        });

        return (new AuthService(accounts, hasher, tokens), hasher);
    }

    [Fact]
    public async Task Valid_credentials_return_a_token()
    {
        var (service, _) = Create(Admin());

        var result = await service.AuthenticateAsync("admin@demo.com", "correcta");

        result.Should().NotBeNull();
        result!.Email.Should().Be("admin@demo.com");
        result.Role.Should().Be("Admin");
        result.Token.Should().Be("token-falso");
    }

    [Fact]
    public async Task Wrong_password_returns_null()
    {
        var (service, _) = Create(Admin());

        (await service.AuthenticateAsync("admin@demo.com", "incorrecta")).Should().BeNull();
    }

    [Fact]
    public async Task Inactive_account_returns_null()
    {
        var (service, _) = Create(Admin(active: false));

        (await service.AuthenticateAsync("admin@demo.com", "correcta")).Should().BeNull();
    }

    [Fact]
    public async Task Missing_account_returns_null()
    {
        var (service, _) = Create(null);

        (await service.AuthenticateAsync("nadie@demo.com", "correcta")).Should().BeNull();
    }
}
