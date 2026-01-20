using Microsoft.AspNetCore.Mvc;
using StudentApi.Model;
using StudentApi.Services;

namespace StudentApi.Controllers;

[ApiController]
[Route("api/Auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("Login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login(LoginRequest loginRequest)
    {
        var (token, error) = authService.Login(loginRequest);

        return (token, error.Type) switch
        {
            (null, ErrorTypes.Failure) => BadRequest(error.Description),
            (null, ErrorTypes.NotFound) or (_, ErrorTypes.Unauthorized) => Unauthorized(error.Description),
            (null, ErrorTypes.Problem) => Problem(error.Description),
            _ => Ok(token)
        };
    }
}