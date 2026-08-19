using System;
using System.Collections.Generic;
using System.Linq;
using NexusEnroll.Models;

namespace NexusEnroll.Services
{

    public interface IAdminService
    {
        // Course & Program Management
        Course CreateCourse(Course course);
        bool UpdateCourse(string courseId, Action<Course> updateAction);
        bool DeleteCourse(string courseId);
        DegreeProgram CreateDegreeProgram(DegreeProgram program);

        // Student & Faculty Management
        bool DeactivateUser(string userId);
        bool ActivateUser(string userId);
        bool ForceEnroll(string studentId, string courseId);

        // Faculty course-change requests (approval workflow)
        IEnumerable<CourseChangeRequest> GetPendingChangeRequests();
        bool ApproveChangeRequest(string requestId, string adminId);
        bool RejectChangeRequest(string requestId, string adminId, string reason = null);

        // Reporting & Analytics (UC4)
        EnrollmentReport GenerateEnrollmentReport(string department, double utilizationThresholdPercent = 90.0);
    }

    /// <summary>Per-course utilization inside an EnrollmentReport.</summary>
    public class CourseUtilization
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public int Enrolled { get; set; }
        public int Capacity { get; set; }
        public double UtilizationPercent { get; set; }
    }


    public class EnrollmentReport
    {
        public DateTime GeneratedAt { get; set; }
        public string Department { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public double UtilizationThresholdPercent { get; set; }
        public List<CourseUtilization> CoursesOverThreshold { get; set; } = new List<CourseUtilization>();

        public override string ToString()
        {
            var lines = new List<string>
            {
                $"Enrollment Report - {Department} (generated {GeneratedAt:u})",
                $"  Total courses      : {TotalCourses}",
                $"  Total enrollments  : {TotalEnrollments}",
                $"  Courses >= {UtilizationThresholdPercent}% capacity:"
            };
            foreach (var c in CoursesOverThreshold)
                lines.Add($"    - {c.CourseName} ({c.CourseId}): {c.Enrolled}/{c.Capacity} = {c.UtilizationPercent}%");

            return string.Join(Environment.NewLine, lines);
        }
    }

    public class AdminService : IAdminService
    {
        private readonly IDictionary<string, Course> _courses;
        private readonly IDictionary<string, User> _users;
        private readonly IDictionary<string, DegreeProgram> _programs;
        private readonly IList<CourseChangeRequest> _changeRequests;
        private readonly INotificationService _notificationService;

        public AdminService(
            IDictionary<string, Course> courseRepository,
            IDictionary<string, User> userRepository,
            IList<CourseChangeRequest> changeRequests,
            INotificationService notificationService)
        {
            _courses = courseRepository ?? throw new ArgumentNullException(nameof(courseRepository));
            _users = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _changeRequests = changeRequests ?? new List<CourseChangeRequest>();
            _programs = new Dictionary<string, DegreeProgram>();
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        // ---- Course & Program Management ---------------------------------

        public Course CreateCourse(Course course)
        {
            if (course == null) throw new ArgumentNullException(nameof(course));
            if (_courses.ContainsKey(course.CourseId))
                throw new InvalidOperationException($"Course '{course.CourseId}' already exists.");

            _courses[course.CourseId] = course;

            _notificationService.NotifyEvent("CourseCreated", new Dictionary<string, object>
            {
                { "CourseId", course.CourseId },
                { "CourseName", course.CourseName }
            });

            return course;
        }

        public bool UpdateCourse(string courseId, Action<Course> updateAction)
        {
            if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));
            if (!_courses.TryGetValue(courseId, out var course)) return false;

            updateAction(course);

            _notificationService.NotifyEvent("CourseUpdated", new Dictionary<string, object>
            {
                { "CourseId", courseId }
            });
            return true;
        }

        public bool DeleteCourse(string courseId)
        {
            var removed = _courses.Remove(courseId);
            if (removed)
            {
                _notificationService.NotifyEvent("CourseDeleted", new Dictionary<string, object>
                {
                    { "CourseId", courseId }
                });
            }
            return removed;
        }

        public DegreeProgram CreateDegreeProgram(DegreeProgram program)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));
            _programs[program.ProgramId] = program;
            return program;
        }

        // ---- Student & Faculty Management ---------------------------------

        public bool DeactivateUser(string userId)
        {
            if (!_users.TryGetValue(userId, out var user)) return false;
            user.Deactivate();

            _notificationService.NotifyEvent("UserDeactivated", new Dictionary<string, object>
            {
                { "UserId", userId }, { "Role", user.Role.ToString() }
            });
            return true;
        }

        public bool ActivateUser(string userId)
        {
            if (!_users.TryGetValue(userId, out var user)) return false;
            user.Activate();
            return true;
        }


        public bool ForceEnroll(string studentId, string courseId)
        {
            if (!_users.TryGetValue(studentId, out var u) || !(u is Student student)) return false;
            if (!_courses.TryGetValue(courseId, out var course)) return false;

            course.EnrolledCount++;
            student.EnrollInCourse(courseId);

            _notificationService.NotifyEvent("EnrollmentOverride", new Dictionary<string, object>
            {
                { "StudentId", studentId },
                { "CourseId", courseId },
                { "RecipientEmail", student.Email }
            });
            return true;
        }

        // ---- Faculty course-change request approval -----------------------

        public IEnumerable<CourseChangeRequest> GetPendingChangeRequests()
            => _changeRequests.Where(r => r.Status == ChangeRequestStatus.Pending);

        public bool ApproveChangeRequest(string requestId, string adminId)
        {
            var request = _changeRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null || request.Status != ChangeRequestStatus.Pending) return false;

            if (_courses.TryGetValue(request.CourseId, out var course))
                ApplyFieldChange(course, request.FieldChanged, request.NewValue);

            request.Status = ChangeRequestStatus.Approved;
            request.ReviewedByAdminId = adminId;

            _notificationService.NotifyEvent("CourseChangeApproved", new Dictionary<string, object>
            {
                { "RequestId", requestId }, { "CourseId", request.CourseId }
            });
            return true;
        }

        public bool RejectChangeRequest(string requestId, string adminId, string reason = null)
        {
            var request = _changeRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request == null || request.Status != ChangeRequestStatus.Pending) return false;

            request.Status = ChangeRequestStatus.Rejected;
            request.ReviewedByAdminId = adminId;

            _notificationService.NotifyEvent("CourseChangeRejected", new Dictionary<string, object>
            {
                { "RequestId", requestId }, { "Reason", reason ?? "Not specified" }
            });
            return true;
        }

        private static void ApplyFieldChange(Course course, string field, string newValue)
        {
            switch (field)
            {
                case "Capacity":
                    if (int.TryParse(newValue, out var capacity)) course.Capacity = capacity;
                    break;
                case "Description":
                    course.Description = newValue;
                    break;
                case "CourseName":
                    course.CourseName = newValue;
                    break;
                default:
                    // Unknown field - ignore rather than throw, so one bad
                    // request can't block the approval workflow.
                    break;
            }
        }

        // ---- Reporting & Analytics (UC4) -----------------------------------

        public EnrollmentReport GenerateEnrollmentReport(string department, double utilizationThresholdPercent = 90.0)
        {
            var report = new EnrollmentReport
            {
                GeneratedAt = DateTime.UtcNow,
                Department = department,
                UtilizationThresholdPercent = utilizationThresholdPercent
            };

            var departmentCourses = _courses.Values
                .Where(c => string.Equals(c.Department, department, StringComparison.OrdinalIgnoreCase))
                .ToList();

            report.TotalCourses = departmentCourses.Count;

            foreach (var course in departmentCourses)
            {
                report.TotalEnrollments += course.EnrolledCount;

                double utilization = course.Capacity == 0
                    ? 0
                    : (double)course.EnrolledCount / course.Capacity * 100.0;

                if (utilization >= utilizationThresholdPercent)
                {
                    report.CoursesOverThreshold.Add(new CourseUtilization
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        Enrolled = course.EnrolledCount,
                        Capacity = course.Capacity,
                        UtilizationPercent = Math.Round(utilization, 1)
                    });
                }
            }

            return report;
        }
    }
}
