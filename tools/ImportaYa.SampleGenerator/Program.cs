using System.Globalization;
using System.Text;
using ClosedXML.Excel;

// Genera dos archivos idénticos en contenido (1.200 registros, filas físicas 2..1201) con errores deliberados.
// i = índice de dato; la fila física del archivo es i + 1 porque la fila 1 es el encabezado.
// - tipos inválidos: registros 5, 57 y 300 (filas 6, 58 y 301)
// - documentos con formato inválido: registros 10, 88 y 512 (filas 11, 89 y 513)
// - emails mal escritos: registros 12, 91 y 700 (filas 13, 92 y 701)
// - duplicados dentro del archivo: registros 20, 33 y 900 (filas 21, 34 y 901)
// - ceros a la izquierda: registro 8 (fila 9, CC 0001234567)
// - los duplicados contra BD los siembra el test de integración en las filas 16 y 17
const int rowCount = 1200;

var outputDirectory = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "samples");
Directory.CreateDirectory(outputDirectory);

var rows = BuildRows(rowCount);

WriteCsv(Path.Combine(outputDirectory, "usuarios.csv"), rows);
WriteExcel(Path.Combine(outputDirectory, "usuarios.xlsx"), rows);

Console.WriteLine($"Generados {rows.Count} registros en {outputDirectory}");

static List<SampleRow> BuildRows(int count)
{
    var rows = new List<SampleRow>(count);
    var byIndex = new Dictionary<int, SampleRow>();

    for (var i = 1; i <= count; i++)
    {
        var tipo = Rotate(i);
        var documento = MakeDocument(i, tipo);

        var row = new SampleRow(tipo, documento, $"Usuario {i:D4}", $"usuario{i}@demo.com", $"300{i:D7}");

        // Ceros a la izquierda: se fuerza tipo CC para que la validación numérica siga pasando.
        if (i == 8)
        {
            row = row with { TipoDocumento = "CC", Documento = "0001234567" };
        }

        if (i is 5 or 57 or 300)
        {
            row = row with { TipoDocumento = "XX" };
        }

        if (i is 10 or 88 or 512)
        {
            row = row with { TipoDocumento = "CC", Documento = "ABC123" };
        }

        if (i is 12 or 91 or 700)
        {
            row = row with { Email = "correo-mal" };
        }

        var duplicateSource = i switch
        {
            20 => 18,
            33 => 21,
            900 => 22,
            _ => (int?)null
        };

        if (duplicateSource.HasValue && byIndex.TryGetValue(duplicateSource.Value, out var source))
        {
            row = row with { TipoDocumento = source.TipoDocumento, Documento = source.Documento };
        }

        rows.Add(row);
        byIndex[i] = row;
    }

    return rows;
}

static string Rotate(int index) => (index % 5) switch
{
    1 => "CC",
    2 => "CE",
    3 => "TI",
    4 => "PAS",
    _ => "NIT"
};

static string MakeDocument(int index, string tipo) => tipo switch
{
    "PAS" => $"PA{index:D8}",
    "NIT" => $"{800000000 + index}-7",
    _ => (1000000000 + index).ToString(CultureInfo.InvariantCulture)
};

static void WriteCsv(string path, IReadOnlyList<SampleRow> rows)
{
    using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
    writer.WriteLine("tipoDocumento,documento,nombre,email,telefono");

    foreach (var row in rows)
    {
        writer.WriteLine(string.Join(',',
            Escape(row.TipoDocumento),
            Escape(row.Documento),
            Escape(row.Nombre),
            Escape(row.Email),
            Escape(row.Telefono)));
    }
}

static string Escape(string value) =>
    value.Contains(',') || value.Contains('"')
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;

static void WriteExcel(string path, IReadOnlyList<SampleRow> rows)
{
    using var workbook = new XLWorkbook();
    var sheet = workbook.AddWorksheet("Usuarios");

    string[] headers = ["tipoDocumento", "documento", "nombre", "email", "telefono"];
    for (var i = 0; i < headers.Length; i++)
    {
        sheet.Cell(1, i + 1).Value = headers[i];
    }

    sheet.Row(1).Style.Font.Bold = true;
    sheet.Column(2).Style.NumberFormat.Format = "@";

    for (var i = 0; i < rows.Count; i++)
    {
        var row = rows[i];
        var excelRow = i + 2;

        sheet.Cell(excelRow, 1).Value = row.TipoDocumento;
        sheet.Cell(excelRow, 2).SetValue(row.Documento);
        sheet.Cell(excelRow, 3).Value = row.Nombre;
        sheet.Cell(excelRow, 4).Value = row.Email;
        sheet.Cell(excelRow, 5).SetValue(row.Telefono);
    }

    workbook.SaveAs(path);
}

internal sealed record SampleRow(
    string TipoDocumento,
    string Documento,
    string Nombre,
    string Email,
    string Telefono);
