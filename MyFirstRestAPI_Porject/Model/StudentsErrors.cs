namespace StudentApi.Model;

public static class StudentsErrors
{
    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Students.NotFoundByEmail",
        "The student with the specified email was not found");
    
    public static readonly Error EmailNotUnique = Error.Conflict(
        "Students.EmailNotUnique",
        "The provided email is not unique");
    
    public static Error Unauthorized() => Error.Failure(
        "Students.Unauthorized",
        "You are not authorized to perform this action.");
    
    public static Error NotFound(int id) => Error.NotFound(
        "Students.NotFoundById",
        $"The student with id = {id} was not found");
}