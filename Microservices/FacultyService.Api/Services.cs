using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusEnroll.Shared;

namespace NexusEnroll.FacultyService
{
    // =========================================================================
    // DTOs
    // =========================================================================

    public class SubmitGradesDto
    {
        [Required(ErrorMessage = "CourseId is required.")]
        public string CourseId { get; set; }

        [Required(ErrorMessage = "FacultyId is required.")]
        public string FacultyId { get; set; }

        [Required(ErrorMessage = "Grades dictionary is required.")]
        public Dictionary<string, string> Grades { get; set; }
    }

    public class CreateCourseChangeRequestDto
    {
        [Required(ErrorMessage = "CourseId is required.")]
        public string CourseId { get; set; }

        [Required(ErrorMessage = "FacultyId is required.")]
        public string FacultyId { get; set; }

        [Required(ErrorMessage = "FieldChanged is required (e.g. Capacity, Description).")]
        public string FieldChanged { get; set; }

        [Required(ErrorMessage = "NewValue is required.")]
        public string NewValue { get; set; }
    }

    public class CourseRosterDto
    {
        public string CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string GradeStatus { get; set; }
        public List<RosterStudentDto> Students { get; set; } = new List<RosterStudentDto>();
    }

    public class RosterStudentDto
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public string Grade { get; set; }
    }

    // =========================================================================
    // SERVICE INTERFACE & IMPLEMENTATION
    // =========================================================================

    public interface IFacultyService
    {
        Task<IEnumerable<Course>> GetTeachingScheduleAsync(string facultyId);
        Task<CourseRosterDto> GetClassRosterAsync(string facultyId, string courseId);
        Task<(bool Success, string Message)> SubmitGradesAsync(string facultyId, SubmitGradesDto dto);
        Task<(bool Success, string Message, string RequestId)> SubmitCourseChangeRequestAsync(string facultyId, CreateCourseChangeRequestDto dto);
        Task<IEnumerable<CourseChangeRequest>> GetChangeRequestHistoryAsync(string facultyId);
    }

    public class FacultyService : IFacultyService
    {
        private readonly FacultyDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<FacultyService> _logger;

        public FacultyService(
            FacultyDbContext dbContext,
            IEventPublisher eventPublisher,
            ILogger<FacultyService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Course>> GetTeachingScheduleAsync(string facultyId)
        {
            return await _dbContext.Courses
                .Where(c => c.InstructorId == facultyId)
                .ToListAsync();
        }

        public async Task<CourseRosterDto> GetClassRosterAsync(string facultyId, string courseId)
        {
            var course = await _dbContext.Courses.FindAsync(courseId);
            if (course == null || course.InstructorId != facultyId)
            {
                throw new KeyNotFoundException("Course not found or not assigned to this faculty member.");
            }

            var entries = await _dbContext.RosterEntries
                .Where(re => re.CourseId == courseId)
                .ToListAsync();

            return new CourseRosterDto
            {
                CourseId = course.CourseId,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                GradeStatus = course.GradeStatus.ToString(),
                Students = entries.Select(re => new RosterStudentDto
                {
                    StudentId = re.StudentId,
                    StudentName = re.StudentName,
                    StudentNumber = re.StudentNumber,
                    Grade = re.Grade.HasValue ? re.Grade.Value.ToString() : "Not Graded"
                }).ToList()
            };
        }

        public async Task<(bool Success, string Message)> SubmitGradesAsync(string facultyId, SubmitGradesDto dto)
        {
            var course = await _dbContext.Courses.FindAsync(dto.CourseId);
            if (course == null || course.InstructorId != facultyId)
                return (false, "Grades submission rejected: Faculty is not assigned to this course.");

            if (course.GradeStatus == GradeSubmissionStatus.Submitted)
                return (false, "Grades submission rejected: Grades for this course have already been approved and finalized by admin.");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var entries = await _dbContext.RosterEntries
                    .Where(re => re.CourseId == dto.CourseId)
                    .ToListAsync();

                foreach (var gradeEntry in dto.Grades)
                {
                    var studentId = gradeEntry.Key;
                    var rawGrade = gradeEntry.Value;

                    if (Enum.TryParse<Grade>(rawGrade, true, out var parsedGrade))
                    {
                        var entry = entries.FirstOrDefault(e => e.StudentId == studentId);
                        if (entry != null)
                        {
                            entry.Grade = parsedGrade;
                        }
                    }
                    else
                    {
                        return (false, $"Grades submission rejected: Invalid grade value '{rawGrade}' for student {studentId}.");
                    }
                }

                course.GradeStatus = GradeSubmissionStatus.Pending;
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Faculty {FacultyId} submitted grades for course {CourseId}", facultyId, dto.CourseId);

                // Publish Event
                await _eventPublisher.PublishAsync(new GradesSubmittedEvent
                {
                    CourseId = course.CourseId,
                    FacultyId = facultyId,
                    Grades = dto.Grades
                }, "grades.submitted");

                return (true, "Grades submitted successfully. Pending administrative approval.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction rollback during faculty grades submission. CourseId: {CourseId}", dto.CourseId);
                return (false, $"Grades submission failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, string RequestId)> SubmitCourseChangeRequestAsync(string facultyId, CreateCourseChangeRequestDto dto)
        {
            var course = await _dbContext.Courses.FindAsync(dto.CourseId);
            if (course == null || course.InstructorId != facultyId)
                return (false, "Request failed: Faculty is not assigned to this course.", null);

            string oldValue = "";
            switch (dto.FieldChanged)
            {
                case "Capacity":
                    oldValue = course.Capacity.ToString();
                    break;
                case "Description":
                    oldValue = course.Description ?? "";
                    break;
                case "CourseName":
                    oldValue = course.CourseName ?? "";
                    break;
                default:
                    return (false, $"Request failed: Field '{dto.FieldChanged}' cannot be modified by faculty.", null);
            }

            var requestId = Guid.NewGuid().ToString();
            var request = new CourseChangeRequest
            {
                RequestId = requestId,
                CourseId = dto.CourseId,
                RequestedByFacultyId = facultyId,
                FieldChanged = dto.FieldChanged,
                OldValue = oldValue,
                NewValue = dto.NewValue,
                Status = ChangeRequestStatus.Pending,
                ReviewedByAdminId = "",
                RequestedAt = DateTime.UtcNow
            };

            _dbContext.CourseChangeRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Faculty {FacultyId} submitted change request {RequestId} for course {CourseId}", facultyId, requestId, dto.CourseId);

            // Publish Event
            await _eventPublisher.PublishAsync(new CourseChangeRequestedEvent
            {
                RequestId = requestId,
                CourseId = dto.CourseId,
                FacultyId = facultyId,
                FieldChanged = dto.FieldChanged,
                OldValue = oldValue,
                NewValue = dto.NewValue,
                Timestamp = request.RequestedAt,
                Message = $"Faculty requested to change {dto.FieldChanged} to '{dto.NewValue}' for {course.CourseCode}."
            }, "faculty.changerequested");

            return (true, "Course change request submitted successfully to admin review queue.", requestId);
        }

        public async Task<IEnumerable<CourseChangeRequest>> GetChangeRequestHistoryAsync(string facultyId)
        {
            return await _dbContext.CourseChangeRequests
                .Where(r => r.RequestedByFacultyId == facultyId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }
    }
}
