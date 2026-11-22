using ImportaYa.Domain.Entities;

namespace ImportaYa.Domain.Repositories;

public interface IUserRepository
{
    Task<(IReadOnlyList<User> Items, int Total)> QueryAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}
