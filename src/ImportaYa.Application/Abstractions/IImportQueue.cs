namespace ImportaYa.Application.Abstractions;

public interface IImportQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);
}
