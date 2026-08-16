using System;
using System.Collections.Generic;
using System.Linq;
using NexusEnroll.Models;

namespace NexusEnroll.Services
{
    // One failed grade within a batch submission.
    public class GradeError
    {
        public string StudentId { get; set; }
        public string RawValue { get; set; }
        public string Reason { get; set; }

        public GradeError(string studentId, string rawValue, string reason)
        {
            StudentId = studentId;
            // @Damika i don't think this is supposed to be here: lines 19 - 24
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using NexusEnroll.Models;

            namespace NexusEnroll.Services
            {
                // One failed grade within a batch submission.
                public class GradeError
                {
                    public string StudentId { get; set; }
                    public string RawValue { get; set; }
                    public string Reason { get; set; }

                    public GradeError(string studentId, string rawValue, string reason)
                    {
                        StudentId = studentId;
                        RawValue  = rawValue;
                        Reason    = reason;
                    }

                    public override string ToString()
                        => $"{StudentId}: '{RawValue}' rejected -- {Reason}";
                }

                // Result of a batch grade submission (SRS 3.2.2).
                // Valid grades are committed, invalid ones are reported,
                // and remaining grades continue processing.
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
                        SuccessCount   = successCount;
                        Errors         = errors ?? new List<GradeError>();
                    }

                    public override string ToString()
                        => AllSucceeded
                            ? $"All {SuccessCount} grade(s) submitted successfully."
                            : $"{SuccessCount}/{TotalSubmitted} grade(s) submitted. {Errors.Count} rejected -- correct and resubmit only those.";
                }

                // Result of an administrator approving a course's pending grades.
                public class GradeApprovalResult
                {
                    public bool Success { get; }
                    public string Message { get; }
                    public int ApprovedCount { get; }

                    public GradeApprovalResult(bool success, string message, int approvedCount)
                    {
                        Success       = success;
                        Message       = message;
                        ApprovedCount = approvedCount;
                    }
                }

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

                    public List<Student> GetClassRoster(string facultyId, string courseId)
                    {
                        VerifyFacultyTeachesCourse(facultyId, courseId);

                        return _enrollments.Values
                            .Where(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Enrolled)
                            .Select(e => _students.TryGetValue(e.StudentId, out var s) ? s : null)
                            .Where(s => s != null)
                            .OrderBy(s => s.FullName)
                            .ToList();
                    }

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
                            string rawValue  = entry.Value;

                            if (string.IsNullOrWhiteSpace(rawValue) || !Enum.TryParse<Grade>(rawValue, true, out var grade))
                            {
                                errors.Add(new GradeError(studentId, rawValue, "Invalid grade format"));
                                continue;
                            }

                            string key = EnrollmentKey(studentId, courseId);
                            if (!_enrollments.TryGetValue(key, out var enrollment) ||
                                enrollment.Status != EnrollmentStatus.Enrolled)
                            {
                                errors.Add(new GradeError(studentId, rawValue, "Student not enrolled in this course"));
                                continue;
                            }

                            enrollment.SubmitGradePending(grade);
                            successCount++;
                        }

                        if (successCount > 0)
                        {
                            _notificationService.NotifyEvent(
                                "grades_submitted",
                                new Dictionary<string, object>
                                {
                                    { "RecipientId", facultyId },
                                    { "CourseId",    courseId },
                                    { "SuccessCount", successCount },
                                    { "Message",     $"{successCount} grade(s) for {courseId} submitted and awaiting approval." }
                                });
                        }

                        return new GradeSubmissionResult(rawGrades.Count, successCount, errors);
                    }

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
                            enrollment.FinaliseGrade();

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
                            new Dictionary<string, object>
                            {
                                { "CourseId",     courseId },
                                { "ApprovedCount", pending.Count },
                                { "Message",      $"{pending.Count} grade(s) for {courseId} approved and finalised." }
                            });

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

                    public CourseChangeRequest RequestCourseUpdate(string facultyId, string courseId,
                                                                     string fieldChanged, string newValue)
                    {
                        VerifyFacultyTeachesCourse(facultyId, courseId);
                        var course = _courses[courseId];

                        string oldValue = fieldChanged switch
                        {
                            "Description" => course.Description,
                            "Capacity"    => course.Capacity.ToString(),
                            _ => "(unspecified)"
                        };

                        _requestSequence++;
                        var request = new CourseChangeRequest(
                            requestId:    $"CCR-{_requestSequence:D4}",
                            courseId:     courseId,
                            facultyId:    facultyId,
                            fieldChanged: fieldChanged,
                            oldValue:     oldValue,
                            newValue:     newValue);

                        _changeRequests.Add(request);

                        _notificationService.NotifyEvent(
                            "course_change_requested",
                            new Dictionary<string, object>
                            {
                                { "RecipientId", facultyId },
                                { "RequestId",   request.RequestId },
                                { "CourseId",    courseId },
                                { "Message",     $"Change request {request.RequestId} for {courseId} submitted, awaiting admin approval." }
                            });

                        return request;
                    }

                    public List<CourseChangeRequest> GetChangeRequestsFor(string facultyId)
                        => _changeRequests.Where(r => r.RequestedByFacultyId == facultyId).ToList();

                    public List<Course> GetTeachingSchedule(string facultyId)
                    {
                        if (!_faculty.ContainsKey(facultyId))
                            throw new KeyNotFoundException($"Faculty '{facultyId}' not found.");

                        return _courses.Values
                            .Where(c => c.InstructorId == facultyId)
                            .OrderBy(c => c.Schedule?.Days)
                            .ToList();
                    }

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