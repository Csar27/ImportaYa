namespace ImportaYa.Application.Models;

public sealed record RowValidationResult(string? Error, ValidatedUser? User)
{
    public bool IsValid => Error is null;

    public static RowValidationResult Ok(ValidatedUser user) => new(null, user);

    public static RowValidationResult Fail(string error) => new(error, null);
}
