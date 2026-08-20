using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusEnroll.Shared;

namespace NexusEnroll.StudentService
{
    public interface IStudentService
    {
        Task<IEnumerable<CourseResponseDto>> BrowseCatalogAsync(string department = null, string keyword = null, string instructorName = null);
        Task<CourseResponseDto> GetCourseDetailsAsync(string courseId);
        Task<IEnumerable<CourseResponseDto>> GetStudentScheduleAsync(string studentId);
        Task<IEnumerable<CourseRecordResponseDto>> GetAcademicHistoryAsync(string studentId);
        Task<IEnumerable<string>> GetDegreeAuditAsync(string studentId, string programId);
        
        Task<(bool Success, string Message)> EnrollInCourseAsync(string studentId, string courseId);
        Task<(bool Success, string Message)> DropCourseAsync(string studentId, string courseId);
        Task<(bool Success, string Message)> JoinWaitlistAsync(string studentId, string courseId);
    }

    public class StudentService : IStudentService
    {
        private readonly StudentDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            StudentDbContext dbContext,
            IEventPublisher eventPublisher,
            ILogger<StudentService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<CourseResponseDto>> BrowseCatalogAsync(string department = null, string keyword = null, string instructorName = null)
        {
            var query = _dbContext.Courses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(c => c.Department != null && c.Department.ToLower() == department.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(instructorName))
            {
                query = query.Where(c => c.InstructorName != null && c.InstructorName.ToLower().Contains(instructorName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(c =>
                    (c.CourseCode != null && c.CourseCode.ToLower().Contains(keyword.ToLower())) ||
                    (c.CourseName != null && c.CourseName.ToLower().Contains(keyword.ToLower())) ||
                    (c.Description != null && c.Description.ToLower().Contains(keyword.ToLower())));
            }

            var courses = await query.ToListAsync();
            return courses.Select(MapToCourseDto);
        }

        public async Task<CourseResponseDto> GetCourseDetailsAsync(string courseId)
        {
            var course = await _dbContext.Courses.FindAsync(courseId);
            return course == null ? null : MapToCourseDto(course);
        }

        public async Task<IEnumerable<CourseResponseDto>> GetStudentScheduleAsync(string studentId)
        {
            var student = await _dbContext.Students.FindAsync(studentId);
            if (student == null) return Enumerable.Empty<CourseResponseDto>();

            var courses = await _dbContext.Courses
                .Where(c => student.EnrolledCourseIds.Contains(c.CourseId))
                .ToListAsync();

            return courses.Select(MapToCourseDto);
        }

        public async Task<IEnumerable<CourseRecordResponseDto>> GetAcademicHistoryAsync(string studentId)
        {
            var records = await _dbContext.CourseRecords
                .Where(cr => cr.StudentId == studentId)
                .OrderByDescending(cr => cr.CompletedAt)
                .ToListAsync();

            return records.Select(cr => new CourseRecordResponseDto
            {
                CourseId = cr.CourseId,
                CourseCode = cr.CourseCode,
                CourseName = cr.CourseName,
                Semester = cr.Semester,
                Grade = cr.Grade.ToString(),
                Credits = cr.Credits,
                ResultDisplay = cr.IsPassing ? "Pass" : "Fail",
                CompletedAt = cr.CompletedAt
            });
        }

        public async Task<IEnumerable<string>> GetDegreeAuditAsync(string studentId, string programId)
        {
            var student = await _dbContext.Students
                .Include(s => s.AcademicHistory)
                .FirstOrDefaultAsync(s => s.UserId == studentId);
            if (student == null) return Enumerable.Empty<string>();

            var program = await _dbContext.DegreePrograms.FindAsync(programId);
            if (program == null) return Enumerable.Empty<string>();

            var completedCourseIds = new HashSet<string>(
                student.AcademicHistory
                    .Where(r => r.IsPassing)
                    .Select(r => r.CourseId)
            );

            return program.RequiredCourseIds.Where(reqId => !completedCourseIds.Contains(reqId)).ToList();
        }

        public async Task<(bool Success, string Message)> EnrollInCourseAsync(string studentId, string courseId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var student = await _dbContext.Students
                    .Include(s => s.AcademicHistory)
                    .FirstOrDefaultAsync(s => s.UserId == studentId);
                if (student == null)
                    return (false, $"Enrollment failed: Student '{studentId}' not found.");

                if (!student.IsActive)
                    return (false, $"Enrollment failed: Student '{student.FullName}' account is inactive.");

                var course = await _dbContext.Courses.FindAsync(courseId);
                if (course == null)
                    return (false, $"Enrollment failed: Course '{courseId}' not found.");

                if (student.EnrolledCourseIds.Contains(courseId))
                    return (false, $"Student is already enrolled in {course.CourseCode}.");

                if (!student.HasCompletedPrerequisites(course.PrerequisiteCourseIds))
                {
                    var missingPrereqs = course.PrerequisiteCourseIds
                        .Where(id => !student.AcademicHistory.Any(h => h.CourseId == id && h.IsPassing))
                        .ToList();
                    return (false, $"Enrollment failed: Student has not completed required prerequisite course(s): {string.Join(", ", missingPrereqs)} for {course.CourseCode}.");
                }

                var enrolledCourses = await _dbContext.Courses
                    .Where(c => student.EnrolledCourseIds.Contains(c.CourseId))
                    .ToListAsync();
                foreach (var enrolled in enrolledCourses)
                {
                    if (course.HasTimeConflictWith(enrolled))
                    {
                        return (false, $"Enrollment failed: Schedule conflict detected between {course.CourseCode} ({course.Days} {course.StartTime}-{course.EndTime}) and enrolled course {enrolled.CourseCode} ({enrolled.Days} {enrolled.StartTime}-{enrolled.EndTime}).");
                    }
                }

                if (!course.HasAvailableSeats())
                {
                    return (false, $"Enrollment failed: Course {course.CourseCode} is at full capacity ({course.Capacity}/{course.Capacity}). You can join the waitlist.");
                }

                course.Enroll();
                student.EnrollInCourse(course.CourseId);

                if (course.IsWaitlisted(student.UserId))
                {
                    course.RemoveFromWaitlist(student.UserId);
                    student.WaitlistedCourseIds.Remove(course.CourseId);
                }

                var enrollment = new Enrollment
                {
                    EnrollmentId = Guid.NewGuid().ToString(),
                    StudentId = student.UserId,
                    CourseId = course.CourseId,
                    Status = EnrollmentStatus.Enrolled
                };
                _dbContext.Enrollments.Add(enrollment);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Enrolled student {StudentId} in course {CourseId}", studentId, courseId);

                await _eventPublisher.PublishAsync(new StudentEnrolledEvent
                {
                    StudentId = student.UserId,
                    StudentName = student.FullName,
                    RecipientEmail = student.Email,
                    RecipientPhone = student.Phone,
                    CourseId = course.CourseId,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    Message = $"Successfully enrolled in {course.CourseCode} - {course.CourseName}."
                }, "student.enrolled");

                return (true, $"Successfully enrolled {student.FullName} in {course.CourseCode} - {course.CourseName}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction rollback during student enrollment. StudentId: {StudentId}, CourseId: {CourseId}", studentId, courseId);
                return (false, $"Enrollment failed due to an internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DropCourseAsync(string studentId, string courseId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var student = await _dbContext.Students.FindAsync(studentId);
                if (student == null)
                    return (false, $"Drop failed: Student '{studentId}' not found.");

                var course = await _dbContext.Courses.FindAsync(courseId);
                if (course == null)
                    return (false, $"Drop failed: Course '{courseId}' not found.");

                if (!student.EnrolledCourseIds.Contains(courseId))
                    return (false, $"Student is not enrolled in {course.CourseCode}.");

                course.Drop();
                student.DropCourse(courseId);

                var enrollment = await _dbContext.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.Status == EnrollmentStatus.Enrolled);
                if (enrollment != null)
                {
                    enrollment.Drop();
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Dropped student {StudentId} from course {CourseId}", studentId, courseId);

                await _eventPublisher.PublishAsync(new StudentDroppedEvent
                {
                    StudentId = student.UserId,
                    CourseId = course.CourseId,
                    RecipientEmail = student.Email,
                    RecipientPhone = student.Phone,
                    Message = $"You dropped course {course.CourseCode}."
                }, "student.dropped");

                string promotionMessage = "";

                if (course.Waitlist.Count > 0)
                {
                    string nextStudentId = course.PopNextWaitlistedStudent();
                    var promotedStudent = await _dbContext.Students.FindAsync(nextStudentId);

                    if (promotedStudent != null)
                    {
                        promotedStudent.WaitlistedCourseIds.Remove(course.CourseId);

                        course.Enroll();
                        promotedStudent.EnrollInCourse(course.CourseId);

                        var promotedEnrollment = new Enrollment
                        {
                            EnrollmentId = Guid.NewGuid().ToString(),
                            StudentId = promotedStudent.UserId,
                            CourseId = course.CourseId,
                            Status = EnrollmentStatus.Enrolled
                        };
                        _dbContext.Enrollments.Add(promotedEnrollment);

                        await _dbContext.SaveChangesAsync();

                        _logger.LogInformation("Waitlist auto-promoted student {StudentId} into course {CourseId}", promotedStudent.UserId, course.CourseId);

                        await _eventPublisher.PublishAsync(new StudentEnrolledEvent
                        {
                            StudentId = promotedStudent.UserId,
                            StudentName = promotedStudent.FullName,
                            RecipientEmail = promotedStudent.Email,
                            RecipientPhone = promotedStudent.Phone,
                            CourseId = course.CourseId,
                            CourseCode = course.CourseCode,
                            CourseName = course.CourseName,
                            Message = $"Successfully enrolled from waitlist in {course.CourseCode}."
                        }, "student.enrolled");

                        await _eventPublisher.PublishAsync(new WaitlistPromotedEvent
                        {
                            StudentId = promotedStudent.UserId,
                            CourseId = course.CourseId,
                            RecipientEmail = promotedStudent.Email,
                            RecipientPhone = promotedStudent.Phone,
                            Message = $"A seat opened in {course.CourseCode}! You have been promoted from waitlist to enrolled."
                        }, "waitlist.promoted");

                        promotionMessage = $" Auto-promoted waitlisted student: {promotedStudent.FullName}.";
                    }
                }

                await transaction.CommitAsync();
                return (true, $"Successfully dropped course {course.CourseCode}.{promotionMessage}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction rollback during student course drop. StudentId: {StudentId}, CourseId: {CourseId}", studentId, courseId);
                return (false, $"Drop failed due to an internal server error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> JoinWaitlistAsync(string studentId, string courseId)
        {
            var student = await _dbContext.Students.FindAsync(studentId);
            if (student == null) return (false, $"Student '{studentId}' not found.");

            var course = await _dbContext.Courses.FindAsync(courseId);
            if (course == null) return (false, $"Course '{courseId}' not found.");

            if (student.EnrolledCourseIds.Contains(courseId))
                return (false, $"Already enrolled in {course.CourseCode}.");

            if (course.HasAvailableSeats())
                return (false, $"Seats are still available in {course.CourseCode}. You can enroll directly.");

            if (course.IsWaitlisted(student.UserId))
                return (false, $"Already on the waitlist for {course.CourseCode}.");

            course.AddToWaitlist(student.UserId);
            student.WaitlistForCourse(course.CourseId);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Student {StudentId} joined waitlist for course {CourseId}", studentId, courseId);

            await _eventPublisher.PublishAsync(new WaitlistJoinedEvent
            {
                StudentId = student.UserId,
                CourseId = course.CourseId,
                RecipientEmail = student.Email,
                RecipientPhone = student.Phone,
                Position = course.Waitlist.Count
            }, "waitlist.joined");

            return (true, $"Added to waitlist for {course.CourseCode}. Position: #{course.Waitlist.Count}");
        }

        private static CourseResponseDto MapToCourseDto(Course c)
        {
            return new CourseResponseDto
            {
                CourseId = c.CourseId,
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                Description = c.Description,
                Department = c.Department,
                InstructorId = c.InstructorId,
                InstructorName = c.InstructorName,
                Credits = c.Credits,
                Semester = c.Semester,
                Capacity = c.Capacity,
                EnrolledCount = c.EnrolledCount,
                Days = c.Days,
                StartTime = c.StartTime.ToString(@"hh\:mm"),
                EndTime = c.EndTime.ToString(@"hh\:mm"),
                Location = c.Location,
                Status = c.Status.ToString(),
                AvailableSeats = c.AvailableSeats,
                WaitlistCount = c.Waitlist.Count
            };
        }
    }
}
