using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ImportaYa.Application.Dtos;
using ImportaYa.Domain.Enums;
using Xunit;

namespace ImportaYa.IntegrationTests;

public sealed class AuthTests(IntegrationFixture fixture) : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture = fixture;

    [Fact]
    public async Task Login_returns_a_token_for_seeded_admin()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(IntegrationFixture.AdminEmail, IntegrationFixture.AdminPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        result.Role.Should().Be("Admin");
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Wrong_password_returns_401()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(IntegrationFixture.AdminEmail, "PasswordIncorrecta123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Import_endpoints_require_authentication()
    {
        using var client = _fixture.Factory.CreateClient();

        (await client.GetAsync("/api/imports")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_returns_the_authenticated_users_claims()
    {
        var response = await _fixture.Client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        me!.Email.Should().Be(IntegrationFixture.AdminEmail);
        me.Role.Should().Be("Admin");
        me.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Admin_creates_user_and_user_can_import_but_not_manage_accounts()
    {
        var email = $"usuario-{Guid.NewGuid():N}@demo.com";
        const string password = "Usuario12345";

        var created = await _fixture.Client.PostAsJsonAsync("/api/accounts",
            new CreateAccountRequest(email, password, "Usuario de prueba", AppRole.Usuario));
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        using var userClient = _fixture.Factory.CreateClient();
        var login = await userClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password));
        login.EnsureSuccessStatusCode();

        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.Token;
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        (await userClient.GetAsync("/api/imports")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await userClient.GetAsync("/api/accounts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Creating_an_account_with_a_short_password_returns_400()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/accounts",
            new CreateAccountRequest($"corto-{Guid.NewGuid():N}@demo.com", "abc", "Corto", AppRole.Usuario));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("al menos 8 caracteres");
    }

    [Fact]
    public async Task Public_register_creates_a_Usuario_that_can_import_but_not_manage_accounts()
    {
        var email = $"registro-{Guid.NewGuid():N}@demo.com";
        const string password = "Registro12345";

        using var anonymous = _fixture.Factory.CreateClient();
        var created = await anonymous.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password, "Usuario registrado"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await created.Content.ReadFromJsonAsync<AccountDto>();
        account!.Role.Should().Be("Usuario");
        account.IsActive.Should().BeTrue();

        var login = await anonymous.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password));
        login.EnsureSuccessStatusCode();

        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.Token;
        using var userClient = _fixture.Factory.CreateClient();
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        (await userClient.GetAsync("/api/imports")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await userClient.GetAsync("/api/accounts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Public_register_rejects_weak_passwords_and_duplicate_emails()
    {
        using var anonymous = _fixture.Factory.CreateClient();

        var weak = await anonymous.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"debil-{Guid.NewGuid():N}@demo.com", "abc", "Débil"));
        weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var duplicate = await anonymous.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(IntegrationFixture.AdminEmail, "OtraPass12345", "Duplicado"));
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await duplicate.Content.ReadAsStringAsync()).Should().Contain("Ya existe una cuenta");
    }
}
