using ImportaYa.Domain.Enums;

namespace ImportaYa.Application.Models;

/// <summary>Clave de negocio compuesta. El string canónico se usa en HashSet/TVP.</summary>
public readonly record struct DocumentKey(TipoDocumento TipoDocumento, string Documento)
{
    public string Value => $"{TipoDocumento}|{Documento}";

    public static DocumentKey From(ValidatedUser user) => new(user.TipoDocumento, user.Documento);
}
