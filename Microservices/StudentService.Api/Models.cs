using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NexusEnroll.Shared;

namespace NexusEnroll.StudentService
{
    // =========================================================================
    // DOMAIN ENTITIES
    // =========================================================================

    public class Student
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
        public List<CourseRecord> AcademicHistory { get; set; } = new List<CourseRecord>();

        public void EnrollInCourse(string courseId)
        {
            if (string.IsNullOrEmpty(courseId)) return;
            if (!EnrolledCourseIds.Contains(courseId))
                EnrolledCourseIds.Add(courseId);
        }

        public void DropCourse(string courseId)
        {
            if (string.IsNullOrEmpty(courseId)) return;
            EnrolledCourseIds.Remove(courseId);
            WaitlistedCourseIds.Remove(courseId);
        }

        public void WaitlistForCourse(string courseId)
        {
            if (string.IsNullOrEmpty(courseId)) return;
            if (!WaitlistedCourseIds.Contains(courseId))
                WaitlistedCourseIds.Add(courseId);
        }

        public bool HasCompletedPrerequisites(IEnumerable<string> prerequisiteCourseIds)
        {
            if (prerequisiteCourseIds == null || !prerequisiteCourseIds.Any()) return true;

            var passed = new HashSet<string>(
                AcademicHistory
                    .Where(r => r.Grade.IsPassing())
                    .Select(r => r.CourseId)
            );

            return prerequisiteCourseIds.All(id => passed.Contains(id));
        }
    }

    public class CourseRecord
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public string CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Semester { get; set; }
        public Grade Grade { get; set; }
        public int Credits { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public bool IsPassing => Grade.IsPassing();
    }

    public class Enrollment
    {
        public string EnrollmentId { get; set; }
        public string StudentId { get; set; }
        public string CourseId { get; set; }
        public EnrollmentStatus Status { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public Grade? Grade { get; set; }
        public DateTime? GradedAt { get; set; }
        public GradeSubmissionStatus SubmissionStatus { get; set; } = GradeSubmissionStatus.NotSubmitted;

        public void SubmitGradePending(Grade grade)
        {
            Grade = grade;
            SubmissionStatus = GradeSubmissionStatus.Pending;
        }

        public void FinaliseGrade()
        {
            if (SubmissionStatus != GradeSubmissionStatus.Pending)
                throw new InvalidOperationException("Cannot finalise a grade that has not been submitted as Pending.");
            if (Grade == null)
                throw new InvalidOperationException("Cannot finalise a null grade.");

            Status = EnrollmentStatus.Completed;
            GradedAt = DateTime.UtcNow;
            SubmissionStatus = GradeSubmissionStatus.Submitted;
        }

        public void Drop()
        {
            Status = EnrollmentStatus.Dropped;
        }
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
        public List<string> PrerequisiteCourseIds { get; set; } = new List<string>();
        public List<string> Waitlist { get; set; } = new List<string>();

        public int AvailableSeats => Math.Max(0, Capacity - EnrolledCount);

        public bool HasAvailableSeats()
            => Status == CourseStatus.Open && EnrolledCount < Capacity;

        public bool Enroll()
        {
            if (!HasAvailableSeats()) return false;
            EnrolledCount++;
            if (EnrolledCount >= Capacity)
                Status = CourseStatus.Closed;
            return true;
        }

        public bool Drop()
        {
            if (EnrolledCount <= 0) return false;
            EnrolledCount--;
            if (Status == CourseStatus.Closed && EnrolledCount < Capacity)
                Status = CourseStatus.Open;
            return true;
        }

        public void AddToWaitlist(string studentId)
        {
            if (string.IsNullOrEmpty(studentId)) return;
            if (!Waitlist.Contains(studentId))
                Waitlist.Add(studentId);
        }

        public void RemoveFromWaitlist(string studentId)
        {
            if (string.IsNullOrEmpty(studentId)) return;
            Waitlist.Remove(studentId);
        }

        public string PopNextWaitlistedStudent()
        {
            if (Waitlist.Count == 0) return null;
            string next = Waitlist[0];
            Waitlist.RemoveAt(0);
            return next;
        }

        public bool IsWaitlisted(string studentId)
            => !string.IsNullOrEmpty(studentId) && Waitlist.Contains(studentId);

        public bool HasTimeConflictWith(Course other)
        {
            if (other == null) return false;
            if (string.IsNullOrEmpty(Days) || string.IsNullOrEmpty(other.Days)) return false;

            var myDays = ParseDays(Days);
            var otherDays = ParseDays(other.Days);
            
            if (!myDays.Overlaps(otherDays)) return false;

            return StartTime < other.EndTime && other.StartTime < EndTime;
        }

        private static HashSet<string> ParseDays(string daysStr)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(daysStr)) return set;

            int i = 0;
            while (i < daysStr.Length)
            {
                if (i + 1 < daysStr.Length && (daysStr.Substring(i, 2).Equals("Th", StringComparison.OrdinalIgnoreCase) ||
                                               daysStr.Substring(i, 2).Equals("Sa", StringComparison.OrdinalIgnoreCase) ||
                                               daysStr.Substring(i, 2).Equals("Su", StringComparison.OrdinalIgnoreCase)))
                {
                    set.Add(daysStr.Substring(i, 2));
                    i += 2;
                }
                else
                {
                    set.Add(daysStr[i].ToString());
                    i++;
                }
            }
            return set;
        }

        public override string ToString()
            => $"{Days} {StartTime}-{EndTime} @ {Location}";
    }

    public class DegreeProgram
    {
        public string ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string Department { get; set; }
        public List<string> RequiredCourseIds { get; set; } = new List<string>();
        public List<string> ElectiveCourseIds { get; set; } = new List<string>();
    }

    // =========================================================================
    // INPUT/OUTPUT DTOs
    // =========================================================================

    public class EnrollCourseDto
    {
        [Required(ErrorMessage = "StudentId is required.")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "CourseId is required.")]
        public string CourseId { get; set; }
    }

    public class DropCourseDto
    {
        [Required(ErrorMessage = "StudentId is required.")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "CourseId is required.")]
        public string CourseId { get; set; }
    }

    public class JoinWaitlistDto
    {
        [Required(ErrorMessage = "StudentId is required.")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "CourseId is required.")]
        public string CourseId { get; set; }
    }

    public class CourseResponseDto
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
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public int AvailableSeats { get; set; }
        public int WaitlistCount { get; set; }
    }

    public class CourseRecordResponseDto
    {
        public string CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Semester { get; set; }
        public string Grade { get; set; }
        public int Credits { get; set; }
        public string ResultDisplay { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
