using FluentAssertions;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using Xunit;

namespace ImportaYa.Domain.Tests;

public sealed class ImportJobTests
{
    [Fact]
    public void Lifecycle_marks_start_and_end_timestamps()
    {
        var job = new ImportJob { Id = Guid.NewGuid(), Status = ImportStatus.Queued };

        job.MarkProcessing();
        job.Status.Should().Be(ImportStatus.Processing);
        job.StartedAt.Should().NotBeNull();

        job.MarkCompleted();
        job.Status.Should().Be(ImportStatus.Completed);
        job.CompletedAt.Should().NotBeNull();
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Mark_failed_truncates_long_messages()
    {
        var job = new ImportJob();
        var message = new string('x', 5000);

        job.MarkFailed(message);

        job.Status.Should().Be(ImportStatus.Failed);
        job.ErrorMessage.Should().HaveLength(2000);
    }
}
