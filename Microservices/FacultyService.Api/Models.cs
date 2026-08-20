using System;
using System.Collections.Generic;
using NexusEnroll.Shared;

namespace NexusEnroll.FacultyService
{
    // =========================================================================
    // DOMAIN ENTITIES
    // =========================================================================

    public class Faculty
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string EmployeeNumber { get; set; }
        public string Department { get; set; }
        public string Rank { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> TeachingCourseIds { get; set; } = new List<string>();

        public void AssignCourse(string courseId)
        {
            if (string.IsNullOrEmpty(courseId)) return;
            if (!TeachingCourseIds.Contains(courseId))
                TeachingCourseIds.Add(courseId);
        }

        public void UnassignCourse(string courseId)
        {
            if (string.IsNullOrEmpty(courseId)) return;
            TeachingCourseIds.Remove(courseId);
        }
    }

    public class CourseChangeRequest
    {
        public string RequestId { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string RequestedByFacultyId { get; set; } = "";
        public string FieldChanged { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;
        public string ReviewedByAdminId { get; set; } = "";
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }

    public class Course
    {
        public string CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }
        public string Department { get; set; }
        public string InstructorId { get; set; }
        public string InstructorName { get; set; }
        public int Credits { get; set; }
        public string Semester { get; set; }
        public int Capacity { get; set; }
        public int EnrolledCount { get; set; }
        public string Days { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }
        public CourseStatus Status { get; set; } = CourseStatus.Open;
        public GradeSubmissionStatus GradeStatus { get; set; } = GradeSubmissionStatus.NotSubmitted;
    }

    public class RosterEntry
    {
        public int Id { get; set; }
        public string CourseId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public Grade? Grade { get; set; }
    }
}
