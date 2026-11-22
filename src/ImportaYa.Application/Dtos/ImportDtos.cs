namespace ImportaYa.Application.Dtos;

public sealed record CreateImportResult(Guid JobId, string Status);

public sealed record ImportJobDto(
    Guid Id,
    string FileName,
    string Format,
    string Status,
    int ProcessedCount,
    int CreatedCount,
    int ErrorCount,
    int DuplicateCount,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record ImportLogDto(
    long Id,
    Guid JobId,
    int Fila,
    string TipoDocumento,
    string Documento,
    string Tipo,
    string Motivo,
    DateTimeOffset Fecha);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
