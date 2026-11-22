using ImportaYa.Domain.Enums;

namespace ImportaYa.Domain.Entities;

/// <summary>Agregado que representa una carga. Guarda estado, formato y contadores en curso.</summary>
public sealed class ImportJob
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFilePath { get; set; } = string.Empty;
    public ImportFormat Format { get; set; }
    public ImportStatus Status { get; set; }
    public int ProcessedCount { get; set; }
    public int CreatedCount { get; set; }
    public int ErrorCount { get; set; }
    public int DuplicateCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public void MarkProcessing()
    {
        Status = ImportStatus.Processing;
        StartedAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    public void MarkCompleted()
    {
        Status = ImportStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string message)
    {
        Status = ImportStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = message.Length > 2000 ? message[..2000] : message;
    }

    public void SetCounters(int processed, int created, int errors, int duplicates)
    {
        ProcessedCount = processed;
        CreatedCount = created;
        ErrorCount = errors;
        DuplicateCount = duplicates;
    }
}
