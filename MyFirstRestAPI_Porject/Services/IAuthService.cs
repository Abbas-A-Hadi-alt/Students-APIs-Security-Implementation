using StudentApi.Model;

namespace StudentApi.Services;

public interface IAuthService
{
    (TokenResponse? Token, Error Error) Login(LoginRequest loginRequest);
    
    string TokenIssue(Student student);
}