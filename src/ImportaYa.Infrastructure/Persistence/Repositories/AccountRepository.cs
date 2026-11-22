using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImportaYa.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(AppDbContext db) : IAccountRepository
{
    public Task<Account?> GetByNormalizedEmailAsync(
        string normalizedEmail, CancellationToken cancellationToken = default) =>
        db.Accounts.AsNoTracking().FirstOrDefaultAsync(
            a => a.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Accounts.AsNoTracking().OrderBy(a => a.Email).ToListAsync(cancellationToken);

    public Task AddAsync(Account account, CancellationToken cancellationToken = default) =>
        db.Accounts.AddAsync(account, cancellationToken).AsTask();
}
