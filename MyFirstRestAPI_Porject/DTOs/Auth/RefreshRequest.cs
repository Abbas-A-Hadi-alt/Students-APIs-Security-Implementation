namespace StudentApi.DTOs.Auth;

public sealed record RefreshRequest(string RefreshToken, string Email);