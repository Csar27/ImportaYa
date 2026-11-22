using FluentAssertions;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Application.Options;
using ImportaYa.Application.Services;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace ImportaYa.Application.Tests;

public sealed class ImportServiceTests
{
    private static ImportService CreateService(
        IFileParserFactory factory,
        ImportOptions? options = null,
        IFileStorage? storage = null,
        IImportJobRepository? jobs = null,
        IImportQueue? queue = null,
        IUnitOfWork? unitOfWork = null) =>
        new(
            Microsoft.Extensions.Options.Options.Create(options ?? new ImportOptions()),
            factory,
            storage ?? Substitute.For<IFileStorage>(),
            jobs ?? Substitute.For<IImportJobRepository>(),
            queue ?? Substitute.For<IImportQueue>(),
            unitOfWork ?? Substitute.For<IUnitOfWork>());

    [Fact]
    public async Task Unsupported_extension_is_rejected_before_saving()
    {
        var factory = Substitute.For<IFileParserFactory>();
        factory.GetFor(".txt").Returns(_ => throw new FormatException("Extensión no admitida '.txt'."));
        var storage = Substitute.For<IFileStorage>();
        var service = CreateService(factory, storage: storage);

        var action = async () => await service.CreateAsync("usuarios.txt", 10, new MemoryStream());

        await action.Should().ThrowAsync<FormatException>().WithMessage("*txt*");
        await storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default);
    }

    [Fact]
    public async Task Oversized_file_is_rejected()
    {
        var parser = Substitute.For<IFileParser>();
        parser.Format.Returns(ImportFormat.Csv);
        var factory = Substitute.For<IFileParserFactory>();
        factory.GetFor(".csv").Returns(parser);
        var service = CreateService(factory, new ImportOptions { MaxFileSizeBytes = 100 });

        var action = async () => await service.CreateAsync("usuarios.csv", 101, new MemoryStream());

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tamaño máximo*");
    }

    [Fact]
    public async Task Valid_file_creates_a_queued_job_and_enqueues_it()
    {
        var parser = Substitute.For<IFileParser>();
        parser.Format.Returns(ImportFormat.Csv);
        var factory = Substitute.For<IFileParserFactory>();
        factory.GetFor(".csv").Returns(parser);

        var storage = Substitute.For<IFileStorage>();
        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("stored.csv");

        ImportJob? captured = null;
        var jobs = Substitute.For<IImportJobRepository>();
        await jobs.AddAsync(Arg.Do<ImportJob>(j => captured = j), Arg.Any<CancellationToken>());

        var queue = Substitute.For<IImportQueue>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var service = CreateService(factory, storage: storage, jobs: jobs, queue: queue, unitOfWork: unitOfWork);

        var result = await service.CreateAsync("usuarios.csv", 10, new MemoryStream());

        result.Status.Should().Be("Queued");
        captured.Should().NotBeNull();
        captured!.Format.Should().Be(ImportFormat.Csv);
        captured.FileName.Should().Be("usuarios.csv");
        captured.StoredFilePath.Should().Be("stored.csv");
        await queue.Received(1).EnqueueAsync(result.JobId, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
