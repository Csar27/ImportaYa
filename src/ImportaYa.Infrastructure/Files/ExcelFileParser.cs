using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ExcelDataReader;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;

namespace ImportaYa.Infrastructure.Files;

public sealed class ExcelFileParser : IFileParser
{
    static ExcelFileParser()
    {
        // ExcelDataReader necesita las páginas de códigos clásicas aunque solo se lean .xlsx.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ImportFormat Format => ImportFormat.Xlsx;

    public async IAsyncEnumerable<RawUserRow> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = ExcelReaderFactory.CreateReader(stream);

        if (!reader.Read())
        {
            throw new InvalidDataException("La hoja XLSX está vacía.");
        }

        var rowNumber = 1;
        var header = new string[ImportHeaders.Columns.Length];
        for (var i = 0; i < header.Length; i++)
        {
            header[i] = i < reader.FieldCount ? ReadCellAsString(reader, i) : string.Empty;
        }

        HeaderValidator.Validate(header, "XLSX");

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            var values = new string[ImportHeaders.Columns.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = i < reader.FieldCount ? ReadCellAsString(reader, i) : string.Empty;
            }

            if (values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            yield return new RawUserRow(rowNumber, values[0], values[1], values[2], values[3], values[4]);
        }
    }

    /// <summary>
    /// Toda celda se lee como texto. Los numéricos se formatean sin notación científica
    /// y los textos (ceros a la izquierda) se conservan tal cual.
    /// </summary>
    private static string ReadCellAsString(IExcelDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return string.Empty;
        }

        var value = reader.GetValue(index);

        return value switch
        {
            string text => text.Trim(),
            double number => number.ToString("0", CultureInfo.InvariantCulture),
            float number => number.ToString("0", CultureInfo.InvariantCulture),
            decimal number => number.ToString("0", CultureInfo.InvariantCulture),
            long number => number.ToString(CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            bool flag => flag.ToString(),
            DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
        };
    }
}
