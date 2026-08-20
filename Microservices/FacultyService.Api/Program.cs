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
using NexusEnroll.FacultyService;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5020");

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure Database (SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Data Source=FacultyDb.db";
builder.Services.AddDbContext<FacultyDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Services & Messaging
builder.Services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();
builder.Services.AddScoped<IFacultyService, NexusEnroll.FacultyService.FacultyService>();
builder.Services.AddHostedService<RabbitMQEventConsumer>();

// Configure JSON Options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Register Global Exception Handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

// Ensure Database is Created & Seeded
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FacultyDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Seed initial faculties, courses, and rosters if empty
    if (!dbContext.Faculties.Any())
    {
        // 1. Seed Faculty Members
        var f1 = new Faculty
        {
            UserId = "FAC001",
            FullName = "Dr. Alan Turing",
            Email = "alan.turing@nexus.edu",
            Phone = "555-0201",
            EmployeeNumber = "F2001",
            Department = "Computer Science",
            Rank = "Professor",
            IsActive = true,
            TeachingCourseIds = new List<string> { "CS101", "CS201", "SE101" }
        };
        dbContext.Faculties.Add(f1);

        var f2 = new Faculty
        {
            UserId = "FAC002",
            FullName = "Prof. Grace Hopper",
            Email = "grace.hopper@nexus.edu",
            Phone = "555-0202",
            EmployeeNumber = "F2002",
            Department = "Computer Science",
            Rank = "Associate Professor",
            IsActive = true,
            TeachingCourseIds = new List<string> { "CS301", "CS302" }
        };
        dbContext.Faculties.Add(f2);

        // 2. Seed Course replicas (instructors assigned)
        dbContext.Courses.Add(new Course
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
            EnrolledCount = 2,
            Days = "MWF",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Location = "Hall A",
            Status = CourseStatus.Open
        });

        dbContext.Courses.Add(new Course
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
            EnrolledCount = 1,
            Days = "MWF",
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            Location = "Hall B",
            Status = CourseStatus.Open
        });

        dbContext.Courses.Add(new Course
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
            EnrolledCount = 1,
            Days = "TTh",
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11.5),
            Location = "Lab 3",
            Status = CourseStatus.Open
        });

        // 3. Seed Class Rosters (students enrolled in courses)
        // Roster for CS101 (2 students: STU002 - Jane Smith, STU004 - Charlie Brown)
        dbContext.RosterEntries.Add(new RosterEntry
        {
            CourseId = "CS101",
            StudentId = "STU002",
            StudentName = "Jane Smith",
            StudentNumber = "S1002"
        });
        dbContext.RosterEntries.Add(new RosterEntry
        {
            CourseId = "CS101",
            StudentId = "STU004",
            StudentName = "Charlie Brown",
            StudentNumber = "S1004"
        });

        // Roster for CS201 (1 student: STU001 - John Doe)
        dbContext.RosterEntries.Add(new RosterEntry
        {
            CourseId = "CS201",
            StudentId = "STU001",
            StudentName = "John Doe",
            StudentNumber = "S1001"
        });

        // Roster for CS301 (1 student: STU003 - Bob Johnson)
        dbContext.RosterEntries.Add(new RosterEntry
        {
            CourseId = "CS301",
            StudentId = "STU003",
            StudentName = "Bob Johnson",
            StudentNumber = "S1003"
        });

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

app.MapGet("/api/faculty/{id}/schedule", async (string id, IFacultyService facultyService) =>
{
    var schedule = await facultyService.GetTeachingScheduleAsync(id);
    return Results.Ok(schedule);
});

app.MapGet("/api/faculty/{id}/courses/{courseId}/roster", async (string id, string courseId, IFacultyService facultyService) =>
{
    var roster = await facultyService.GetClassRosterAsync(id, courseId);
    return Results.Ok(roster);
});

app.MapPost("/api/faculty/grades/submit", async (SubmitGradesDto dto, IFacultyService facultyService) =>
{
    var val = ValidateDto(dto);
    if (val != null) return val;

    var result = await facultyService.SubmitGradesAsync(dto.FacultyId, dto);
    return result.Success ? Results.Ok(new { message = result.Message }) : Results.BadRequest(new { error = result.Message });
});

app.MapPost("/api/faculty/course-change-request", async (CreateCourseChangeRequestDto dto, IFacultyService facultyService, ILogger<Program> logger) =>
{
    try
    {
        var val = ValidateDto(dto);
        if (val != null) return val;

        var result = await facultyService.SubmitCourseChangeRequestAsync(dto.FacultyId, dto);
        return result.Success 
            ? Results.Ok(new { message = result.Message, requestId = result.RequestId }) 
            : Results.BadRequest(new { error = result.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in /api/faculty/course-change-request");
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/faculty/{id}/change-requests", async (string id, IFacultyService facultyService) =>
{
    var history = await facultyService.GetChangeRequestHistoryAsync(id);
    return Results.Ok(history);
});

// Extra helper endpoints to verify SQLite state in testing
app.MapGet("/api/faculty/all", async (FacultyDbContext db) =>
{
    var faculties = await db.Faculties.ToListAsync();
    return Results.Ok(faculties);
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
        _logger.LogError(exception, "Unhandled exception in FacultyService: {Message}", exception.Message);

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
