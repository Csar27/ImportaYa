using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;

namespace ImportaYa.Infrastructure.Background;

/// <summary>Implementación temporal: el progreso ya se persiste en ImportJob. SignalR llega en la Fase 3.</summary>
public sealed class NoOpImportProgressReporter : IImportProgressReporter
{
    public Task ReportAsync(ImportProgress progress, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
