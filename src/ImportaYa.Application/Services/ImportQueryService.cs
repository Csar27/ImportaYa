using ImportaYa.Application.Dtos;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;

namespace ImportaYa.Application.Services;

public sealed class ImportQueryService(
    IImportJobRepository jobs,
    IImportLogRepository logs)
{
    public async Task<ImportJobDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await jobs.GetByIdAsync(id, cancellationToken);
        return job is null ? null : Map(job);
    }

    public async Task<PagedResult<ImportJobDto>> ListAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var items = await jobs.ListAsync(page, pageSize, cancellationToken);
        var total = await jobs.CountAsync(cancellationToken);

        return new PagedResult<ImportJobDto>(items.Select(Map).ToList(), total, page, pageSize);
    }

    public async Task<PagedResult<ImportLogDto>?> GetLogAsync(
        Guid jobId, LogTipo? tipo, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (await jobs.GetByIdAsync(jobId, cancellationToken) is null)
        {
            return null;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var (items, total) = await logs.QueryAsync(jobId, tipo, page, pageSize, cancellationToken);

        return new PagedResult<ImportLogDto>(
            items.Select(l => new ImportLogDto(
                l.Id, l.JobId, l.Fila, l.TipoDocumento, l.Documento,
                l.Tipo.ToString(), l.Motivo, l.Fecha)).ToList(),
            total, page, pageSize);
    }

    private static ImportJobDto Map(Domain.Entities.ImportJob job) => new(
        job.Id,
        job.FileName,
        job.Format.ToString(),
        job.Status.ToString(),
        job.ProcessedCount,
        job.CreatedCount,
        job.ErrorCount,
        job.DuplicateCount,
        job.ErrorMessage,
        job.CreatedAt,
        job.StartedAt,
        job.CompletedAt);
}
