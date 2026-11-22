using ImportaYa.Domain.Entities;

namespace ImportaYa.Domain.Repositories;

public interface IImportJobRepository
{
    Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportJob>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ImportJob job, CancellationToken cancellationToken = default);
    void Update(ImportJob job);
}
