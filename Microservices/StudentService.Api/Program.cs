using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusEnroll.Shared;
using NexusEnroll.StudentService;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5010");

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure Database (SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Data Source=StudentDb.db";
builder.Services.AddDbContext<StudentDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Services & Messaging
builder.Services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddHostedService<RabbitMQEventConsumer>();

// Register Global Exception Handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

// Ensure Database is Created & Seeded
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Seed initial degree program, students, and courses if empty
    if (!dbContext.Students.Any())
    {
        // 1. Seed Degree Program
        var csProgram = new DegreeProgram
        {
            ProgramId = "BS-CS",
            ProgramName = "BSc in Computer Science",
            Department = "Computer Science",
            RequiredCourseIds = new List<string> { "CS101", "CS201", "CS301" },
            ElectiveCourseIds = new List<string> { "SE101" }
        };
        dbContext.DegreePrograms.Add(csProgram);

        // 2. Seed Courses
        var cs101 = new Course
        {
            CourseId = "CS101",
            CourseCode = "CS 101",
            CourseName = "Intro to Programming",
            Description = "Fundamental principles of programming and algorithms.",
            Department = "Computer Science",
            InstructorId = "FAC001",
            InstructorName = "Dr. Alan Turing",
            Credits = 3,
            Semester = "Fall 2026",
            Capacity = 5,
            EnrolledCount = 0,
            Days = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Location = "Hall A",
            Status = CourseStatus.Open
        };
        dbContext.Courses.Add(cs101);

        var cs201 = new Course
        {
            CourseId = "CS201",
            CourseCode = "CS 201",
            CourseName = "Data Structures & Algorithms",
            Description = "Abstract data types, trees, graphs, and algorithmic complexity.",
            Department = "Computer Science",
            InstructorId = "FAC001",
            InstructorName = "Dr. Alan Turing",
            Credits = 4,
            Semester = "Fall 2026",
            Capacity = 2,
            EnrolledCount = 0,
            Days = "MWF",
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            Location = "Hall B",
            Status = CourseStatus.Open,
            PrerequisiteCourseIds = new List<string> { "CS101" }
        };
        dbContext.Courses.Add(cs201);

        var cs301 = new Course
        {
            CourseId = "CS301",
            CourseCode = "CS 301",
            CourseName = "Advanced Algorithms",
            Description = "Greedy algorithms, dynamic programming, and NP-completeness.",
            Department = "Computer Science",
            InstructorId = "FAC002",
            InstructorName = "Prof. Grace Hopper",
            Credits = 4,
            Semester = "Fall 2026",
            Capacity = 2,
            EnrolledCount = 0,
            Days = "TTh",
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11.5),
            Location = "Lab 3",
            Status = CourseStatus.Open,
            PrerequisiteCourseIds = new List<string> { "CS201" }
        };
        dbContext.Courses.Add(cs301);

        var cs302 = new Course
        {
            CourseId = "CS302",
            CourseCode = "CS 302",
            CourseName = "Operating Systems",
            Description = "Concurrency, processes, memory management, and file systems.",
            Department = "Computer Science",
            InstructorId = "FAC002",
            InstructorName = "Prof. Grace Hopper",
            Credits = 4,
            Semester = "Fall 2026",
            Capacity = 2,
            EnrolledCount = 0,
            Days = "MWF",
            StartTime = TimeSpan.FromHours(10.5),
            EndTime = TimeSpan.FromHours(11.5),
            Location = "Hall C",
            Status = CourseStatus.Open,
            PrerequisiteCourseIds = new List<string> { "CS201" }
        };
        dbContext.Courses.Add(cs302);

        var se101 = new Course
        {
            CourseId = "SE101",
            CourseCode = "SE 101",
            CourseName = "Software Architecture",
            Description = "Design patterns, architectural styles, and microservices design.",
            Department = "Software Engineering",
            InstructorId = "FAC001",
            InstructorName = "Dr. Alan Turing",
            Credits = 3,
            Semester = "Fall 2026",
            Capacity = 2,
            EnrolledCount = 0,
            Days = "TTh",
            StartTime = TimeSpan.FromHours(14),
            EndTime = TimeSpan.FromHours(15.5),
            Location = "Hall D",
            Status = CourseStatus.Open,
            PrerequisiteCourseIds = new List<string> { "CS101" }
        };
        dbContext.Courses.Add(se101);

        // 3. Seed Students & History
        var s1 = new Student
        {
            UserId = "STU001",
            FullName = "John Doe",
            Email = "john.doe@nexus.edu",
            Phone = "555-0301",
            StudentNumber = "S1001",
            ProgramId = "BS-CS",
            EnrolledYear = 2023,
            IsActive = true
        };
        s1.AcademicHistory.Add(new CourseRecord
        {
            StudentId = "STU001",
            CourseId = "CS101",
            CourseCode = "CS 101",
            CourseName = "Intro to Programming",
            Semester = "Spring 2024",
            Grade = Grade.A,
            Credits = 3
        });
        dbContext.Students.Add(s1);

        var s2 = new Student
        {
            UserId = "STU002",
            FullName = "Jane Smith",
            Email = "jane.smith@nexus.edu",
            Phone = "555-0302",
            StudentNumber = "S1002",
            ProgramId = "BS-CS",
            EnrolledYear = 2026,
            IsActive = true
        };
        dbContext.Students.Add(s2);

        var s3 = new Student
        {
            UserId = "STU003",
            FullName = "Bob Johnson",
            Email = "bob.johnson@nexus.edu",
            Phone = "555-0303",
            StudentNumber = "S1003",
            ProgramId = "BS-CS",
            EnrolledYear = 2024,
            IsActive = true
        };
        s3.AcademicHistory.Add(new CourseRecord
        {
            StudentId = "STU003",
            CourseId = "CS101",
            CourseCode = "CS 101",
            CourseName = "Intro to Programming",
            Semester = "Spring 2024",
            Grade = Grade.B,
            Credits = 3
        });
        s3.AcademicHistory.Add(new CourseRecord
        {
            StudentId = "STU003",
            CourseId = "CS201",
            CourseCode = "CS 201",
            CourseName = "Data Structures & Algorithms",
            Semester = "Fall 2024",
            Grade = Grade.A,
            Credits = 4
        });
        dbContext.Students.Add(s3);

        var s4 = new Student
        {
            UserId = "STU004",
            FullName = "Charlie Brown",
            Email = "charlie.b@nexus.edu",
            Phone = "555-0304",
            StudentNumber = "S1004",
            ProgramId = "BS-CS",
            EnrolledYear = 2025,
            IsActive = true
        };
        s4.AcademicHistory.Add(new CourseRecord
        {
            StudentId = "STU004",
            CourseId = "CS101",
            CourseCode = "CS 101",
            CourseName = "Intro to Programming",
            Semester = "Spring 2025",
            Grade = Grade.C,
            Credits = 3
        });
        dbContext.Students.Add(s4);

        dbContext.SaveChanges();
    }
}

// =========================================================================
// VALIDATION HELPER
// =========================================================================
static IResult ValidateDto<T>(T dto)
{
    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(dto, context, results, true))
    {
        return Results.BadRequest(new { errors = results.Select(r => r.ErrorMessage).ToList() });
    }
    return null;
}

// =========================================================================
// ROUTE MAPPINGS
// =========================================================================

app.MapGet("/api/students/catalog", async (string department, string keyword, string instructor, IStudentService studentService) =>
{
    var courses = await studentService.BrowseCatalogAsync(department, keyword, instructor);
    return Results.Ok(courses);
});

app.MapGet("/api/students/courses/{courseId}", async (string courseId, IStudentService studentService) =>
{
    var course = await studentService.GetCourseDetailsAsync(courseId);
    return course != null ? Results.Ok(course) : Results.NotFound("Course not found.");
});

app.MapGet("/api/students/{id}/schedule", async (string id, IStudentService studentService) =>
{
    var schedule = await studentService.GetStudentScheduleAsync(id);
    return Results.Ok(schedule);
});

app.MapGet("/api/students/{id}/history", async (string id, IStudentService studentService) =>
{
    var history = await studentService.GetAcademicHistoryAsync(id);
    return Results.Ok(history);
});

app.MapGet("/api/students/{id}/degree-audit", async (string id, string programId, IStudentService studentService) =>
{
    if (string.IsNullOrWhiteSpace(programId)) return Results.BadRequest("ProgramId query parameter is required.");
    var audit = await studentService.GetDegreeAuditAsync(id, programId);
    return Results.Ok(audit);
});

app.MapPost("/api/students/enroll", async (EnrollCourseDto dto, IStudentService studentService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    var result = await studentService.EnrollInCourseAsync(dto.StudentId, dto.CourseId);
    return result.Success ? Results.Ok(new { message = result.Message }) : Results.BadRequest(new { error = result.Message });
});

app.MapPost("/api/students/drop", async (DropCourseDto dto, IStudentService studentService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    var result = await studentService.DropCourseAsync(dto.StudentId, dto.CourseId);
    return result.Success ? Results.Ok(new { message = result.Message }) : Results.BadRequest(new { error = result.Message });
});

app.MapPost("/api/students/waitlist", async (JoinWaitlistDto dto, IStudentService studentService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    var result = await studentService.JoinWaitlistAsync(dto.StudentId, dto.CourseId);
    return result.Success ? Results.Ok(new { message = result.Message }) : Results.BadRequest(new { error = result.Message });
});

// Extra helper endpoints to verify SQLite state in testing
app.MapGet("/api/students/all", async (StudentDbContext db) =>
{
    var students = await db.Students.Include(s => s.AcademicHistory).ToListAsync();
    return Results.Ok(students);
});

app.Run();

// =========================================================================
// GLOBAL EXCEPTION HANDLER
// =========================================================================
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception in StudentService: {Message}", exception.Message);

        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An internal server error occurred.";

        if (exception is InvalidOperationException || exception is ArgumentException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = exception.Message;
        }
        else if (exception is KeyNotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
            message = exception.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            statusCode = HttpStatusCode.Forbidden;
            message = exception.Message;
        }

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = new { error = message };
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
