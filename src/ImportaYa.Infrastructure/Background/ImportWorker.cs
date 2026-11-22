using ImportaYa.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImportaYa.Infrastructure.Background;

public sealed class ImportWorker(
    ChannelImportQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IImportProcessor>();

            try
            {
                await processor.ProcessAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // El processor ya persistió el estado Failed; aquí solo evitamos que el worker muera.
                logger.LogError(ex, "El trabajo {JobId} terminó con una excepción no controlada", jobId);
            }
        }
    }
}
