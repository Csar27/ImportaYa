using ImportaYa.Domain.Enums;

namespace ImportaYa.Domain.Rules;

/// <summary>Reglas puras de formato del documento según su tipo.</summary>
public static class DocumentFormatRules
{
    public static bool IsValid(TipoDocumento tipo, string documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return false;
        }

        var value = documento.Trim();

        return tipo switch
        {
            TipoDocumento.CC or TipoDocumento.CE or TipoDocumento.TI => value.All(char.IsDigit),
            TipoDocumento.PAS => value.All(char.IsLetterOrDigit),
            TipoDocumento.NIT => IsValidNit(value),
            _ => false
        };
    }

    private static bool IsValidNit(string value)
    {
        var parts = value.Split('-');

        if (parts.Length > 2)
        {
            return false;
        }

        if (parts.Length == 2 && (parts[1].Length != 1 || !char.IsDigit(parts[1][0])))
        {
            return false;
        }

        return parts[0].Length > 0 && parts[0].All(char.IsDigit);
    }
}
