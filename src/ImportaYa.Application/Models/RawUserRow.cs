namespace ImportaYa.Application.Models;

/// <summary>Fila neutra emitida por cualquier parser. RowNumber es la fila física del archivo (encabezado = 1).</summary>
public sealed record RawUserRow(
    int RowNumber,
    string TipoDocumento,
    string Documento,
    string Nombre,
    string Email,
    string Telefono);
