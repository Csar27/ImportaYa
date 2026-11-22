using ImportaYa.Domain.Enums;

namespace ImportaYa.Application.Dtos;

public sealed record LoginRequest(string Email, string Password);

/// <summary>Registro público: el rol siempre se fuerza a Usuario en la API.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginResult(
    string Token,
    DateTimeOffset ExpiresAt,
    string Email,
    string DisplayName,
    string Role);

public sealed record CurrentUserDto(Guid Id, string Email, string DisplayName, string Role);

public sealed record AccountDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateAccountRequest(string Email, string Password, string DisplayName, AppRole Role);
