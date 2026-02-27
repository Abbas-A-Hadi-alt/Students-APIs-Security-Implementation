using StudentApi.DTOs.Auth;
using StudentApi.Model;

namespace StudentApi.Services;

public interface IAuthService
{
    (TokenResponse? Token, Error Error) Login(LoginRequest loginRequest);
    
    (Student? Student, Error Error) Logout(LogoutRequest logoutRequest);
    
    (TokenResponse? Token, Error Error) Refresh(RefreshRequest refreshRequest);
    
    string TokenIssue(Student student);
    
    string RefreshTokenIssue();
}