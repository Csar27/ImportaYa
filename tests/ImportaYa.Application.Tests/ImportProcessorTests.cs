using FluentAssertions;
using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;
using Xunit;

namespace ImportaYa.Application.Tests;

public sealed class ImportProcessorTests
{
    [Fact]
    public async Task Duplicate_within_file_is_logged_with_first_row_number()
    {
        var (processor, job, logs, writer, _) = ProcessorTestHarness.Create([
            ProcessorTestHarness.Row(2, "CC", "1045678901"),
            ProcessorTestHarness.Row(3, "CC", "1045678901")
        ]);

        await processor.ProcessAsync(job.Id);

        job.Status.Should().Be(ImportStatus.Completed);
        job.CreatedCount.Should().Be(1);
        job.DuplicateCount.Should().Be(1);
        writer.Batches.Should().HaveCount(1);
        writer.Batches[0].Should().ContainSingle();

        var duplicate = logs.Entries.Should().ContainSingle().Subject;
        duplicate.Tipo.Should().Be(LogTipo.Duplicado);
        duplicate.Fila.Should().Be(3);
        duplicate.Motivo.Should().Be("Repetido en el archivo, fila 2");
    }

    [Fact]
    public async Task Duplicate_against_database_is_logged_and_not_inserted()
    {
        var existing = new HashSet<string>(StringComparer.Ordinal) { "CC|1045678901" };
        var (processor, job, logs, writer, _) = ProcessorTestHarness.Create(
            [ProcessorTestHarness.Row(2, "CC", "1045678901")], existing);

        await processor.ProcessAsync(job.Id);

        job.CreatedCount.Should().Be(0);
        job.DuplicateCount.Should().Be(1);
        writer.Batches.Should().BeEmpty();

        var duplicate = logs.Entries.Should().ContainSingle().Subject;
        duplicate.Tipo.Should().Be(LogTipo.Duplicado);
        duplicate.Fila.Should().Be(2);
        duplicate.Motivo.Should().Be("Ya existe en la base de datos");
    }

    [Fact]
    public async Task Invalid_document_type_is_logged_as_error()
    {
        var (processor, job, logs, writer, _) = ProcessorTestHarness.Create([
            ProcessorTestHarness.Row(2, "XX", "1045678901")
        ]);

        await processor.ProcessAsync(job.Id);

        job.ErrorCount.Should().Be(1);
        writer.Batches.Should().BeEmpty();
        logs.Entries.Should().ContainSingle(e =>
            e.Tipo == LogTipo.Error && e.Motivo.Contains("Tipo de documento inválido"));
    }

    [Fact]
    public async Task Invalid_document_format_is_logged_as_error()
    {
        var (processor, job, logs, _, _) = ProcessorTestHarness.Create([
            ProcessorTestHarness.Row(2, "CC", "ABC123")
        ]);

        await processor.ProcessAsync(job.Id);

        job.ErrorCount.Should().Be(1);
        logs.Entries.Should().ContainSingle(e =>
            e.Tipo == LogTipo.Error && e.Motivo.Contains("formato válido"));
    }

    [Fact]
    public async Task Invalid_email_is_logged_as_error()
    {
        var (processor, job, logs, _, _) = ProcessorTestHarness.Create([
            ProcessorTestHarness.Row(7, "CC", "1045678901", "correo-mal")
        ]);

        await processor.ProcessAsync(job.Id);

        job.ErrorCount.Should().Be(1);
        logs.Entries.Should().ContainSingle(e => e.Fila == 7 && e.Tipo == LogTipo.Error);
    }

    [Fact]
    public async Task A_bad_row_does_not_stop_the_rest_of_the_import()
    {
        var (processor, job, logs, writer, _) = ProcessorTestHarness.Create([
            ProcessorTestHarness.Row(2, "CC", "1045678901"),
            ProcessorTestHarness.Row(3, "CC", "ABC123"),
            ProcessorTestHarness.Row(4, "CE", "2045678901"),
            ProcessorTestHarness.Row(5, "TI", "3045678901")
        ]);

        await processor.ProcessAsync(job.Id);

        job.Status.Should().Be(ImportStatus.Completed);
        job.ProcessedCount.Should().Be(4);
        job.CreatedCount.Should().Be(3);
        job.ErrorCount.Should().Be(1);
        writer.Batches.SelectMany(b => b).Should().HaveCount(3);
        logs.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Rows_are_flushed_in_batches()
    {
        var rows = Enumerable.Range(2, 5)
            .Select(i => ProcessorTestHarness.Row(i, "CC", (1_000_000_000 + i).ToString()))
            .ToList();

        var (processor, job, _, writer, _) = ProcessorTestHarness.Create(rows, batchSize: 2);

        await processor.ProcessAsync(job.Id);

        writer.Batches.Should().HaveCount(3);
        writer.Batches.SelectMany(b => b).Should().HaveCount(5);
        job.CreatedCount.Should().Be(5);
    }

    [Fact]
    public async Task Row_limit_fails_the_job()
    {
        var (processor, job, _, _, _) = ProcessorTestHarness.Create([
            ProcessorTestHarness.Row(2, "CC", "1045678901"),
            ProcessorTestHarness.Row(3, "CE", "2045678901")
        ], maxRows: 1);

        await processor.ProcessAsync(job.Id);

        job.Status.Should().Be(ImportStatus.Failed);
        job.ErrorMessage.Should().Contain("límite de 1 filas");
    }
}
