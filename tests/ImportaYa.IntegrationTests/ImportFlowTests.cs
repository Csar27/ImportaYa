using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ImportaYa.Application.Dtos;
using ImportaYa.Domain.Entities;
using ImportaYa.Domain.Enums;
using ImportaYa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportaYa.IntegrationTests;

public sealed class ImportFlowTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly HttpClient _client;

    public ImportFlowTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM ImportLogs; DELETE FROM Users; DELETE FROM ImportJobs;");

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

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Csv_upload_of_1200_rows_processes_every_row_and_logs_duplicates()
    {
        var jobId = await UploadSampleAsync("usuarios.csv");

        var job = await WaitForCompletionAsync(jobId);

        AssertExpectedCounters(job);
        job.Format.Should().Be("Csv");
        job.FileName.Should().Be("usuarios.csv");

        var log = await _client.GetFromJsonAsync<PagedResult<ImportLogDto>>(
            $"/api/imports/{jobId}/log?pageSize=500");

        log!.Total.Should().Be(14);
        log.Items.Count(l => l.Motivo.StartsWith("Repetido en el archivo")).Should().Be(3);
        log.Items.Count(l => l.Motivo == "Ya existe en la base de datos").Should().Be(2);
        // La fila 1 es el encabezado: el registro 20 del archivo es la fila 21 y copia la fila 19.
        log.Items.Should().Contain(l => l.Fila == 21 && l.Motivo == "Repetido en el archivo, fila 19");
        log.Items.Should().Contain(l => l.Fila == 16 && l.Motivo == "Ya existe en la base de datos");
        log.Items.Should().Contain(l => l.Fila == 6 && l.Tipo == "Error");
    }

    [Fact]
    public async Task Excel_upload_of_1200_rows_preserves_leading_zeros_and_processes_every_row()
    {
        var jobId = await UploadSampleAsync("usuarios.xlsx");

        var job = await WaitForCompletionAsync(jobId);

        AssertExpectedCounters(job);
        job.Format.Should().Be("Xlsx");

        var log = await _client.GetFromJsonAsync<PagedResult<ImportLogDto>>(
            $"/api/imports/{jobId}/log?tipo=Duplicado&pageSize=500");

        log!.Total.Should().Be(5);
        log.Items.Select(l => l.Fila).Should().BeEquivalentTo([16, 17, 21, 34, 901]);
        log.Items.Should().NotContain(l => l.Documento == "0001234567");
    }

    [Fact]
    public async Task Log_can_be_filtered_and_downloaded_in_both_formats()
    {
        var jobId = await UploadInlineAsync("""
            tipoDocumento,documento,nombre,email,telefono
            CC,0001234567,Ana,ana@demo.com,3001234567
            CC,0001234567,Repetida,repetida@demo.com,3001234567
            CC,ABC123,Mal,mx@demo.com,3001234567
            """);

        var job = await WaitForCompletionAsync(jobId);
        job.CreatedCount.Should().Be(1);
        job.DuplicateCount.Should().Be(1);
        job.ErrorCount.Should().Be(1);

        var errors = await _client.GetFromJsonAsync<PagedResult<ImportLogDto>>(
            $"/api/imports/{jobId}/log?tipo=Error");
        errors!.Total.Should().Be(1);
        errors.Items[0].Fila.Should().Be(4);

        using var csv = await _client.GetAsync($"/api/imports/{jobId}/log?formato=csv");
        csv.StatusCode.Should().Be(HttpStatusCode.OK);
        var csvBody = await csv.Content.ReadAsStringAsync();
        csvBody.Should().StartWith("Fila,TipoDocumento,Documento,Tipo,Motivo,Fecha");
        csvBody.Should().Contain("Repetido en el archivo, fila 2");

        using var xlsx = await _client.GetAsync($"/api/imports/{jobId}/log?formato=xlsx");
        xlsx.StatusCode.Should().Be(HttpStatusCode.OK);
        xlsx.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        (await xlsx.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task Template_downloads_contain_the_expected_headers()
    {
        using var csv = await _client.GetAsync("/api/imports/plantilla?formato=csv");
        csv.StatusCode.Should().Be(HttpStatusCode.OK);
        (await csv.Content.ReadAsStringAsync()).Trim().Should()
            .Be("tipoDocumento,documento,nombre,email,telefono");

        using var xlsx = await _client.GetAsync("/api/imports/plantilla?formato=xlsx");
        xlsx.StatusCode.Should().Be(HttpStatusCode.OK);
        xlsx.Content.Headers.ContentDisposition?.ToString().Should().Contain("plantilla-usuarios.xlsx");
    }

    [Fact]
    public async Task Unsupported_extension_is_rejected_with_400()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("no,es,un,csv"u8.ToArray()), "file", "usuarios.txt");

        var response = await _client.PostAsync("/api/imports", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Extensión no admitida");
    }

    [Fact]
    public async Task History_returns_the_created_jobs()
    {
        var jobId = await UploadInlineAsync("""
            tipoDocumento,documento,nombre,email,telefono
            CC,5555555555,Historial,historial@demo.com,3001234567
            """);
        await WaitForCompletionAsync(jobId);

        var history = await _client.GetFromJsonAsync<PagedResult<ImportJobDto>>("/api/imports?pageSize=5");

        history!.Total.Should().BeGreaterThan(0);
        history.Items.Should().Contain(j => j.Id == jobId);
    }

    [Fact]
    public async Task Imported_users_can_be_listed_and_searched()
    {
        var jobId = await UploadInlineAsync("""
            tipoDocumento,documento,nombre,email,telefono
            CC,7777777777,Usuar Listado,listado@demo.com,3007777777
            """);
        await WaitForCompletionAsync(jobId);

        var all = await _client.GetFromJsonAsync<PagedResult<UserDto>>("/api/users?pageSize=10");
        all!.Total.Should().BeGreaterThanOrEqualTo(3);

        var found = await _client.GetFromJsonAsync<PagedResult<UserDto>>("/api/users?q=listado@demo.com");
        found!.Total.Should().Be(1);
        found.Items[0].Documento.Should().Be("7777777777");
        found.Items[0].Nombre.Should().Be("Usuar Listado");
        found.Items[0].TipoDocumento.Should().Be("CC");
    }

    private async Task<Guid> UploadSampleAsync(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "samples", fileName);
        File.Exists(path).Should().BeTrue($"debe existir el archivo de ejemplo {path}");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(await File.ReadAllBytesAsync(path)), "file", fileName);

        return await PostAndGetJobIdAsync(content);
    }

    private async Task<Guid> UploadInlineAsync(string csv)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(csv)), "file", "inline.csv");
        return await PostAndGetJobIdAsync(content);
    }

    private async Task<Guid> PostAndGetJobIdAsync(MultipartFormDataContent content)
    {
        var response = await _client.PostAsync("/api/imports", content);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted, await response.Content.ReadAsStringAsync());
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<CreateImportResult>();
        result!.JobId.Should().NotBeEmpty();
        return result.JobId;
    }

    private async Task<ImportJobDto> WaitForCompletionAsync(Guid jobId)
    {
        for (var attempt = 0; attempt < 160; attempt++)
        {
            var job = await _client.GetFromJsonAsync<ImportJobDto>($"/api/imports/{jobId}");
            job.Should().NotBeNull();

            if (job!.Status is "Completed" or "Failed")
            {
                return job;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"El trabajo {jobId} no terminó a tiempo.");
    }

    private static void AssertExpectedCounters(ImportJobDto job)
    {
        job.Status.Should().Be("Completed", job.ErrorMessage);
        job.ProcessedCount.Should().Be(1200);
        job.CreatedCount.Should().Be(1186);
        job.ErrorCount.Should().Be(9);
        job.DuplicateCount.Should().Be(5);
    }
}
