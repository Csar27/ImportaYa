using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ImportaYa.Application.Dtos;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImportaYa.IntegrationTests;

/// <summary>
/// Usa SQL Server LocalDB (SQL Server real, necesario para SqlBulkCopy y el TVP).
/// Cada ejecución crea una base de datos aislada, sembra el administrador inicial y dos usuarios duplicados.
/// </summary>
public sealed class IntegrationFixture : IAsyncLifetime
{
    private string _databaseName = string.Empty;
    private string _storagePath = string.Empty;

    public const string AdminEmail = "admin@demo.com";
    public const string AdminPassword = "Admin12345";

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public string AdminToken { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // WebApplicationFactory inyecta su configuración después de que Program.cs valida la clave,
        // así que la clave JWT de pruebas se entrega por variable de entorno.
        Environment.SetEnvironmentVariable("Jwt__Key", "ClaveDePrueba-ImportaYa-0123456789-ABCDEFGHIJ");

        StartLocalDbIfNeeded();

        _databaseName = $"ImportaYa_Tests_{Guid.NewGuid():N}";
        _storagePath = Path.Combine(Path.GetTempPath(), "ImportaYaTests", Guid.NewGuid().ToString("N"));

        var connectionString =
            $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = connectionString,
                        ["ImportOptions:StoragePath"] = _storagePath,
                        ["Jwt:Key"] = "ClaveDePrueba-ImportaYa-0123456789-ABCDEFGHIJ",
                        ["AdminSeed:Email"] = AdminEmail,
                        ["AdminSeed:Password"] = AdminPassword,
                        ["AdminSeed:DisplayName"] = "Administrador de pruebas"
                    });
                });
            });

        Client = Factory.CreateClient();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.AddRange(
                new User
                {
                    TipoDocumento = TipoDocumento.NIT,
                    Documento = "800000015-7",
                    Nombre = "Existente en BD",
                    Email = "existente1@demo.com",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new User
                {
                    TipoDocumento = TipoDocumento.CC,
                    Documento = "1000000016",
                    Nombre = "Existente en BD 2",
                    Email = "existente2@demo.com",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            await db.SaveChangesAsync();
        }

        var login = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AdminEmail, AdminPassword));
        login.EnsureSuccessStatusCode();

        var result = await login.Content.ReadFromJsonAsync<LoginResult>();
        AdminToken = result!.Token;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (Factory is not null)
        {
            try
            {
                using var scope = Factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureDeletedAsync();
            }
            catch
            {
                // La limpieza no debe enmascarar el resultado del test.
            }

            await Factory.DisposeAsync();
        }

        if (!string.IsNullOrEmpty(_storagePath) && Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    private static void StartLocalDbIfNeeded()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo("sqllocaldb", "start MSSQLLocalDB")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(10_000);
        }
        catch
        {
            // Si ya está iniciado o el comando no existe, la conexión dirá si hay problema.
        }
    }
}
