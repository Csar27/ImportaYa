using ImportaYa.Application.Abstractions;
using ImportaYa.Domain.Enums;

namespace ImportaYa.Infrastructure.Files;

public sealed class FileParserFactory(IEnumerable<IFileParser> parsers) : IFileParserFactory
{
    private static readonly Dictionary<string, ImportFormat> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        [".csv"] = ImportFormat.Csv,
        [".xlsx"] = ImportFormat.Xlsx
    };

    public ImportFormat GetFormat(string extension) => GetFormatOrThrow(extension);

    public IFileParser GetFor(string extension)
    {
        var format = GetFormatOrThrow(extension);
        return parsers.First(parser => parser.Format == format);
    }

    private static ImportFormat GetFormatOrThrow(string extension)
    {
        if (!SupportedFormats.TryGetValue(extension ?? string.Empty, out var format))
        {
            throw new FormatException(
                $"Extensión no admitida '{extension}'. Solo se permiten archivos .csv y .xlsx.");
        }

        return format;
    }
}
