using System.Threading.Channels;
using ImportaYa.Application.Abstractions;

namespace ImportaYa.Infrastructure.Background;

/// <summary>Cola en memoria de trabajos pendientes. Un único lector (el worker).</summary>
public sealed class ChannelImportQueue : IImportQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(jobId, cancellationToken);
}
