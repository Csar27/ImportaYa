using ImportaYa.Application.Dtos;
using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;
using ImportaYa.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace ImportaYa.Infrastructure.Tests;

public sealed class SignalRImportProgressReporterTests
{
    [Fact]
    public async Task ReportAsync_sends_the_progress_to_the_job_group()
    {
        var clients = Substitute.For<IHubClients>();
        var proxy = Substitute.For<IClientProxy>();
        var hub = Substitute.For<IHubContext<ImportProgressHub>>();
        hub.Clients.Returns(clients);
        clients.Group(Arg.Any<string>()).Returns(proxy);

        var reporter = new SignalRImportProgressReporter(hub);
        var jobId = Guid.NewGuid();

        await reporter.ReportAsync(
            new ImportProgress(jobId, 42, 40, 1, 1, ImportStatus.Processing));

        var group = $"job:{jobId}";
        clients.Received(1).Group(group);

        await proxy.Received(1).SendCoreAsync(
            "progress",
            Arg.Is<object?[]>(args =>
                args.Length == 1 &&
                ((ImportProgressEvent)args[0]!).JobId == jobId &&
                ((ImportProgressEvent)args[0]!).Processed == 42 &&
                ((ImportProgressEvent)args[0]!).Created == 40 &&
                ((ImportProgressEvent)args[0]!).Errors == 1 &&
                ((ImportProgressEvent)args[0]!).Duplicates == 1 &&
                ((ImportProgressEvent)args[0]!).Status == "Processing"),
            Arg.Any<CancellationToken>());
    }
}
