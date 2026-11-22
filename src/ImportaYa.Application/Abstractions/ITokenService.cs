using ImportaYa.Application.Dtos;
using ImportaYa.Domain.Entities;

namespace ImportaYa.Application.Abstractions;

public interface ITokenService
{
    LoginResult CreateToken(Account account);
}
