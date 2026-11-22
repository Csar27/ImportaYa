using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;

namespace ImportaYa.Domain.Repositories;

public interface IImportLogRepository
{
    Task AddRangeAsync(IEnumerable<ImportLogEntry> entries, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ImportLogEntry> Items, int Total)> QueryAsync(
        Guid jobId, LogTipo? tipo, int page, int pageSize, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ImportLogEntry> StreamAsync(
        Guid jobId, LogTipo? tipo, CancellationToken cancellationToken = default);
}
