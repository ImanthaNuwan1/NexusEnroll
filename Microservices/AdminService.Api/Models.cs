using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NexusEnroll.Shared;

namespace NexusEnroll.AdminService
{
    // =========================================================================
    // DOMAIN ENTITIES
    // =========================================================================

    public class Admin
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string StaffNumber { get; set; }
        public string Office { get; set; }
        public AdminScope Scope { get; set; } = AdminScope.Full;
        public bool IsActive { get; set; } = true;
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
        public List<string> PrerequisiteCourseIds { get; set; } = new List<string>();

        public void Enroll()
        {
            if (EnrolledCount < Capacity)
            {
                EnrolledCount++;
                if (EnrolledCount >= Capacity)
                {
                    Status = CourseStatus.Closed;
                }
            }
        }

        public void Drop()
        {
            if (EnrolledCount > 0)
            {
                EnrolledCount--;
                if (Status == CourseStatus.Closed && EnrolledCount < Capacity)
                {
                    Status = CourseStatus.Open;
                }
            }
        }
    }

    public class StudentProfile
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string StudentNumber { get; set; }
        public string ProgramId { get; set; }
        public int EnrolledYear { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> EnrolledCourseIds { get; set; } = new List<string>();
        public List<string> WaitlistedCourseIds { get; set; } = new List<string>();
    }

    public class FacultyProfile
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

    public class DegreeProgram
    {
        public string ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string Department { get; set; }
        public List<string> RequiredCourseIds { get; set; } = new List<string>();
        public List<string> ElectiveCourseIds { get; set; } = new List<string>();

        public void AddRequiredCourse(string courseId)
        {
            if (string.IsNullOrEmpty(courseId)) return;
            if (!RequiredCourseIds.Contains(courseId))
                RequiredCourseIds.Add(courseId);
        }

        public void AddElectiveCourse(string courseId)
        {
            if (string.IsNullOrEmpty(courseId)) return;
            if (!ElectiveCourseIds.Contains(courseId))
                ElectiveCourseIds.Add(courseId);
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

    // =========================================================================
    // INPUT DTOs
    // =========================================================================

    public class CreateCourseDto
    {
        [Required]
        public string CourseId { get; set; }

        [Required]
        public string CourseCode { get; set; }

        [Required]
        public string CourseName { get; set; }

        public string Description { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; }

        public string InstructorId { get; set; }
        public string InstructorName { get; set; } = "Unassigned";

        [Range(1, 10, ErrorMessage = "Credits must be between 1 and 10.")]
        public int Credits { get; set; }

        [Required]
        public string Semester { get; set; }

        [Range(1, 1000, ErrorMessage = "Capacity must be at least 1.")]
        public int Capacity { get; set; }

        [Required]
        public string Days { get; set; }

        [Required]
        public string StartTime { get; set; }

        [Required]
        public string EndTime { get; set; }

        [Required]
        public string Location { get; set; }

        public List<string> PrerequisiteCourseIds { get; set; } = new List<string>();
    }

    public class CreateDegreeProgramDto
    {
        [Required]
        public string ProgramId { get; set; }

        [Required]
        public string ProgramName { get; set; }

        [Required]
        public string Department { get; set; }

        public List<string> RequiredCourseIds { get; set; } = new List<string>();
        public List<string> ElectiveCourseIds { get; set; } = new List<string>();
    }

    public class CreateStudentDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        public string Phone { get; set; }
        public string StudentNumber { get; set; }

        [Required]
        public string ProgramId { get; set; }

        [Range(1900, 2100, ErrorMessage = "Enrolled year must be a valid year.")]
        public int EnrolledYear { get; set; }
    }

    public class CreateFacultyDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        public string Phone { get; set; }
        public string EmployeeNumber { get; set; }

        [Required]
        public string Department { get; set; }

        public string Rank { get; set; } = "Lecturer";
        public List<string> AssignedCourseIds { get; set; } = new List<string>();
    }

    public class ForceEnrollDto
    {
        [Required]
        public string StudentId { get; set; }

        [Required]
        public string CourseId { get; set; }
    }

    public class CreateAdminDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        public string Phone { get; set; }
        public string StaffNumber { get; set; }
        public string Office { get; set; }
        public AdminScope Scope { get; set; } = AdminScope.Full;
    }
}
