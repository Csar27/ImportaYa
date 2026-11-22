using ImportaYa.Application.Dtos;
using ImportaYa.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportaYa.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize(Policy = "AdminOnly")]
public sealed class AccountsController(AccountService accountService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await accountService.ListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await accountService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No se pudo crear la cuenta",
                Detail = ex.Message
            });
        }
    }
}
