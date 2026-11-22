using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;

namespace ImportaYa.Application.Abstractions;

/// <summary>Contrato de lectura streaming. El resto del sistema ignora el formato de origen.</summary>
public interface IFileParser
{
    ImportFormat Format { get; }

    IAsyncEnumerable<RawUserRow> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
}
