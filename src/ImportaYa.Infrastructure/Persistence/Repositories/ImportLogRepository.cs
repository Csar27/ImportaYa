using System.Runtime.CompilerServices;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ImportaYa.Infrastructure.Persistence.Repositories;

public sealed class ImportLogRepository(AppDbContext db) : IImportLogRepository
{
    public Task AddRangeAsync(IEnumerable<ImportLogEntry> entries, CancellationToken cancellationToken = default) =>
        db.ImportLogs.AddRangeAsync(entries, cancellationToken);

    public async Task<(IReadOnlyList<ImportLogEntry> Items, int Total)> QueryAsync(
        Guid jobId, LogTipo? tipo, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Filter(jobId, tipo);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(l => l.Fila)
            .ThenBy(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async IAsyncEnumerable<ImportLogEntry> StreamAsync(
        Guid jobId,
        LogTipo? tipo,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var entry in Filter(jobId, tipo)
                           .OrderBy(l => l.Fila)
                           .ThenBy(l => l.Id)
                           .AsAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            yield return entry;
        }
    }

    private IQueryable<ImportLogEntry> Filter(Guid jobId, LogTipo? tipo)
    {
        var query = db.ImportLogs.AsNoTracking().Where(l => l.JobId == jobId);
        return tipo.HasValue ? query.Where(l => l.Tipo == tipo.Value) : query;
    }
}
