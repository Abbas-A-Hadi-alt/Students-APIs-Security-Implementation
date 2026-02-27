namespace StudentApi.DTOs.Auth;

public sealed record TokenResponse(string AccessToken, string RefreshToken);