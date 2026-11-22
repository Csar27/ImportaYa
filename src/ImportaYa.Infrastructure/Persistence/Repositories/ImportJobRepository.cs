using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImportaYa.Infrastructure.Persistence.Repositories;

public sealed class ImportJobRepository(AppDbContext db) : IImportJobRepository
{
    public Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ImportJobs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ImportJob>> ListAsync(
        int page, int pageSize, CancellationToken cancellationToken = default) =>
        await db.ImportJobs
            .AsNoTracking()
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        db.ImportJobs.CountAsync(cancellationToken);

    public Task AddAsync(ImportJob job, CancellationToken cancellationToken = default) =>
        db.ImportJobs.AddAsync(job, cancellationToken).AsTask();

    public void Update(ImportJob job) => db.ImportJobs.Update(job);
}
