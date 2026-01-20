namespace StudentApi.Model;

public sealed class Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorTypes.Failure);
    
    public static readonly Error NullValue = new(
        "General.Null", 
        "Null value was provided", 
        ErrorTypes.Failure);
    
    public string Code { get; init; }
    public string Description { get; init; }
    public ErrorTypes Type { get; init; }
    
    public Error(string code, string description, ErrorTypes type)
    {
        Code = code;
        Description = description;
        Type = type;
    }
    
    public static Error Failure(string code, string description)
        => new(code, description, ErrorTypes.Failure);
    
    public static Error Validation(string code, string description)
        => new(code, description, ErrorTypes.Validation);
    
    public static Error Problem(string code, string description)
        => new(code, description, ErrorTypes.Problem);
    
    public static Error NotFound(string code, string description)
        => new(code, description, ErrorTypes.NotFound);
    
    public static Error Conflict(string code, string description)
        => new(code, description, ErrorTypes.Conflict);
    
    public static Error Unauthorized(string code, string description)
        => new(code, description, ErrorTypes.Unauthorized);
}