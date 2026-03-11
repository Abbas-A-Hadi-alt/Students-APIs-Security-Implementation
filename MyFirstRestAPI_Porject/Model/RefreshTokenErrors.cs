namespace StudentApi.Model;

public static class RefreshTokenErrors
{
    public static readonly Error NotFound = Error.Unauthorized("RefreshToken.NotFound",
        "Refresh token is not found");
    
    public static readonly Error Invalid = Error.Unauthorized("RefreshToken.Invalid",
        "Refresh token is invalid");
    
    public static readonly Error Revoked = Error.Unauthorized("RefreshToken.Revoked",
        "Refresh token is revoked");
    
    public static readonly Error Expired = Error.Unauthorized("RefreshToken.Expired",
        "Refresh token is expired");
}