using ImportaYa.Domain.Enums;

namespace ImportaYa.Application.Abstractions;

public interface ITemplateGenerator
{
    Task GenerateAsync(ImportFormat format, Stream destination, CancellationToken cancellationToken = default);
}
