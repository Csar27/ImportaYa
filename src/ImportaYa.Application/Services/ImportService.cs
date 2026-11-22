using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Dtos;
using ImportaYa.Application.Options;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace ImportaYa.Application.Services;

/// <summary>Crea el trabajo, persiste el archivo y lo encola. No procesa nada en la petición HTTP.</summary>
public sealed class ImportService(
    IOptions<ImportOptions> options,
    IFileParserFactory parserFactory,
    IFileStorage fileStorage,
    IImportJobRepository jobs,
    IImportQueue queue,
    IUnitOfWork unitOfWork)
{
    public async Task<CreateImportResult> CreateAsync(
        string fileName,
        long contentLength,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var parser = parserFactory.GetFor(extension);

        if (contentLength > options.Value.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"El archivo supera el tamaño máximo de {options.Value.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var storedPath = await fileStorage.SaveAsync(content, fileName, cancellationToken);

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            FileName = Path.GetFileName(fileName),
            StoredFilePath = storedPath,
            Format = parser.Format,
            Status = ImportStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await jobs.AddAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await queue.EnqueueAsync(job.Id, cancellationToken);

        return new CreateImportResult(job.Id, job.Status.ToString());
    }
}
