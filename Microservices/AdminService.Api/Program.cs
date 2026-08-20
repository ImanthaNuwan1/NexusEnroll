using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
using NexusEnroll.AdminService;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5030");

// Add Logging & Logging formats
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure Database (SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Data Source=AdminDb.db";
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Services and Messaging
builder.Services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddHostedService<RabbitMQEventConsumer>();

// Register Global Exception Handler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

// Ensure Database is Created & Migrated (Automated Database Provisioning)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Seed initial Admin if not exists
    if (!dbContext.Admins.Any())
    {
        dbContext.Admins.Add(new Admin
        {
            UserId = "ADM001",
            FullName = "Alice Administrator",
            Email = "alice.admin@nexus.edu",
            Phone = "555-0201",
            StaffNumber = "ADM-001",
            Office = "Dean's Office 201",
            Scope = AdminScope.Full,
            IsActive = true
        });
        dbContext.Admins.Add(new Admin
        {
            UserId = "ADM002",
            FullName = "Bob Registrar",
            Email = "bob.clerk@nexus.edu",
            Phone = "555-0202",
            StaffNumber = "ADM-002",
            Office = "Registrar 105",
            Scope = AdminScope.CourseManagement,
            IsActive = true
        });
        dbContext.SaveChanges();
    }

    if (!dbContext.FacultyProfiles.Any())
    {
        dbContext.FacultyProfiles.Add(new FacultyProfile
        {
            UserId = "FAC001",
            FullName = "Dr. Alan Turing",
            Email = "turing@nexus.edu",
            Phone = "555-0101",
            EmployeeNumber = "EMP-001",
            Department = "Computer Science",
            Rank = "Professor",
            IsActive = true,
            TeachingCourseIds = new List<string> { "CS101", "CS201", "SE101" }
        });
        dbContext.FacultyProfiles.Add(new FacultyProfile
        {
            UserId = "FAC002",
            FullName = "Prof. Grace Hopper",
            Email = "hopper@nexus.edu",
            Phone = "555-0102",
            EmployeeNumber = "EMP-002",
            Department = "Computer Science",
            Rank = "Associate Professor",
            IsActive = true,
            TeachingCourseIds = new List<string> { "CS301", "CS302" }
        });
        dbContext.SaveChanges();
    }

    if (!dbContext.StudentProfiles.Any())
    {
        dbContext.StudentProfiles.Add(new StudentProfile
        {
            UserId = "STU001",
            FullName = "John Doe",
            Email = "john.doe@nexus.edu",
            Phone = "555-0301",
            StudentNumber = "S1001",
            ProgramId = "BS-CS",
            EnrolledYear = 2023,
            IsActive = true
        });
        dbContext.StudentProfiles.Add(new StudentProfile
        {
            UserId = "STU002",
            FullName = "Jane Smith",
            Email = "jane.smith@nexus.edu",
            Phone = "555-0302",
            StudentNumber = "S1002",
            ProgramId = "BS-CS",
            EnrolledYear = 2026,
            IsActive = true
        });
        dbContext.StudentProfiles.Add(new StudentProfile
        {
            UserId = "STU003",
            FullName = "Bob Johnson",
            Email = "bob.johnson@nexus.edu",
            Phone = "555-0303",
            StudentNumber = "S1003",
            ProgramId = "BS-CS",
            EnrolledYear = 2024,
            IsActive = true
        });
        dbContext.StudentProfiles.Add(new StudentProfile
        {
            UserId = "STU004",
            FullName = "Charlie Brown",
            Email = "charlie.b@nexus.edu",
            Phone = "555-0304",
            StudentNumber = "S1004",
            ProgramId = "BS-CS",
            EnrolledYear = 2025,
            IsActive = true
        });
        dbContext.SaveChanges();
    }

    if (!dbContext.Courses.Any())
    {
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
            EnrolledCount = 2,
            Days = "MWF",
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            Location = "Hall B",
            Status = CourseStatus.Open
        });

        dbContext.Courses.Add(new Course
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
            Status = CourseStatus.Open
        });

        dbContext.Courses.Add(new Course
        {
            CourseId = "CS301",
            CourseCode = "CS 301",
            CourseName = "Database Systems",
            Description = "Relational algebra, SQL, indexing, and transaction management.",
            Department = "Computer Science",
            InstructorId = "FAC002",
            InstructorName = "Prof. Grace Hopper",
            Credits = 3,
            Semester = "Fall 2026",
            Capacity = 3,
            EnrolledCount = 1,
            Days = "TTh",
            StartTime = TimeSpan.FromHours(11),
            EndTime = TimeSpan.FromHours(12.5),
            Location = "Lab 1",
            Status = CourseStatus.Open
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

// --- READ Endpoints (Convenience for GUI client) ---
app.MapGet("/api/admin/users", async (AdminDbContext dbContext, ILogger<Program> logger) =>
{
    try
    {
        var students = await dbContext.StudentProfiles.ToListAsync();
        var faculty = await dbContext.FacultyProfiles.ToListAsync();
        var admins = await dbContext.Admins.ToListAsync();

        return Results.Ok(new
        {
            Students = students,
            Faculty = faculty,
            Admins = admins
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching users in /api/admin/users");
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/admin/courses", async (AdminDbContext dbContext) =>
{
    var courses = await dbContext.Courses.ToListAsync();
    return Results.Ok(courses);
});

app.MapGet("/api/admin/programs", async (AdminDbContext dbContext) =>
{
    var programs = await dbContext.DegreePrograms.ToListAsync();
    return Results.Ok(programs);
});

// --- Course & Program Management ---
app.MapPost("/api/admin/courses", async (CreateCourseDto dto, IAdminService adminService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    if (!TimeSpan.TryParse(dto.StartTime, out var startTime) || !TimeSpan.TryParse(dto.EndTime, out var endTime))
    {
        return Results.BadRequest(new { error = "StartTime and EndTime must be valid TimeSpan formats (e.g. 09:00:00)." });
    }

    var course = new Course
    {
        CourseId = dto.CourseId,
        CourseCode = dto.CourseCode,
        CourseName = dto.CourseName,
        Description = dto.Description,
        Department = dto.Department,
        InstructorId = dto.InstructorId,
        InstructorName = dto.InstructorName,
        Credits = dto.Credits,
        Semester = dto.Semester,
        Capacity = dto.Capacity,
        EnrolledCount = 0,
        Days = dto.Days,
        StartTime = startTime,
        EndTime = endTime,
        Location = dto.Location,
        PrerequisiteCourseIds = dto.PrerequisiteCourseIds
    };

    var created = await adminService.CreateCourseAsync(course);
    return Results.Created($"/api/admin/courses/{created.CourseId}", created);
});

app.MapPut("/api/admin/courses/{id}", async (string id, CreateCourseDto dto, IAdminService adminService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    if (!TimeSpan.TryParse(dto.StartTime, out var startTime) || !TimeSpan.TryParse(dto.EndTime, out var endTime))
    {
        return Results.BadRequest(new { error = "StartTime and EndTime must be valid TimeSpan formats." });
    }

    var updated = await adminService.UpdateCourseAsync(id, c =>
    {
        c.CourseCode = dto.CourseCode;
        c.CourseName = dto.CourseName;
        c.Description = dto.Description;
        c.Department = dto.Department;
        c.InstructorId = dto.InstructorId;
        c.InstructorName = dto.InstructorName;
        c.Credits = dto.Credits;
        c.Semester = dto.Semester;
        c.Capacity = dto.Capacity;
        c.Days = dto.Days;
        c.StartTime = startTime;
        c.EndTime = endTime;
        c.Location = dto.Location;
        c.PrerequisiteCourseIds = dto.PrerequisiteCourseIds;
    });

    return updated ? Results.Ok(new { message = "Course updated successfully." }) : Results.NotFound();
});

app.MapDelete("/api/admin/courses/{id}", async (string id, IAdminService adminService) =>
{
    var deleted = await adminService.DeleteCourseAsync(id);
    return deleted ? Results.Ok(new { message = "Course deleted successfully." }) : Results.NotFound();
});

app.MapPost("/api/admin/programs", async (CreateDegreeProgramDto dto, IAdminService adminService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    var program = new DegreeProgram
    {
        ProgramId = dto.ProgramId,
        ProgramName = dto.ProgramName,
        Department = dto.Department,
        RequiredCourseIds = dto.RequiredCourseIds,
        ElectiveCourseIds = dto.ElectiveCourseIds
    };

    var created = await adminService.CreateDegreeProgramAsync(program);
    return Results.Created($"/api/admin/programs/{created.ProgramId}", created);
});

// --- User Account Management ---
app.MapPost("/api/admin/students", async (CreateStudentDto dto, IAdminService adminService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    var student = new StudentProfile
    {
        UserId = dto.UserId,
        FullName = dto.FullName,
        Email = dto.Email,
        Phone = dto.Phone,
        StudentNumber = dto.StudentNumber,
        ProgramId = dto.ProgramId,
        EnrolledYear = dto.EnrolledYear,
        IsActive = true
    };

    var created = await adminService.CreateStudentAccountAsync(student);
    return Results.Created($"/api/admin/students/{created.UserId}", created);
});

app.MapPost("/api/admin/faculty", async (CreateFacultyDto dto, IAdminService adminService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    var faculty = new FacultyProfile
    {
        UserId = dto.UserId,
        FullName = dto.FullName,
        Email = dto.Email,
        Phone = dto.Phone,
        EmployeeNumber = dto.EmployeeNumber,
        Department = dto.Department,
        Rank = dto.Rank,
        IsActive = true
    };

    var created = await adminService.CreateFacultyAccountAsync(faculty, dto.AssignedCourseIds);
    return Results.Created($"/api/admin/faculty/{created.UserId}", created);
});

app.MapPost("/api/admin/admins", async (CreateAdminDto dto, AdminDbContext dbContext) =>
{
    var val = ValidateDto(dto);
    if (val != null) return val;

    var exists = await dbContext.Admins.AnyAsync(a => a.UserId == dto.UserId);
    if (exists) return Results.BadRequest(new { error = "Admin user already exists." });

    var admin = new Admin
    {
        UserId = dto.UserId,
        FullName = dto.FullName,
        Email = dto.Email,
        Phone = dto.Phone,
        StaffNumber = dto.StaffNumber,
        Office = dto.Office,
        Scope = dto.Scope,
        IsActive = true
    };

    dbContext.Admins.Add(admin);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/admin/admins/{admin.UserId}", admin);
});

app.MapPost("/api/admin/users/{id}/deactivate", async (string id, IAdminService adminService) =>
{
    var ok = await adminService.DeactivateUserAsync(id);
    return ok ? Results.Ok(new { message = "User account deactivated." }) : Results.NotFound();
});

app.MapPost("/api/admin/users/{id}/activate", async (string id, IAdminService adminService) =>
{
    var ok = await adminService.ActivateUserAsync(id);
    return ok ? Results.Ok(new { message = "User account activated." }) : Results.NotFound();
});

app.MapDelete("/api/admin/students/{id}", async (string id, IAdminService adminService) =>
{
    var ok = await adminService.DeleteStudentAccountAsync(id);
    return ok ? Results.Ok(new { message = "Student account deleted." }) : Results.NotFound();
});

app.MapDelete("/api/admin/faculty/{id}", async (string id, IAdminService adminService) =>
{
    var ok = await adminService.DeleteFacultyAccountAsync(id);
    return ok ? Results.Ok(new { message = "Faculty account deleted." }) : Results.NotFound();
});

app.MapPost("/api/admin/enroll-override", async (ForceEnrollDto dto, IAdminService adminService) =>
{
    var validationResult = ValidateDto(dto);
    if (validationResult != null) return validationResult;

    var ok = await adminService.ForceEnrollAsync(dto.StudentId, dto.CourseId);
    return ok ? Results.Ok(new { message = "Student successfully enrolled by administrative override." }) : Results.BadRequest("Override failed.");
});

// --- Faculty Change Requests Workflow ---
app.MapGet("/api/admin/change-requests/pending", async (IAdminService adminService) =>
{
    var pending = await adminService.GetPendingChangeRequestsAsync();
    return Results.Ok(pending);
});

app.MapPost("/api/admin/change-requests/{id}/approve", async (string id, string adminId, IAdminService adminService) =>
{
    if (string.IsNullOrEmpty(adminId)) return Results.BadRequest("AdminId is required.");
    var ok = await adminService.ApproveChangeRequestAsync(id, adminId);
    return ok ? Results.Ok(new { message = $"Request {id} approved." }) : Results.BadRequest("Approval failed.");
});

app.MapPost("/api/admin/change-requests/{id}/reject", async (string id, string adminId, string reason, IAdminService adminService) =>
{
    if (string.IsNullOrEmpty(adminId)) return Results.BadRequest("AdminId is required.");
    var ok = await adminService.RejectChangeRequestAsync(id, adminId, reason);
    return ok ? Results.Ok(new { message = $"Request {id} rejected." }) : Results.BadRequest("Rejection failed.");
});

// --- Reports & Analytics ---
app.MapGet("/api/admin/reports/utilization", async (string department, double? threshold, IAdminService adminService) =>
{
    if (string.IsNullOrWhiteSpace(department)) return Results.BadRequest("Department parameter is required.");
    var report = await adminService.GenerateEnrollmentReportAsync(department, threshold ?? 90.0);
    return Results.Ok(report);
});

// --- Finalise Grades Endpoint ---
app.MapPost("/api/admin/courses/{courseId}/approve-grades", async (string courseId, IAdminService adminService, IEventPublisher eventPublisher, AdminDbContext dbContext) =>
{
    var course = await dbContext.Courses.FindAsync(courseId);
    if (course == null) return Results.NotFound("Course not found.");

    course.GradeStatus = GradeSubmissionStatus.Submitted;
    await dbContext.SaveChangesAsync();

    // Broadcast grade approval to notify student service to finalize student records and notify observers
    await eventPublisher.PublishAsync(new GradesApprovedEvent
    {
        CourseId = courseId,
        ApprovedCount = course.EnrolledCount, // Approximate or synced
        Message = $"Grades for course {courseId} approved by Administrator."
    }, "grades.approved");

    return Results.Ok(new { message = $"Grades for course {courseId} approved and finalized." });
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
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

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
