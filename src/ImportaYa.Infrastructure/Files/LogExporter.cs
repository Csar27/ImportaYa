using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ImportaYa.Application.Abstractions;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Repositories;

namespace ImportaYa.Infrastructure.Files;

public sealed class LogExporter(IImportLogRepository logs) : ILogExporter
{
    private static readonly string[] Columns = ["Fila", "TipoDocumento", "Documento", "Tipo", "Motivo", "Fecha"];

    public async Task ExportAsync(
        Guid jobId,
        LogTipo? tipo,
        ImportFormat format,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        switch (format)
        {
            case ImportFormat.Csv:
                await ExportCsvAsync(jobId, tipo, destination, cancellationToken);
                break;
            case ImportFormat.Xlsx:
                await ExportXlsxAsync(jobId, tipo, destination, cancellationToken);
                break;
            default:
                throw new FormatException("Formato de exportación no admitido.");
        }
    }

    private async Task ExportCsvAsync(
        Guid jobId, LogTipo? tipo, Stream destination, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(destination, new UTF8Encoding(false), leaveOpen: true);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var column in Columns)
        {
            csv.WriteField(column);
        }
        await csv.NextRecordAsync();

        await foreach (var entry in logs.StreamAsync(jobId, tipo, cancellationToken))
        {
            csv.WriteField(entry.Fila);
            csv.WriteField(entry.TipoDocumento);
            csv.WriteField(entry.Documento);
            csv.WriteField(entry.Tipo.ToString());
            csv.WriteField(entry.Motivo);
            csv.WriteField(entry.Fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            await csv.NextRecordAsync();
        }

        await csv.FlushAsync();
    }

    private async Task ExportXlsxAsync(
        Guid jobId, LogTipo? tipo, Stream destination, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Log de carga");

        for (var i = 0; i < Columns.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = Columns[i];
        }
        sheet.Row(1).Style.Font.Bold = true;

        var rowIndex = 2;
        await foreach (var entry in logs.StreamAsync(jobId, tipo, cancellationToken))
        {
            sheet.Cell(rowIndex, 1).Value = entry.Fila;
            sheet.Cell(rowIndex, 2).Value = entry.TipoDocumento;
            sheet.Cell(rowIndex, 3).Value = entry.Documento;
            sheet.Cell(rowIndex, 4).Value = entry.Tipo.ToString();
            sheet.Cell(rowIndex, 5).Value = entry.Motivo;
            sheet.Cell(rowIndex, 6).Value = entry.Fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(destination);
    }
}
