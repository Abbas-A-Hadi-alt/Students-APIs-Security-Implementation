using System.Security.Claims;
using System.Text;
using StudentApi.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using StudentApi.DataSimulation;
using StudentApi.DTOs.Auth;

namespace StudentApi.Services;

public sealed class AuthService(IConfiguration config) : IAuthService
{
	public (TokenResponse? Token, Error Error) Login(LoginRequest loginRequest)
	{
		Student? student = StudentDataSimulation.StudentsList
			.FirstOrDefault(x => x.Email == loginRequest.Email);

		if (student is null)
		{
			return (null, Error.NotFound("Auth.NotFound", "Invalid Credentials"));
		}

		bool isValidPassword = BCrypt.Net.BCrypt.Verify(loginRequest.Password, student.PasswordHash);

		if (!isValidPassword)
		{
			return (null, Error.Unauthorized("Auth.Unauthorized", "Invalid Credentials"));
		}

		string jwt = TokenIssue(student);
		string refreshToken = RefreshTokenIssue();
		
		student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
		student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
		student.RefreshTokenRevokedAt = null;
		
		TokenResponse token = new(AccessToken: jwt, RefreshToken: refreshToken);

		return (token, Error.None);
	}

	public (TokenResponse? Token, Error Error) Refresh(RefreshRequest refreshRequest)
	{
		Student? student = StudentDataSimulation.StudentsList
			.FirstOrDefault(x => x.Email == refreshRequest.Email);

		if (student is null)
		{
			return (null, Error.NotFound("Auth.NotFound", "Invalid Credentials"));
		}

		if (student.RefreshTokenRevokedAt is not null)
		{
			return (null, Error.Unauthorized("Auth.Unauthorized", "Refresh token is revoked"));
		}

		if (student.RefreshTokenExpiresAt is null || student.RefreshTokenExpiresAt <= DateTime.Now)
		{
			return (null, Error.Unauthorized("Auth.Unauthorized", "Refresh token is expired"));
		}
		
		bool isRefreshTokenValid = BCrypt.Net.BCrypt.Verify(refreshRequest.RefreshToken, student.RefreshTokenHash);

		if (!isRefreshTokenValid)
		{
			return (null, Error.Unauthorized("Auth.Unauthorized", "Invalid refresh token"));
		}
		
		string accessToken = TokenIssue(student);
		string refreshToken = RefreshTokenIssue();
		
		student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
		student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
		student.RefreshTokenRevokedAt = null;
		
		return (new TokenResponse(accessToken, refreshToken), Error.None);
	}

	public (Student? Student, Error Error) Logout(LogoutRequest logoutRequest)
	{
		Student? student = StudentDataSimulation.StudentsList
			.FirstOrDefault(x => x.Email == logoutRequest.Email);

		if (student is null)
		{
			return (null, Error.NotFound("Auth.NotFound", "Invalid Credentials"));
		}

		if (student.RefreshTokenRevokedAt is not null)
		{
			return (null, Error.Unauthorized("Auth.Unauthorized", "Invalid Credentials"));
		}
		
		if (student.RefreshTokenExpiresAt is null || student.RefreshTokenExpiresAt <= DateTime.Now)
		{
			return (null, Error.Unauthorized("Auth.Unauthorized", "Invalid Credentials"));
		}
		
		bool isRefreshTokenValid = BCrypt.Net.BCrypt.Verify(logoutRequest.RefreshToken, student.RefreshTokenHash);
		
		if (!isRefreshTokenValid)
		{
			return (null, Error.Unauthorized("Auth.Unauthorized", "Invalid Credentials"));
		}
		
		student.RefreshTokenRevokedAt = DateTime.UtcNow;
		
		return (student, Error.None);
	}

	public string TokenIssue(Student student)
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

	public string RefreshTokenIssue()
	{
		var bytes = new byte[64];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(bytes);
		return Convert.ToBase64String(bytes);
	}
}
