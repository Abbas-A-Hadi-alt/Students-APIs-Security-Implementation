using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using StudentApi.DataSimulation;
using StudentApi.DTOs.Auth;
using StudentApi.Model;
using LoginRequest = StudentApi.DTOs.Auth.LoginRequest;

namespace StudentApi.Controllers;

[ApiController]
[Route("api/Auth")]
public sealed class AuthController(
	IConfiguration config,
	ILogger<AuthController> logger)
: ControllerBase
{
	[HttpPost("login")]
	[EnableRateLimiting("AuthLimiter")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public IActionResult Login(LoginRequest request)
	{
		Student? student = StudentDataSimulation.StudentsList
			.FirstOrDefault(x => x.Email == request.Email);

		var ip =  HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

		if (student is null)
		{
			logger.LogWarning(
				"Failed login attempt (email not found). Email={Email}, IP={IP}",
				request.Email,
				ip
			);

			// Return generic message to avoid revealing whether email exists.
			return Unauthorized("Invalid credentials");
		}

		bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, student.PasswordHash);

		if (!isValidPassword)
		{
			logger.LogWarning(
				"Failed login attempt (bad password). Email={Email}, IP={IP}",
				request.Email,
				ip
			);

			// Return generic message to avoid revealing which field is wrong.
			return Unauthorized("Invalid credentials");
		}

		string jwt = TokenIssue(student);
		string refreshToken = RefreshTokenIssue();

		student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
		student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
		student.RefreshTokenRevokedAt = null;

		logger.LogInformation(
			"Successful login. UserId={UserId}, Email={Email}, IP={IP}",
			student.Id,
			student.Email,
			ip
		);

		return Ok(new TokenResponse(AccessToken: jwt, RefreshToken: refreshToken));
	}


	[HttpPost("refresh")]
	[EnableRateLimiting("AuthLimiter")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public IActionResult Refresh(RefreshRequest request)
	{
		var ip =  HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

		Student? student = StudentDataSimulation.StudentsList
			.FirstOrDefault(x => x.Email == request.Email);

		if (student is null)
		{
			logger.LogWarning(
				"Invalid refresh attempt (email not found). Email={Email}, IP={IP}",
				request.Email,
				ip
			);

			return Unauthorized("Invalid refresh request");
		}

		if (student.RefreshTokenRevokedAt is not null)
		{
			logger.LogWarning(
				"Refresh attempt using revoked token. UserId={UserId}, Email={Email}, IP={IP}",
				student.Id,
				student.Email,
				ip
			);

			return Unauthorized("Refresh token is revoked");
		}

		if (student.RefreshTokenExpiresAt is null || student.RefreshTokenExpiresAt <= DateTime.Now)
		{
			logger.LogWarning(
				"Refresh attempt using expired token. UserId={UserId}, Email={Email}, IP={IP}",
				student.Id,
				student.Email,
				ip
			);

			return Unauthorized("Refresh token expired");
		}

		var isRefreshTokenValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, student.RefreshTokenHash);

		if (!isRefreshTokenValid)
		{
			logger.LogWarning(
				"Invalid refresh token attempt. UserId={UserId}, Email={Email}, IP={IP}",
				student.Id,
				student.Email,
				ip
			);

			return Unauthorized("Invalid refresh token");
		}

		string accessToken = TokenIssue(student);
		string refreshToken = RefreshTokenIssue();

		student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
		student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
		student.RefreshTokenRevokedAt = null;

		return Ok(new TokenResponse(accessToken, refreshToken));
	}


	[HttpPost("logout")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public IActionResult Logout(LogoutRequest request)
	{
		Student? student = StudentDataSimulation.StudentsList
			.FirstOrDefault(x => x.Email == request.Email);

		if (student is null)
		{
			return Unauthorized("Invalid Credentials");
		}

		if (student.RefreshTokenRevokedAt is not null)
		{
			return Unauthorized("Invalid Credentials");
		}

		if (student.RefreshTokenExpiresAt is null || student.RefreshTokenExpiresAt <= DateTime.Now)
		{
			return Unauthorized("Invalid Credentials");
		}

		bool isRefreshTokenValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, student.RefreshTokenHash);

		if (!isRefreshTokenValid)
		{
			return Unauthorized("Invalid Credentials");
		}

		student.RefreshTokenRevokedAt = DateTime.UtcNow;

		return Ok("Logged out successfully");
	}


	public static string MaskEmail(string email)
	{
		const int secondPartLength = 6;

		var emailAsTwoParts = email.Split('@');

		var maskedEmail = new StringBuilder(secondPartLength + emailAsTwoParts[1].Length)
			.Append(emailAsTwoParts[0].Length > 2 ? emailAsTwoParts[0][..2] : emailAsTwoParts[0])
			.Append("****@")
			.Append(emailAsTwoParts[1])
			.ToString();

		return maskedEmail;
	}

	private string TokenIssue(Student student)
	{
		var key = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(config["JwtSigningKey"]
			                       ?? throw new KeyNotFoundException("'JWT_SECRET_KEY' key is not found in Environment Variables.")));

		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		Claim[] claims = [
			new (ClaimTypes.NameIdentifier, student.Id.ToString()),
			new (ClaimTypes.Email, student.Email),
			new (ClaimTypes.Role, student.Role),
		];

		var token = new JwtSecurityToken(
			claims: claims,
			expires: DateTime.Now.AddMinutes(5),
			signingCredentials: credentials,
			issuer: "StudentApi",
			audience: "StudentApiUsers");

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private string RefreshTokenIssue()
	{
		var bytes = new byte[64];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(bytes);
		return Convert.ToBase64String(bytes);
	}
}
