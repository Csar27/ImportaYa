using ImportaYa.Application.Dtos;
using ImportaYa.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportaYa.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "ImportAllowed")]
public sealed class UsersController(UserQueryService queryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await queryService.ListAsync(q, page, pageSize, cancellationToken));
}
