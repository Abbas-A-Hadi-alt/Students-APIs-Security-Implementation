using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StudentApi.DTOs.Auth;
using StudentApi.Model;
using StudentApi.Services;

namespace StudentApi.Controllers;

[ApiController]
[Route("api/Auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
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
            _ => Ok(token)
        };
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Refresh(RefreshRequest refreshRequest)
    {
        var (token, error) = authService.Refresh(refreshRequest);

        return (token, error.Type) switch
        {
            (null, ErrorTypes.Failure) => BadRequest(error.Description),
            (null, ErrorTypes.NotFound) or (_, ErrorTypes.Unauthorized) => Unauthorized(error.Description),
            _ => Ok(token)
        };
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout(LogoutRequest logoutRequest)
    {
        var (student, error) = authService.Logout(logoutRequest);

        return (student, error.Type) switch
        {
            (null, _) => Ok(),
            _ => Ok("Logged out successfully")
        };
    }
}