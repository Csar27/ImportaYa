using System.Runtime.CompilerServices;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Application.Options;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace ImportaYa.Application.Tests;

internal sealed class FakeParser(ImportFormat format, IReadOnlyList<RawUserRow> rows) : IFileParser
{
    public ImportFormat Format => format;

    public async IAsyncEnumerable<RawUserRow> ParseAsync(
        Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
            await Task.Yield();
        }
    }
}

internal sealed class FakeParserFactory(IFileParser parser) : IFileParserFactory
{
    public IFileParser GetFor(string extension) => parser;
    public ImportFormat GetFormat(string extension) => parser.Format;
}

internal sealed class FakeFileStorage : IFileStorage
{
    public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default) =>
        Task.FromResult("fake.csv");

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream());
}

internal sealed class FakeJobRepository(ImportJob job) : IImportJobRepository
{
    public ImportJob Job { get; private set; } = job;
    public Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ImportJob?>(Job);
    public Task<IReadOnlyList<ImportJob>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ImportJob>>([]);
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task AddAsync(ImportJob job, CancellationToken cancellationToken = default) { Job = job; return Task.CompletedTask; }
    public void Update(ImportJob job) { }
}

internal sealed class FakeLogRepository : IImportLogRepository
{
    public List<ImportLogEntry> Entries { get; } = [];

    public Task AddRangeAsync(IEnumerable<ImportLogEntry> entries, CancellationToken cancellationToken = default)
    {
        Entries.AddRange(entries);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<ImportLogEntry> Items, int Total)> QueryAsync(
        Guid jobId, LogTipo? tipo, int page, int pageSize, CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<ImportLogEntry>, int)>(([], 0));

    public async IAsyncEnumerable<ImportLogEntry> StreamAsync(
        Guid jobId, LogTipo? tipo, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var entry in Entries)
        {
            yield return entry;
            await Task.Yield();
        }
    }
}

internal sealed class FakeUserBulkWriter(HashSet<string> existingKeys) : IUserBulkWriter
{
    public List<List<User>> Batches { get; } = [];

    public Task<HashSet<string>> FindExistingKeysAsync(
        IEnumerable<DocumentKey> keys, CancellationToken cancellationToken = default) =>
        Task.FromResult(keys.Where(key => existingKeys.Contains(key.Value))
            .Select(key => key.Value)
            .ToHashSet(StringComparer.Ordinal));

    public Task WriteAsync(IEnumerable<User> users, CancellationToken cancellationToken = default)
    {
        Batches.Add(users.ToList());
        return Task.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}

internal sealed class FakeProgressReporter : IImportProgressReporter
{
    public List<ImportProgress> Reports { get; } = [];
    public Task ReportAsync(ImportProgress progress, CancellationToken cancellationToken = default)
    {
        Reports.Add(progress);
        return Task.CompletedTask;
    }
}

internal static class ProcessorTestHarness
{
    public static (ImportaYa.Application.Services.ImportProcessor Processor, ImportJob Job,
        FakeLogRepository Logs, FakeUserBulkWriter Writer, FakeProgressReporter Reporter)
        Create(
            IReadOnlyList<RawUserRow> rows,
            HashSet<string>? existingKeys = null,
            int batchSize = 200,
            int maxRows = 50_000)
    {
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            FileName = "usuarios.csv",
            StoredFilePath = "fake.csv",
            Format = ImportFormat.Csv,
            Status = ImportStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var parser = new FakeParser(ImportFormat.Csv, rows);
        var logs = new FakeLogRepository();
        var writer = new FakeUserBulkWriter(existingKeys ?? []);
        var reporter = new FakeProgressReporter();

        var processor = new ImportaYa.Application.Services.ImportProcessor(
            Microsoft.Extensions.Options.Options.Create(new ImportOptions { BatchSize = batchSize, MaxRows = maxRows }),
            new FakeParserFactory(parser),
            new FakeFileStorage(),
            new FakeJobRepository(job),
            logs,
            writer,
            new ImportaYa.Application.Validators.UserRowValidator(),
            reporter,
            new FakeUnitOfWork(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ImportaYa.Application.Services.ImportProcessor>.Instance);

        return (processor, job, logs, writer, reporter);
    }

    public static RawUserRow Row(int fila, string tipo, string documento, string? email = null) =>
        new(fila, tipo, documento, "Ana Pérez", email ?? "ana@demo.com", "3001234567");
}
