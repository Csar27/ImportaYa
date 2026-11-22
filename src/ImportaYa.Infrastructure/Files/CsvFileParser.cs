using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;

namespace ImportaYa.Infrastructure.Files;

public sealed class CsvFileParser : IFileParser
{
    public ImportFormat Format => ImportFormat.Csv;

    public async IAsyncEnumerable<RawUserRow> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false),
            detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            DetectDelimiter = true,
            DetectDelimiterValues = [",", ";"],
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, configuration);

        if (!await csv.ReadAsync())
        {
            throw new InvalidDataException("El archivo CSV está vacío.");
        }

        // Con DetectDelimiter, HeaderRecord no se puebla: se leen los campos del registro actual.
        var header = new string[ImportHeaders.Columns.Length];
        for (var i = 0; i < header.Length; i++)
        {
            header[i] = csv.GetField(i) ?? string.Empty;
        }

        HeaderValidator.Validate(header, "CSV");

        var fallbackRow = 1;

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            fallbackRow++;

            var values = new string[ImportHeaders.Columns.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = csv.GetField(i) ?? string.Empty;
            }

            if (values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            // RawRow respeta filas en blanco y saltos de línea dentro de comillas.
            var rowNumber = (int)(csv.Context.Parser?.RawRow ?? fallbackRow);

            yield return new RawUserRow(rowNumber, values[0], values[1], values[2], values[3], values[4]);
        }
    }
}
