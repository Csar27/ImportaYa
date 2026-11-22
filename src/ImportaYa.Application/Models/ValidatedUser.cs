using ImportaYa.Domain.Enums;

namespace ImportaYa.Application.Models;

public sealed record ValidatedUser(
    TipoDocumento TipoDocumento,
    string Documento,
    string Nombre,
    string Email,
    string? Telefono);
