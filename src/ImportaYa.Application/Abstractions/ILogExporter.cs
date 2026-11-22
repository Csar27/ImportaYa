using ImportaYa.Domain.Enums;

namespace ImportaYa.Application.Abstractions;

public interface ILogExporter
{
    Task ExportAsync(Guid jobId, LogTipo? tipo, ImportFormat format, Stream destination, CancellationToken cancellationToken = default);
}
