using System.Collections.Concurrent;
using NexusEnroll.Api;
using NexusEnroll.Models;
using NexusEnroll.Patterns;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// ---- Single in-memory domain instance + auth state (demo-grade, one process) ----
var facade = new UniversityFacade();
facade.AttachObserver(new ConsoleNotificationObserver());
DemoData.Seed(facade);

var tokens = new ConcurrentDictionary<string, string>(); // token -> userId
var gate = new object(); // guards mutations to the shared facade state

string IssueToken(string userId)
{
    var token = Guid.NewGuid().ToString("N");
    tokens[token] = userId;
    return token;
}

User Authenticate(HttpRequest request)
{
    var header = request.Headers["Authorization"].ToString();
    if (!header.StartsWith("Bearer ")) return null;
    var token = header["Bearer ".Length..];
    return tokens.TryGetValue(token, out var userId) && facade.Users.TryGetValue(userId, out var user)
        ? user
        : null;
}

object ToUserDto(User u) => new { id = u.UserId, name = u.FullName, email = u.Email, role = u.Role.ToString() };

app.MapGet("/", () => "NexusEnroll API is running.");

// ================= AUTH =================
// The domain model (backend/Models/User.cs) has no password field, so this
// bridge can only verify the email belongs to a known user - good enough for
// the course demo, not real authentication.
app.MapPost("/api/auth/login", (LoginRequest body) =>
{
    if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        return Results.Json(new { message = "Email and password are required." }, statusCode: 400);

    var user = facade.Users.Values.FirstOrDefault(u =>
        string.Equals(u.Email, body.Email, StringComparison.OrdinalIgnoreCase));

    if (user == null || !user.IsActive)
        return Results.Json(new { message = "Invalid credentials." }, statusCode: 401);

    user.RecordLogin();
    return Results.Json(new { token = IssueToken(user.UserId), user = ToUserDto(user) });
});

app.MapPost("/api/auth/register", (RegisterRequest body) =>
{
    if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        return Results.Json(new { message = "All fields are required." }, statusCode: 400);

    lock (gate)
    {
        if (facade.Users.Values.Any(u => string.Equals(u.Email, body.Email, StringComparison.OrdinalIgnoreCase)))
            return Results.Json(new { message = "An account with that email already exists." }, statusCode: 400);

        bool asFaculty = string.Equals(body.Role, "Faculty", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(body.Role, "Instructor", StringComparison.OrdinalIgnoreCase);

        var id = (asFaculty ? "FAC" : "STU") + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        User created = asFaculty
            ? facade.CreateAndRegisterFaculty(id, body.Name, body.Email, "", "EMP-" + id, "General", "Lecturer")
            : facade.CreateAndRegisterStudent(id, body.Name, body.Email, "", "S-" + id, "BS-CS", DateTime.UtcNow.Year);

        return Results.Json(new { user = ToUserDto(created) }, statusCode: 201);
    }
});

app.MapPost("/api/auth/logout", (HttpRequest request) =>
{
    var header = request.Headers["Authorization"].ToString();
    if (header.StartsWith("Bearer "))
        tokens.TryRemove(header["Bearer ".Length..], out _);
    return Results.Ok();
});

// ================= USERS =================
app.MapGet("/api/users", (HttpRequest request) =>
{
    if (Authenticate(request) == null) return Results.Json(new { message = "Unauthorized" }, statusCode: 401);
    return Results.Json(facade.Users.Values.Select(ToUserDto));
});

app.MapGet("/api/users/{id}", (string id) =>
    facade.Users.TryGetValue(id, out var user)
        ? Results.Json(ToUserDto(user))
        : Results.Json(new { message = "User not found." }, statusCode: 404));

app.MapPut("/api/users/{id}", (string id, UpdateUserRequest body) =>
{
    if (!facade.Users.TryGetValue(id, out var user))
        return Results.Json(new { message = "User not found." }, statusCode: 404);

    lock (gate)
    {
        if (!string.IsNullOrWhiteSpace(body.Name)) user.FullName = body.Name;
        if (!string.IsNullOrWhiteSpace(body.Email)) user.Email = body.Email;
    }
    return Results.Json(ToUserDto(user));
});

// ================= COURSES =================
app.MapGet("/api/courses/available", (string department = null, string keyword = null, string instructor = null) =>
{
    var dto = facade.BrowseCatalog(department, keyword, instructor).Select(c => new
    {
        id = c.CourseId,
        code = c.CourseId,
        title = c.CourseName,
        credits = c.Credits,
        instructor = c.InstructorName,
        slots = c.AvailableSeats
    });
    return Results.Json(dto);
});

// ================= DASHBOARD =================
app.MapGet("/api/dashboard/stats", (string userId = null) =>
{
    if (userId == null || !facade.Users.TryGetValue(userId, out var u) || u is not Student student)
        return Results.Json(new { total_enrollments = 0, active_courses = 0, completed = 0, pending = 0 });

    int active = student.EnrolledCourseIds.Count;
    int pending = student.WaitlistedCourseIds.Count;
    int completed = student.AcademicHistory.Count;
    return Results.Json(new
    {
        total_enrollments = active + pending + completed,
        active_courses = active,
        completed,
        pending
    });
});

app.MapGet("/api/enrollments/recent", (string userId = null) =>
{
    if (userId == null || !facade.Users.TryGetValue(userId, out var u) || u is not Student student)
        return Results.Json(Array.Empty<object>());

    var completed = student.AcademicHistory
        .OrderByDescending(r => r.CompletedAt)
        .Select(r => new { course = r.CourseName, status = "Completed", date = r.CompletedAt.ToString("yyyy-MM-dd") });

    var active = facade.GetStudentSchedule(userId)
        .Select(c => new { course = c.CourseName, status = "Active", date = DateTime.UtcNow.ToString("yyyy-MM-dd") });

    return Results.Json(completed.Concat(active).Take(5));
});

// ================= ENROLLMENTS =================
app.MapPost("/api/enrollments", (EnrollRequest body) =>
{
    lock (gate)
    {
        bool ok = facade.EnrollStudentInCourse(body.UserId, body.CourseId, out var message);
        return ok
            ? Results.Json(new { message }, statusCode: 201)
            : Results.Json(new { message }, statusCode: 400);
    }
});

app.MapGet("/api/enrollments/user/{userId}", (string userId) =>
{
    if (!facade.Users.TryGetValue(userId, out var u) || u is not Student student)
        return Results.Json(Array.Empty<object>());

    var active = facade.GetStudentSchedule(userId).Select(c => new
    {
        id = c.CourseId,
        course = c.CourseName,
        code = c.CourseId,
        status = "Active",
        enrolled_date = "",
        grade = "-"
    });

    var waitlisted = student.WaitlistedCourseIds
        .Select(cid => facade.GetCourseDetails(cid))
        .Where(c => c != null)
        .Select(c => new
        {
            id = c.CourseId,
            course = c.CourseName,
            code = c.CourseId,
            status = "Pending",
            enrolled_date = "",
            grade = "-"
        });

    var completed = student.AcademicHistory.Select(r => new
    {
        id = r.CourseId,
        course = r.CourseName,
        code = r.CourseId,
        status = "Completed",
        enrolled_date = r.CompletedAt.ToString("yyyy-MM-dd"),
        grade = r.Grade.ToString()
    });

    return Results.Json(active.Concat(waitlisted).Concat(completed));
});

app.MapDelete("/api/enrollments/{courseId}", (string courseId, string userId = null) =>
{
    if (userId == null || !facade.Users.TryGetValue(userId, out var u) || u is not Student student)
        return Results.Json(new { message = "Student not found." }, statusCode: 404);

    lock (gate)
    {
        if (student.EnrolledCourseIds.Contains(courseId))
        {
            bool ok = facade.DropCourseAndPromoteWaitlist(userId, courseId, out var message);
            return ok ? Results.Ok(new { message }) : Results.Json(new { message }, statusCode: 400);
        }

        if (student.WaitlistedCourseIds.Contains(courseId))
        {
            facade.GetCourseDetails(courseId)?.RemoveFromWaitlist(userId);
            student.WaitlistedCourseIds.Remove(courseId);
            return Results.Ok(new { message = "Removed from waitlist." });
        }
    }

    return Results.Json(new { message = "Not enrolled in this course." }, statusCode: 400);
});

// ================= ADMIN =================
app.MapGet("/api/enrollments", (HttpRequest request) =>
{
    if (Authenticate(request) == null) return Results.Json(new { message = "Unauthorized" }, statusCode: 401);

    var rows = facade.Users.Values.OfType<Student>().SelectMany(s =>
        s.EnrolledCourseIds.Select(cid => new
        {
            id = s.UserId + ":" + cid,
            student = s.FullName,
            course = facade.GetCourseDetails(cid) != null ? facade.GetCourseDetails(cid).CourseName : cid,
            status = "Active",
            date = ""
        })
        .Concat(s.AcademicHistory.Select(r => new
        {
            id = s.UserId + ":" + r.CourseId,
            student = s.FullName,
            course = r.CourseName,
            status = "Completed",
            date = r.CompletedAt.ToString("yyyy-MM-dd")
        })));

    return Results.Json(rows);
});

app.Run();
