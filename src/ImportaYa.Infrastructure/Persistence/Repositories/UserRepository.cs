using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImportaYa.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<(IReadOnlyList<User> Items, int Total)> QueryAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.Documento.Contains(term) ||
                u.Nombre.Contains(term) ||
                u.Email.Contains(term) ||
                (u.Telefono != null && u.Telefono.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
