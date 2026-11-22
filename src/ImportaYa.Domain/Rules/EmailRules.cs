using System.Text.RegularExpressions;

namespace ImportaYa.Domain.Rules;

public static partial class EmailRules
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    public static bool IsValid(string? email) =>
        !string.IsNullOrWhiteSpace(email) && EmailPattern().IsMatch(email.Trim());
}
