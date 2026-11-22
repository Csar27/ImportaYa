using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Rules;

namespace ImportaYa.Application.Validators;

public sealed class UserRowValidator : IUserRowValidator
{
    public RowValidationResult Validate(RawUserRow row)
    {
        if (!Enum.TryParse<TipoDocumento>(row.TipoDocumento?.Trim(), ignoreCase: true, out var tipo))
        {
            return RowValidationResult.Fail($"Tipo de documento inválido: '{row.TipoDocumento}'.");
        }

        var documento = row.Documento?.Trim() ?? string.Empty;
        if (documento.Length == 0)
        {
            return RowValidationResult.Fail("El documento es obligatorio.");
        }

        if (!DocumentFormatRules.IsValid(tipo, documento))
        {
            return RowValidationResult.Fail($"El documento '{documento}' no tiene un formato válido para {tipo}.");
        }

        var nombre = row.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length == 0)
        {
            return RowValidationResult.Fail("El nombre es obligatorio.");
        }

        var email = row.Email?.Trim() ?? string.Empty;
        if (!EmailRules.IsValid(email))
        {
            return RowValidationResult.Fail($"El email '{email}' no tiene un formato válido.");
        }

        var telefono = string.IsNullOrWhiteSpace(row.Telefono) ? null : row.Telefono.Trim();

        return RowValidationResult.Ok(new ValidatedUser(tipo, documento, nombre, email, telefono));
    }
}
