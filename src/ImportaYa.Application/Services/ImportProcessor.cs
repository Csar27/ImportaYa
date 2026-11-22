using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Application.Options;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImportaYa.Application.Services;

/// <summary>
/// Procesa un trabajo en streaming y por lotes. Ninguna fila inválida detiene la carga.
/// La deduplicación se hace en dos niveles: HashSet del archivo y consulta por lotes contra BD.
/// </summary>
public sealed class ImportProcessor(
    IOptions<ImportOptions> options,
    IFileParserFactory parserFactory,
    IFileStorage fileStorage,
    IImportJobRepository jobs,
    IImportLogRepository logs,
    IUserBulkWriter userWriter,
    IUserRowValidator validator,
    IImportProgressReporter progressReporter,
    IUnitOfWork unitOfWork,
    ILogger<ImportProcessor> logger) : IImportProcessor
{
    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await jobs.GetByIdAsync(jobId, cancellationToken)
                 ?? throw new InvalidOperationException($"No existe el trabajo {jobId}.");

        job.MarkProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var batchSize = Math.Max(1, options.Value.BatchSize);
        var pendingUsers = new List<User>(batchSize);
        var pendingLogs = new List<ImportLogEntry>(batchSize);
        // Clave -> número de fila de la primera aparición dentro del archivo.
        var firstRowByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        var processed = 0;

        try
        {
            var parser = parserFactory.GetFor(Path.GetExtension(job.FileName));

            await using var stream = await fileStorage.OpenReadAsync(job.StoredFilePath, cancellationToken);

            await foreach (var row in parser.ParseAsync(stream, cancellationToken).WithCancellation(cancellationToken))
            {
                processed++;

                if (processed > options.Value.MaxRows)
                {
                    throw new InvalidOperationException(
                        $"El archivo supera el límite de {options.Value.MaxRows} filas.");
                }

                var result = validator.Validate(row);

                if (!result.IsValid)
                {
                    pendingLogs.Add(LogEntry(job.Id, row.RowNumber, row.TipoDocumento, row.Documento,
                        LogTipo.Error, result.Error!));
                }
                else
                {
                    var key = DocumentKey.From(result.User!).Value;

                    if (firstRowByKey.TryGetValue(key, out var firstRow))
                    {
                        pendingLogs.Add(LogEntry(job.Id, row.RowNumber, row.TipoDocumento, row.Documento,
                            LogTipo.Duplicado, $"Repetido en el archivo, fila {firstRow}"));
                    }
                    else
                    {
                        firstRowByKey.Add(key, row.RowNumber);
                        pendingUsers.Add(ToUser(result.User!));
                    }
                }

                if (pendingUsers.Count + pendingLogs.Count >= batchSize)
                {
                    await FlushAsync(job, pendingUsers, pendingLogs, firstRowByKey, processed, cancellationToken);
                }
            }

            if (pendingUsers.Count > 0 || pendingLogs.Count > 0)
            {
                await FlushAsync(job, pendingUsers, pendingLogs, firstRowByKey, processed, cancellationToken);
            }

            job.MarkCompleted();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await ReportAsync(job, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al procesar la carga {JobId}", jobId);
            job.MarkFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await ReportAsync(job, cancellationToken);
        }
    }

    private async Task FlushAsync(
        ImportJob job,
        List<User> pendingUsers,
        List<ImportLogEntry> pendingLogs,
        Dictionary<string, int> firstRowByKey,
        int processed,
        CancellationToken cancellationToken)
    {
        var createdNow = 0;

        if (pendingUsers.Count > 0)
        {
            var keys = pendingUsers.Select(u => new DocumentKey(u.TipoDocumento, u.Documento));
            var existing = await userWriter.FindExistingKeysAsync(keys, cancellationToken);

            var accepted = new List<User>(pendingUsers.Count);

            foreach (var user in pendingUsers)
            {
                var key = new DocumentKey(user.TipoDocumento, user.Documento).Value;

                if (existing.Contains(key))
                {
                    pendingLogs.Add(LogEntry(job.Id, firstRowByKey[key], user.TipoDocumento.ToString(),
                        user.Documento, LogTipo.Duplicado, "Ya existe en la base de datos"));
                }
                else
                {
                    accepted.Add(user);
                }
            }

            if (accepted.Count > 0)
            {
                await userWriter.WriteAsync(accepted, cancellationToken);
                createdNow = accepted.Count;
            }
        }

        if (pendingLogs.Count > 0)
        {
            await logs.AddRangeAsync(pendingLogs, cancellationToken);
        }

        var errorsNow = pendingLogs.Count(l => l.Tipo == LogTipo.Error);
        var duplicatesNow = pendingLogs.Count(l => l.Tipo == LogTipo.Duplicado);

        job.SetCounters(
            processed,
            job.CreatedCount + createdNow,
            job.ErrorCount + errorsNow,
            job.DuplicateCount + duplicatesNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await ReportAsync(job, cancellationToken);

        pendingUsers.Clear();
        pendingLogs.Clear();
    }

    private Task ReportAsync(ImportJob job, CancellationToken cancellationToken) =>
        progressReporter.ReportAsync(
            new ImportProgress(job.Id, job.ProcessedCount, job.CreatedCount, job.ErrorCount,
                job.DuplicateCount, job.Status),
            cancellationToken);

    private static ImportLogEntry LogEntry(
        Guid jobId, int fila, string tipoDocumento, string documento, LogTipo tipo, string motivo) => new()
    {
        JobId = jobId,
        Fila = fila,
        TipoDocumento = tipoDocumento,
        Documento = documento,
        Tipo = tipo,
        Motivo = motivo,
        Fecha = DateTimeOffset.UtcNow
    };

    private static User ToUser(ValidatedUser user) => new()
    {
        TipoDocumento = user.TipoDocumento,
        Documento = user.Documento,
        Nombre = user.Nombre,
        Email = user.Email,
        Telefono = user.Telefono,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
