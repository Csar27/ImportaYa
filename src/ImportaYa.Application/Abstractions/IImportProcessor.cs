namespace ImportaYa.Application.Abstractions;

public interface IImportProcessor
{
    Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default);
}
