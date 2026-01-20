namespace StudentApi.Model;

public enum ErrorTypes : byte
{
    Failure = 0,
    Validation = 1,
    Problem = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5
}