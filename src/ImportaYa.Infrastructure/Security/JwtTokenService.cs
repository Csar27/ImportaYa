using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Dtos;
using ImportaYa.Application.Options;
using ImportaYa.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ImportaYa.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public LoginResult CreateToken(Account account)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Key) || settings.Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key debe configurarse con al menos 32 caracteres.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(settings.ExpiresInMinutes);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(JwtRegisteredClaimNames.UniqueName, account.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, account.Role.ToString()),
            new("displayName", account.DisplayName)
        ];

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new LoginResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            account.Email,
            account.DisplayName,
            account.Role.ToString());
    }
}
