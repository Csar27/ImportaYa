using ImportaYa.Application.Dtos;
using ImportaYa.Domain.Repositories;

namespace ImportaYa.Application.Services;

public sealed class UserQueryService(IUserRepository users)
{
    public async Task<PagedResult<UserDto>> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
        var (items, total) = await users.QueryAsync(term, page, pageSize, cancellationToken);

        return new PagedResult<UserDto>(
            items.Select(u => new UserDto(
                u.Id,
                u.TipoDocumento.ToString(),
                u.Documento,
                u.Nombre,
                u.Email,
                u.Telefono,
                u.CreatedAt)).ToList(),
            total, page, pageSize);
    }
}
