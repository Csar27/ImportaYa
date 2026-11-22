using ImportaYa.Domain.Entities;

namespace ImportaYa.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
}
