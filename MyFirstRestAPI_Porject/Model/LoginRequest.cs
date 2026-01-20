namespace StudentApi.Model;

public sealed class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}