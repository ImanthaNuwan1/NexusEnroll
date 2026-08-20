namespace NexusEnroll.Api;

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Student";
}

public class UpdateUserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class EnrollRequest
{
    public string UserId { get; set; } = "";
    public string CourseId { get; set; } = "";
}
