using StudentApi.Model;

namespace StudentApi.DataSimulation;

public static class StudentDataSimulation
{
    // Static list of students, acting as an in-memory data store, you can change it later on to retrieve students from Database.
    public static readonly List<Student> StudentsList= [
        // Initialize the list with some student objects.
        new Student 
        {
            Id = 1, 
            Name = "Ali Ahmed", 
            Age = 20, 
            Grade = 88,
            Email = "ali.ahmed@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin"
        },
        new Student 
        {
            Id = 2, 
            Name = "Fadi Khalil", 
            Age = 22, 
            Grade = 77,
            Email = "fadi.Khalil@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
            Role = "Student"
        },
        new Student 
        {
            Id = 3, 
            Name = "Ola Jabber", 
            Age = 21, 
            Grade = 66,
            Email = "ola.jabber@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password2"),
            Role = "Student"
        },
        new Student 
        {
            Id = 4, 
            Name = "Alia Maher", 
            Age = 19, 
            Grade = 44,
            Email = "alia.maher@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password3"),
            Role = "Student"
        }
    ];

}
