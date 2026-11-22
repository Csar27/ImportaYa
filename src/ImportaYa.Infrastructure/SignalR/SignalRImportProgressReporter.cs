using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Dtos;
using ImportaYa.Application.Models;
using Microsoft.AspNetCore.SignalR;

namespace ImportaYa.Infrastructure.SignalR;

/// <summary>Publica el progreso persistido en el grupo SignalR del trabajo.</summary>
public sealed class SignalRImportProgressReporter(IHubContext<ImportProgressHub> hub)
    : IImportProgressReporter
{
    public Task ReportAsync(ImportProgress progress, CancellationToken cancellationToken = default)
    {
        var payload = new ImportProgressEvent(
            progress.JobId,
            progress.Processed,
            progress.Created,
            progress.Errors,
            progress.Duplicates,
            progress.Status.ToString());

        return hub.Clients
            .Group(ImportProgressHub.GroupName(progress.JobId))
            .SendAsync("progress", payload, cancellationToken);
    }
}
