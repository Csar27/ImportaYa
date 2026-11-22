using ImportaYa.Application.Models;

namespace ImportaYa.Application.Abstractions;

public interface IUserRowValidator
{
    RowValidationResult Validate(RawUserRow row);
}
