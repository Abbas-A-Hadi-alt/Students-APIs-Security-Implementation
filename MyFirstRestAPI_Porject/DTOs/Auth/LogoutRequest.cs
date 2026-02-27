namespace StudentApi.DTOs.Auth;

public sealed record LogoutRequest(string RefreshToken, string Email);