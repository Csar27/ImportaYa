using ImportaYa.Application.Models;

namespace ImportaYa.Infrastructure.Files;

/// <summary>Valida encabezados en el orden exacto del contrato, tolerando BOM, espacios y mayúsculas.</summary>
internal static class HeaderValidator
{
    public static void Validate(IReadOnlyList<string> header, string format)
    {
        var found = header.Select(Normalize).ToArray();

        for (var i = 0; i < ImportHeaders.Columns.Length; i++)
        {
            var expected = ImportHeaders.Columns[i];
            if (i >= found.Length || found[i] != Normalize(expected))
            {
                throw new InvalidDataException(
                    $"Falta la columna '{expected}' en el encabezado ({format}). " +
                    $"Se encontraron: {string.Join(", ", header)}.");
            }
        }
    }

    private static string Normalize(string value) =>
        (value ?? string.Empty).Trim().Trim('\uFEFF').ToLowerInvariant();
}
