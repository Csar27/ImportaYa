namespace ImportaYa.Application.Dtos;

public sealed record UserDto(
    long Id,
    string TipoDocumento,
    string Documento,
    string Nombre,
    string Email,
    string? Telefono,
    DateTimeOffset CreatedAt);
