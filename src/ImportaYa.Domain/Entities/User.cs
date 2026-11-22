using ImportaYa.Domain.Enums;

namespace ImportaYa.Domain.Entities;

/// <summary>Usuario válido persistido por el worker. Clave de negocio: (TipoDocumento, Documento).</summary>
public sealed class User
{
    public long Id { get; set; }
    public TipoDocumento TipoDocumento { get; set; }
    public string Documento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
