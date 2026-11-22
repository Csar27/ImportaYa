using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Dtos;
using ImportaYa.Application.Options;
using ImportaYa.Application.Services;
using ImportaYa.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ImportaYa.Api.Controllers;

[ApiController]
[Route("api/imports")]
[Authorize(Policy = "ImportAllowed")]
public sealed class ImportsController(
    ImportService importService,
    ImportQueryService queryService,
    ILogExporter logExporter,
    ITemplateGenerator templateGenerator,
    IOptions<ImportOptions> options) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType(typeof(CreateImportResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo requerido",
                Detail = "Adjunte un archivo .csv o .xlsx en el campo 'file'."
            });
        }

        if (file.Length > options.Value.MaxFileSizeBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo demasiado grande",
                Detail = $"El tamaño máximo permitido es {options.Value.MaxFileSizeBytes / (1024 * 1024)} MB."
            });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await importService.CreateAsync(file.FileName, file.Length, stream, cancellationToken);

            Response.Headers.Location = Url.Action(nameof(GetById), new { id = result.JobId }) ?? string.Empty;
            return Accepted(result);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Archivo no válido",
                Detail = ex.Message
            });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ImportJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await queryService.ListAsync(page, pageSize, cancellationToken));

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(ImportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var job = await queryService.GetAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("{id:guid}/log")]
    [ProducesResponseType(typeof(PagedResult<ImportLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLog(
        Guid id,
        [FromQuery] string? tipo,
        [FromQuery] string? formato,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseLogTipo(tipo, out var logTipo))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Filtro no válido",
                Detail = "El parámetro 'tipo' debe ser 'Error' o 'Duplicado'."
            });
        }

        if (await queryService.GetAsync(id, cancellationToken) is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(formato))
        {
            return Ok(await queryService.GetLogAsync(id, logTipo, page, pageSize, cancellationToken));
        }

        if (!TryParseFormat(formato, out var format))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Formato no válido",
                Detail = "Use formato=csv o formato=xlsx."
            });
        }

        var stream = new MemoryStream();
        await logExporter.ExportAsync(id, logTipo, format, stream, cancellationToken);
        stream.Position = 0;

        var contentType = format == ImportFormat.Csv
            ? "text/csv; charset=utf-8"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var extension = format == ImportFormat.Csv ? "csv" : "xlsx";

        return File(stream, contentType, $"log-{id}.{extension}");
    }

    [HttpGet("plantilla")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTemplate(
        [FromQuery] string? formato, CancellationToken cancellationToken = default)
    {
        if (!TryParseFormat(formato, out var format))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Formato no válido",
                Detail = "Use formato=csv o formato=xlsx."
            });
        }

        var stream = new MemoryStream();
        await templateGenerator.GenerateAsync(format, stream, cancellationToken);
        stream.Position = 0;

        var contentType = format == ImportFormat.Csv
            ? "text/csv; charset=utf-8"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var extension = format == ImportFormat.Csv ? "csv" : "xlsx";

        return File(stream, contentType, $"plantilla-usuarios.{extension}");
    }

    private static bool TryParseFormat(string? value, out ImportFormat format)
    {
        format = default;
        return !string.IsNullOrWhiteSpace(value)
               && Enum.TryParse(value, ignoreCase: true, out format)
               && Enum.IsDefined(format);
    }

    private static bool TryParseLogTipo(string? value, out LogTipo? tipo)
    {
        tipo = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse<LogTipo>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            return false;
        }

        tipo = parsed;
        return true;
    }
}
