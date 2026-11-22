using System.Data;
using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Models;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ImportaYa.Infrastructure.Persistence;

/// <summary>
/// Consulta existentes con un TVP (una sola ida a BD por lote, usando el índice único)
/// e inserta válidos con SqlBulkCopy, fuera del change tracker de EF Core.
/// </summary>
public sealed class UserBulkWriter(IConfiguration configuration) : IUserBulkWriter
{
    private const string DocumentKeyTypeName = "dbo.DocumentKeyList";

    private string ConnectionString =>
        configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");

    public async Task<HashSet<string>> FindExistingKeysAsync(
        IEnumerable<DocumentKey> keys, CancellationToken cancellationToken = default)
    {
        var keyList = keys.Distinct().ToList();
        var result = new HashSet<string>(StringComparer.Ordinal);

        if (keyList.Count == 0)
        {
            return result;
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.TipoDocumento, u.Documento
            FROM dbo.Users AS u
            INNER JOIN @keys AS k
                ON u.TipoDocumento = k.TipoDocumento AND u.Documento = k.Documento
            """;

        var table = new DataTable();
        table.Columns.Add("TipoDocumento", typeof(string));
        table.Columns.Add("Documento", typeof(string));

        foreach (var key in keyList)
        {
            table.Rows.Add(key.TipoDocumento.ToString(), key.Documento);
        }

        command.Parameters.Add(new SqlParameter("@keys", SqlDbType.Structured)
        {
            TypeName = DocumentKeyTypeName,
            Value = table
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var tipo = Enum.Parse<TipoDocumento>(reader.GetString(0), ignoreCase: true);
            result.Add(new DocumentKey(tipo, reader.GetString(1)).Value);
        }

        return result;
    }

    public async Task WriteAsync(IEnumerable<User> users, CancellationToken cancellationToken = default)
    {
        var list = users.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var table = new DataTable();
        table.Columns.Add("TipoDocumento", typeof(string));
        table.Columns.Add("Documento", typeof(string));
        table.Columns.Add("Nombre", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("Telefono", typeof(string));
        table.Columns.Add("CreatedAt", typeof(DateTimeOffset));

        foreach (var user in list)
        {
            table.Rows.Add(
                user.TipoDocumento.ToString(),
                user.Documento,
                user.Nombre,
                user.Email,
                user.Telefono,
                user.CreatedAt);
        }

        using var bulk = new SqlBulkCopy(connection)
        {
            DestinationTableName = "dbo.Users",
            BatchSize = list.Count,
            BulkCopyTimeout = 60
        };

        foreach (DataColumn column in table.Columns)
        {
            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulk.WriteToServerAsync(table, cancellationToken);
    }
}
