using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ImportaYa.Application.Dtos;
using ImportaYa.Application.Services;
using ImportaYa.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportaYa.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService, AccountService accountService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.AuthenticateAsync(request.Email, request.Password, cancellationToken);

        if (result is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales inválidas",
                Detail = "El email o la contraseña no son correctos, o la cuenta está inactiva."
            });
        }

        return Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // El rol se impone aquí: un registro público nunca puede crear un Admin.
            var account = await accountService.CreateAsync(
                new CreateAccountRequest(
                    request.Email, request.Password, request.DisplayName, AppRole.Usuario),
                cancellationToken);

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

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var idValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? User.Identity?.Name ?? string.Empty;
        var displayName = User.FindFirstValue("displayName") ?? email;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        return Ok(new CurrentUserDto(
            Guid.TryParse(idValue, out var id) ? id : Guid.Empty,
            email,
            displayName,
            role));
    }
}
