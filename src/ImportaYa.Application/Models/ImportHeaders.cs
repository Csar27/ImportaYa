namespace ImportaYa.Application.Models;

/// <summary>Contrato de columnas compartido por parsers, plantillas y exportación.</summary>
public static class ImportHeaders
{
    public static readonly string[] Columns = ["tipoDocumento", "documento", "nombre", "email", "telefono"];
}
