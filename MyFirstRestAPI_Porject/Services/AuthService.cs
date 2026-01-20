using System.Security.Claims;
using System.Text;
using StudentApi.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using StudentApi.DataSimulation;

namespace StudentApi.Services;

public sealed class AuthService(IConfiguration config) : IAuthService
{
	public (TokenResponse? Token, Error Error) Login(LoginRequest loginRequest)
	{
		Student? student = StudentDataSimulation.StudentsList.FirstOrDefault(x => x.Email == loginRequest.Email);

		if (student is null)
		{
			return (Token: null, Error.NotFound("Auth.NotFound", "Invalid Credentials"));
		}

		bool isValidPassword = BCrypt.Net.BCrypt.Verify(loginRequest.Password, student.PasswordHash);

		if (!isValidPassword)
		{
			return (Token: null, Error.Unauthorized("Auth.Unauthorized", "Invalid Credentials"));
		}

		string jwt = TokenIssue(student);
		TokenResponse token = new(jwt);

		return (Token: token, Error.None);
	}

	public string TokenIssue(Student student)
	{
		var key = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(config["JWT_SECRET_KEY"] 
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
			issuer: config["StudentApi.Issuer"]!,
			audience: config["StudentApi.Audience"]!);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
