using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;
using ImportaYa.Infrastructure.Files;
using Xunit;

namespace ImportaYa.Infrastructure.Tests;

public sealed class ParserTests
{
    private const string Header = "tipoDocumento,documento,nombre,email,telefono";

    [Fact]
    public async Task Csv_detects_comma_and_keeps_physical_row_numbers()
    {
        var content = $"""
                       {Header}
                       CC,1045678901,Ana Pérez,ana@demo.com,3001234567
                       CE,2045678901,Luis Gómez,luis@demo.com,3009876543
                       """;

        var rows = await Collect(new CsvFileParser(), StreamOf(content));

        rows.Should().HaveCount(2);
        rows[0].RowNumber.Should().Be(2);
        rows[0].Documento.Should().Be("1045678901");
        rows[0].Nombre.Should().Be("Ana Pérez");
        rows[1].RowNumber.Should().Be(3);
    }

    [Fact]
    public async Task Csv_detects_semicolon()
    {
        var content = $"""
                       {Header.Replace(',', ';')}
                       CC;1045678901;Ana;ana@demo.com;3001234567
                       """;

        var rows = await Collect(new CsvFileParser(), StreamOf(content));

        rows.Should().ContainSingle();
        rows[0].Documento.Should().Be("1045678901");
        rows[0].Telefono.Should().Be("3001234567");
    }

    [Fact]
    public async Task Csv_ignores_blank_rows_without_renumbering()
    {
        var content = $"""
                       {Header}
                       CC,1045678901,Ana,ana@demo.com,3001234567

                       CE,2045678901,Luis,luis@demo.com,3009876543
                       """;

        var rows = await Collect(new CsvFileParser(), StreamOf(content));

        rows.Select(r => r.RowNumber).Should().Equal(2, 4);
    }

    [Fact]
    public async Task Csv_rejects_header_with_missing_column()
    {
        var content = "tipoDocumento,documento,nombre,telefono\nCC,1045678901,Ana,3001234567";

        var action = async () => await Collect(new CsvFileParser(), StreamOf(content));

        (await action.Should().ThrowAsync<InvalidDataException>())
            .WithMessage("*email*");
    }

    [Fact]
    public async Task Csv_accepts_utf8_bom()
    {
        var content = "\uFEFF" + Header + "\nCC,1045678901,Ana Pérez,ana@demo.com,3001234567";
        var bytes = new UTF8Encoding(true).GetBytes(content);

        var rows = await Collect(new CsvFileParser(), new MemoryStream(bytes));

        rows.Should().ContainSingle();
        rows[0].TipoDocumento.Should().Be("CC");
        rows[0].Nombre.Should().Be("Ana Pérez");
    }

    [Fact]
    public async Task Excel_reads_text_cells_with_leading_zeros_and_large_numbers()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Usuarios");
        WriteHeader(sheet);

        sheet.Cell(2, 1).Value = "CC";
        sheet.Cell(2, 2).SetValue("0001234567");
        sheet.Cell(2, 3).Value = "Ana";
        sheet.Cell(2, 4).Value = "ana@demo.com";
        sheet.Cell(2, 5).SetValue("3001234567");

        sheet.Cell(3, 1).Value = "TI";
        sheet.Cell(3, 2).Value = 1045678901d;
        sheet.Cell(3, 3).Value = "Luis";
        sheet.Cell(3, 4).Value = "luis@demo.com";

        var rows = await Collect(new ExcelFileParser(), StreamOf(workbook));

        rows.Should().HaveCount(2);
        rows[0].RowNumber.Should().Be(2);
        rows[0].Documento.Should().Be("0001234567");
        rows[1].RowNumber.Should().Be(3);
        rows[1].Documento.Should().Be("1045678901");
    }

    [Fact]
    public async Task Excel_ignores_blank_rows_without_renumbering()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Usuarios");
        WriteHeader(sheet);

        sheet.Cell(2, 1).Value = "CC";
        sheet.Cell(2, 2).Value = "1045678901";
        sheet.Cell(4, 1).Value = "CE";
        sheet.Cell(4, 2).Value = "2045678901";

        var rows = await Collect(new ExcelFileParser(), StreamOf(workbook));

        rows.Select(r => r.RowNumber).Should().Equal(2, 4);
    }

    [Fact]
    public async Task Excel_rejects_header_with_missing_column()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Usuarios");
        sheet.Cell(1, 1).Value = "tipoDocumento";
        sheet.Cell(1, 2).Value = "documento";
        sheet.Cell(1, 3).Value = "nombre";
        sheet.Cell(1, 4).Value = "telefono";

        var action = async () => await Collect(new ExcelFileParser(), StreamOf(workbook));

        (await action.Should().ThrowAsync<InvalidDataException>()).WithMessage("*email*");
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".xls")]
    [InlineData("")]
    public void Factory_rejects_unsupported_extensions(string extension)
    {
        var factory = new FileParserFactory([new CsvFileParser(), new ExcelFileParser()]);

        var action = () => factory.GetFor(extension);

        action.Should().Throw<FormatException>().WithMessage("*Extensión no admitida*");
    }

    [Theory]
    [InlineData(".csv", ImportFormat.Csv)]
    [InlineData(".CSV", ImportFormat.Csv)]
    [InlineData(".xlsx", ImportFormat.Xlsx)]
    public void Factory_selects_parser_by_extension(string extension, ImportFormat expected)
    {
        var factory = new FileParserFactory([new CsvFileParser(), new ExcelFileParser()]);

        factory.GetFor(extension).Format.Should().Be(expected);
        factory.GetFormat(extension).Should().Be(expected);
    }

    private static void WriteHeader(IXLWorksheet sheet)
    {
        for (var i = 0; i < ImportHeaders.Columns.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = ImportHeaders.Columns[i];
        }
    }

    private static MemoryStream StreamOf(string content) =>
        new(Encoding.UTF8.GetBytes(content));

    private static MemoryStream StreamOf(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static async Task<List<RawUserRow>> Collect(IFileParser parser, Stream stream)
    {
        var result = new List<RawUserRow>();
        await foreach (var row in parser.ParseAsync(stream))
        {
            result.Add(row);
        }
        return result;
    }
}
