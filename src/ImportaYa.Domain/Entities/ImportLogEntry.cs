using ImportaYa.Domain.Enums;

namespace ImportaYa.Domain.Entities;

/// <summary>Fila rechazada de una carga. Guardamos el tipo de documento como texto para conservar valores inválidos.</summary>
public sealed class ImportLogEntry
{
    public long Id { get; set; }
    public Guid JobId { get; set; }
    public int Fila { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public LogTipo Tipo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTimeOffset Fecha { get; set; }
}
