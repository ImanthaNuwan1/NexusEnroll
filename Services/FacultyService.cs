using System;
using System.Collections.Generic;
using System.Linq;
using NexusEnroll.Models;

namespace NexusEnroll.Services
{
    // ========================================================================
    // SHARED CONTRACT -- NOT owned by the Faculty module.
    //
    // FacultyService needs to raise notifications but must not know how they
    // are delivered (that is Member D's Observer pattern). This is the
    // minimal interface FacultyService depends on, matching the
    // "INotificationService" box on the class diagram.
    //
    // TEMPORARY HOME: this belongs in Services/NotificationService.cs once
    // Member D builds it. It lives here for now only so FacultyService
    // compiles independently. Confirm this shape with Member D before they
    // start -- once they build NotificationService.cs, DELETE this block
    // from this file and let it live only in theirs.
    // ========================================================================
    public interface INotificationService
    {
        void NotifyEvent(string eventType, string recipientId, string message);
    }


    // ========================================================================
    // Result types returned by IFacultyService methods.
    // Bundled here rather than in a separate file, matching how Member A
    // groups related small types together (see Models/Course.cs, which
    // bundles Course, CourseSchedule, CourseRecord, Enrollment,
    // DegreeProgram and CourseChangeRequest in one file).
    // ========================================================================

    /// <summary>One failed grade within a batch submission.</summary>
    public class GradeError
    {
        public string StudentId { get; set; }
        public string RawValue { get; set; }
        public string Reason { get; set; }

        public GradeError(string studentId, string rawValue, string reason)
        {
            StudentId = studentId;
            RawValue = rawValue;
            Reason = reason;
        }

        public override string ToString()
            => $"{StudentId}: '{RawValue}' rejected -- {Reason}";
    }

    /// <summary>
    /// Result of a batch grade submission (SRS 3.2.2 use case). Valid grades
    /// are committed individually; invalid ones are reported here without
    /// blocking the rest of the batch -- this is what satisfies "the system
    /// must handle it gracefully and allow the professor to correct it
    /// without losing other submitted grades."
    /// </summary>
    public class GradeSubmissionResult
    {
        public int TotalSubmitted { get; }
        public int SuccessCount { get; }
        public List<GradeError> Errors { get; }

        public bool AllSucceeded => Errors.Count == 0;
        public bool AnySucceeded => SuccessCount > 0;

        public GradeSubmissionResult(int totalSubmitted, int successCount, List<GradeError> errors)
        {
            TotalSubmitted = totalSubmitted;
            SuccessCount = successCount;
            Errors = errors ?? new List<GradeError>();
        }

        public override string ToString()
            => AllSucceeded
                ? $"All {SuccessCount} grade(s) submitted successfully."
                : $"{SuccessCount}/{TotalSubmitted} grade(s) submitted. {Errors.Count} rejected -- correct and resubmit only those.";
    }

    /// <summary>Result of an administrator approving a course's pending grades.</summary>
    public class GradeApprovalResult
    {
        public bool Success { get; }
        public string Message { get; }
        public int ApprovedCount { get; }

        public GradeApprovalResult(bool success, string message, int approvedCount)
        {
            Success = success;
            Message = message;
            ApprovedCount = approvedCount;
        }
    }


    // ========================================================================
    // FacultyService -- implements the Faculty module's business logic
    // (SRS section 3.2): class roster viewing, batch grade submission with
    // validation, grade approval, and course-change requests.
    //
    // Design notes for the report:
    //  - Realises IFacultyService (Dependency Inversion: UniversityFacade
    //    depends on the interface, not this class).
    //  - Depends on INotificationService (an abstraction) to raise events,
    //    not on any concrete Observer/notifier class -- Programming to an
    //    Interface.
    //  - Holds its own in-memory collections of Faculty/Course/Student/
    //    Enrollment, matching the class diagram ("FacultyService: -faculty,
    //    -courses, -grades"). Cross-service data consistency is wired up
    //    centrally in UniversityFacade during integration.
    //  - Single Responsibility: only Faculty-module logic. Does not create
    //    Faculty objects (Factory Method's job) and does not decide how
    //    notifications are delivered (Observer pattern's job).
    // ========================================================================
    public class FacultyService : IFacultyService
    {
        private readonly Dictionary<string, Faculty> _faculty = new Dictionary<string, Faculty>();
        private readonly Dictionary<string, Course> _courses = new Dictionary<string, Course>();
        private readonly Dictionary<string, Student> _students = new Dictionary<string, Student>();

        // Enrollments keyed by "studentId|courseId" for quick lookup during grading.
        private readonly Dictionary<string, Enrollment> _enrollments = new Dictionary<string, Enrollment>();

        private readonly List<CourseChangeRequest> _changeRequests = new List<CourseChangeRequest>();

        private readonly INotificationService _notificationService;
        private int _requestSequence = 0;

        public FacultyService(INotificationService notificationService)
        {
            _notificationService = notificationService
                ?? throw new ArgumentNullException(nameof(notificationService));
        }

        // ------------------------------------------------------------------
        // Setup / seeding
        // ------------------------------------------------------------------

        public void RegisterFaculty(Faculty faculty)
        {
            if (faculty == null) throw new ArgumentNullException(nameof(faculty));
            _faculty[faculty.UserId] = faculty;
        }

        public void AddCourse(Course course)
        {
            if (course == null) throw new ArgumentNullException(nameof(course));
            _courses[course.CourseId] = course;
        }

        public void AddStudent(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));
            _students[student.UserId] = student;
        }

        public void AddEnrollment(Enrollment enrollment)
        {
            if (enrollment == null) throw new ArgumentNullException(nameof(enrollment));
            _enrollments[EnrollmentKey(enrollment.StudentId, enrollment.CourseId)] = enrollment;
        }

        private static string EnrollmentKey(string studentId, string courseId) => studentId + "|" + courseId;

        // ------------------------------------------------------------------
        // Class Roster Viewing (SRS 3.2.1)
        // "Instructors can view a real-time list of all students currently
        //  enrolled in their courses. The roster should include student
        //  names, IDs, and contact information."
        // ------------------------------------------------------------------

        public List<Student> GetClassRoster(string facultyId, string courseId)
        {
            VerifyFacultyTeachesCourse(facultyId, courseId);

            return _enrollments.Values
                .Where(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => _students.TryGetValue(e.StudentId, out var s) ? s : null)
                .Where(s => s != null)
                .OrderBy(s => s.FullName)
                .ToList();
            // Each Student already carries FullName, UserId, Email, Phone
            // (inherited from User) -- satisfies "names, IDs, and contact
            // information" directly from the existing domain model.
        }

        // ------------------------------------------------------------------
        // Grade Submission (SRS 3.2.2)
        // "The system must have a process for grade approval (a 'Pending'
        //  state before a final 'Submitted' state)... If an error occurs
        //  (e.g. an invalid grade is submitted), the system must handle it
        //  gracefully and allow the professor to correct it without losing
        //  other submitted grades."
        //
        // Each grade in the batch is validated and committed INDEPENDENTLY.
        // A bad value for one student never blocks the others.
        // ------------------------------------------------------------------

        public GradeSubmissionResult SubmitGrades(string facultyId, string courseId,
                                                    Dictionary<string, string> rawGrades)
        {
            VerifyFacultyTeachesCourse(facultyId, courseId);

            if (rawGrades == null || rawGrades.Count == 0)
                return new GradeSubmissionResult(0, 0, new List<GradeError>());

            var errors = new List<GradeError>();
            int successCount = 0;

            foreach (var entry in rawGrades)
            {
                string studentId = entry.Key;
                string rawValue = entry.Value;

                // Rule 1: grade format must parse to a known Grade value.
                if (string.IsNullOrWhiteSpace(rawValue) || !Enum.TryParse<Grade>(rawValue, true, out var grade))
                {
                    errors.Add(new GradeError(studentId, rawValue, "Invalid grade format"));
                    continue; // other grades remain intact -- keep processing the batch
                }

                // Rule 2: student must actually be enrolled in this course.
                string key = EnrollmentKey(studentId, courseId);
                if (!_enrollments.TryGetValue(key, out var enrollment) ||
                    enrollment.Status != EnrollmentStatus.Enrolled)
                {
                    errors.Add(new GradeError(studentId, rawValue, "Student not enrolled in this course"));
                    continue;
                }

                // Rule 3: range -- the Grade enum only defines valid values
                // (A, B, C, D, F, W, I), so a value that parsed successfully
                // is automatically in range. The type system enforces this
                // business rule instead of a hand-written range check.

                enrollment.SubmitGradePending(grade); // SubmissionStatus -> Pending
                successCount++;
            }

            if (successCount > 0)
            {
                _notificationService.NotifyEvent(
                    "grades_submitted",
                    facultyId,
                    $"{successCount} grade(s) for {courseId} submitted and awaiting approval.");
            }

            return new GradeSubmissionResult(rawGrades.Count, successCount, errors);
        }

        // ------------------------------------------------------------------
        // Grade Approval
        // Triggered by an Administrator (via UniversityFacade ->
        // IAdminService -> IFacultyService), executed here because the
        // grade data itself belongs to the Faculty/Course domain.
        // ------------------------------------------------------------------

        public GradeApprovalResult ApproveGrades(string courseId)
        {
            var pending = _enrollments.Values
                .Where(e => e.CourseId == courseId
                         && e.SubmissionStatus == GradeSubmissionStatus.Pending)
                .ToList();

            if (pending.Count == 0)
                return new GradeApprovalResult(false, "No pending grades for this course.", 0);

            foreach (var enrollment in pending)
            {
                enrollment.FinaliseGrade(); // SubmissionStatus -> Submitted, Status -> Completed

                if (_students.TryGetValue(enrollment.StudentId, out var student) &&
                    _courses.TryGetValue(courseId, out var course))
                {
                    student.RecordCompletedCourse(new CourseRecord(
                        course.CourseId, course.CourseCode, course.CourseName,
                        course.Semester, enrollment.Grade.Value, course.Credits));
                }
            }

            _notificationService.NotifyEvent(
                "grades_approved",
                courseId,
                $"{pending.Count} grade(s) for {courseId} approved and finalised.");

            return new GradeApprovalResult(true, "Grades approved.", pending.Count);
        }

        public GradeSubmissionStatus GetGradeStatus(string courseId)
        {
            if (!_courses.TryGetValue(courseId, out var course))
                throw new KeyNotFoundException($"Course '{courseId}' not found.");

            var statuses = _enrollments.Values
                .Where(e => e.CourseId == courseId)
                .Select(e => e.SubmissionStatus)
                .ToList();

            if (statuses.Count == 0) return GradeSubmissionStatus.NotSubmitted;
            if (statuses.All(s => s == GradeSubmissionStatus.Submitted)) return GradeSubmissionStatus.Submitted;
            if (statuses.Any(s => s == GradeSubmissionStatus.Pending)) return GradeSubmissionStatus.Pending;
            return GradeSubmissionStatus.NotSubmitted;
        }

        // ------------------------------------------------------------------
        // Course Information Management (SRS 3.2.3)
        // "Instructors can submit requests to update course descriptions,
        //  add prerequisites, or change course capacity (these requests
        //  must be approved by an administrator)."
        // ------------------------------------------------------------------

        public CourseChangeRequest RequestCourseUpdate(string facultyId, string courseId,
                                                         string fieldChanged, string newValue)
        {
            VerifyFacultyTeachesCourse(facultyId, courseId);
            var course = _courses[courseId];

            string oldValue = fieldChanged switch
            {
                "Description" => course.Description,
                "Capacity" => course.Capacity.ToString(),
                _ => "(unspecified)"
            };

            _requestSequence++;
            var request = new CourseChangeRequest(
                requestId: $"CCR-{_requestSequence:D4}",
                courseId: courseId,
                facultyId: facultyId,
                fieldChanged: fieldChanged,
                oldValue: oldValue,
                newValue: newValue);

            _changeRequests.Add(request);

            _notificationService.NotifyEvent(
                "course_change_requested",
                facultyId,
                $"Change request {request.RequestId} for {courseId} submitted, awaiting admin approval.");

            return request;
            // Approval/rejection is AdminService's responsibility -- this
            // method's job ends at raising the request.
        }

        public List<CourseChangeRequest> GetChangeRequestsFor(string facultyId)
            => _changeRequests.Where(r => r.RequestedByFacultyId == facultyId).ToList();

        // ------------------------------------------------------------------
        // Teaching schedule
        // ------------------------------------------------------------------

        public List<Course> GetTeachingSchedule(string facultyId)
        {
            if (!_faculty.ContainsKey(facultyId))
                throw new KeyNotFoundException($"Faculty '{facultyId}' not found.");

            return _courses.Values
                .Where(c => c.InstructorId == facultyId)
                .OrderBy(c => c.Schedule?.Days)
                .ToList();
        }

        // ------------------------------------------------------------------
        // Internal helpers
        // ------------------------------------------------------------------

        private void VerifyFacultyTeachesCourse(string facultyId, string courseId)
        {
            if (!_faculty.TryGetValue(facultyId, out var faculty))
                throw new KeyNotFoundException($"Faculty '{facultyId}' not found.");

            if (!_courses.ContainsKey(courseId))
                throw new KeyNotFoundException($"Course '{courseId}' not found.");

            if (!faculty.TeachesCourse(courseId))
                throw new UnauthorizedAccessException(
                    $"Faculty '{facultyId}' is not authorised for course '{courseId}'.");
        }
    }
}
