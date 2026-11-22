using ImportaYa.Application.Models;
using ImportaYa.Domain.Entities;

namespace ImportaYa.Application.Abstractions;

/// <summary>Acceso por lotes a Users: consulta de existentes (TVP) e inserción con SqlBulkCopy.</summary>
public interface IUserBulkWriter
{
    Task<HashSet<string>> FindExistingKeysAsync(IEnumerable<DocumentKey> keys, CancellationToken cancellationToken = default);

    Task WriteAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);
}
