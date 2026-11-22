using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;

namespace ImportaYa.Infrastructure.Background;

/// <summary>Implementación de respaldo sin notificaciones; el progreso sigue persistiéndose en ImportJob.</summary>
public sealed class NoOpImportProgressReporter : IImportProgressReporter
{
    public Task ReportAsync(ImportProgress progress, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
