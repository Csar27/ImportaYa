using ImportaYa.Application.Models;

namespace ImportaYa.Application.Abstractions;

public interface IImportProgressReporter
{
    Task ReportAsync(ImportProgress progress, CancellationToken cancellationToken = default);
}
