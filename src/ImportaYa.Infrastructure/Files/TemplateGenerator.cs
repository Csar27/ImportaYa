using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;

namespace ImportaYa.Infrastructure.Files;

public sealed class TemplateGenerator : ITemplateGenerator
{
    public async Task GenerateAsync(
        ImportFormat format, Stream destination, CancellationToken cancellationToken = default)
    {
        switch (format)
        {
            case ImportFormat.Csv:
                await GenerateCsvAsync(destination, cancellationToken);
                break;
            case ImportFormat.Xlsx:
                GenerateXlsx(destination, cancellationToken);
                break;
            default:
                throw new FormatException("Formato de plantilla no admitido.");
        }
    }

    private static async Task GenerateCsvAsync(Stream destination, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(destination, new UTF8Encoding(false), leaveOpen: true);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var column in ImportHeaders.Columns)
        {
            csv.WriteField(column);
        }

        await csv.NextRecordAsync();
        await csv.FlushAsync();
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void GenerateXlsx(Stream destination, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Usuarios");

        for (var i = 0; i < ImportHeaders.Columns.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = ImportHeaders.Columns[i];
        }

        // Evita que Excel convierta documentos con ceros a la izquierda en números.
        sheet.Column(2).Style.NumberFormat.Format = "@";
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        cancellationToken.ThrowIfCancellationRequested();
        workbook.SaveAs(destination);
    }
}
