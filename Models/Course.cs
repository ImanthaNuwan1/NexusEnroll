using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusEnroll.Models
{

    public enum CourseStatus
    {
        Open,

        Closed,

        Cancelled
    }

    public enum Grade
    {
        A,
        B,
        C,
        D,
        F,
        W,
        I
    }

    public enum EnrollmentStatus
    {
        Enrolled,
        Waitlisted,
        Completed,
        Dropped
    }

    public enum GradeSubmissionStatus
    {
        NotSubmitted,
        Pending,
        Submitted
    }

    public static class GradeExtensions
    {
        public static bool IsPassing(this Grade grade)
            => grade == Grade.A
            || grade == Grade.B
            || grade == Grade.C
            || grade == Grade.D;
    }

    public class CourseSchedule
    {
        public string Days { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string Location { get; set; }

        public CourseSchedule() { }

        public CourseSchedule(string days, TimeSpan start, TimeSpan end, string location)
        {
            Days      = days;
            StartTime = start;
            EndTime   = end;
            Location  = location;
        }

        public bool ConflictsWith(CourseSchedule other)
        {
            if (other == null) return false;
            if (string.IsNullOrEmpty(Days) || string.IsNullOrEmpty(other.Days)) return false;

            bool shareDay = Days.Any(d => other.Days.IndexOf(d) >= 0);
            if (!shareDay) return false;

            return StartTime < other.EndTime && other.StartTime < EndTime;
        }

        public override string ToString()
            => Days + " " + StartTime + "-" + EndTime + " @ " + Location;
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

        public CourseSchedule Schedule { get; set; }

        public CourseStatus Status { get; set; }

        public GradeSubmissionStatus GradeStatus { get; set; }

        private readonly List<string> _prerequisiteCourseIds;

        public IReadOnlyList<string> PrerequisiteCourseIds => _prerequisiteCourseIds;

        private readonly List<string> _waitlist;

        public IReadOnlyList<string> Waitlist => _waitlist;

        public Course()
        {
            _prerequisiteCourseIds = new List<string>();
            _waitlist              = new List<string>();
            Status                 = CourseStatus.Open;
            GradeStatus            = GradeSubmissionStatus.NotSubmitted;
        }

        public Course(string courseId, string courseCode, string courseName,
                      string department, int credits, int capacity,
                      string instructorId, string instructorName,
                      CourseSchedule schedule, string semester)
            : this()
        {
            CourseId         = courseId;
            CourseCode       = courseCode;
            CourseName       = courseName;
            Department       = department;
            Credits          = credits;
            Capacity         = capacity;
            InstructorId     = instructorId;
            InstructorName   = instructorName;
            Schedule         = schedule;
            Semester         = semester;
            Description      = string.Empty;
        }


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


        public void AddPrerequisite(string prerequisiteCourseId)
        {
            if (prerequisiteCourseId == null) return;
            if (!_prerequisiteCourseIds.Contains(prerequisiteCourseId))
                _prerequisiteCourseIds.Add(prerequisiteCourseId);
        }

        public void RemovePrerequisite(string prerequisiteCourseId)
        {
            if (prerequisiteCourseId == null) return;
            _prerequisiteCourseIds.Remove(prerequisiteCourseId);
        }

        public bool HasPrerequisite(string prerequisiteCourseId)
            => prerequisiteCourseId != null
               && _prerequisiteCourseIds.Contains(prerequisiteCourseId);

        public bool MeetsPrerequisites(IEnumerable<string> completedCourseIds)
        {
            if (_prerequisiteCourseIds.Count == 0) return true;
            if (completedCourseIds == null) return false;

            var completed = new HashSet<string>(completedCourseIds);
            return _prerequisiteCourseIds.All(id => completed.Contains(id));
        }


        public void AddToWaitlist(string studentId)
        {
            if (studentId == null) return;
            if (!_waitlist.Contains(studentId))
                _waitlist.Add(studentId);
        }

        public void RemoveFromWaitlist(string studentId)
        {
            if (studentId == null) return;
            _waitlist.Remove(studentId);
        }

        public string PopNextWaitlistedStudent()
        {
            if (_waitlist.Count == 0) return null;
            string next = _waitlist[0];
            _waitlist.RemoveAt(0);
            return next;
        }

        public bool IsWaitlisted(string studentId)
            => studentId != null && _waitlist.Contains(studentId);

        public bool HasTimeConflictWith(Course other)
        {
            if (other == null || Schedule == null || other.Schedule == null) return false;
            return Schedule.ConflictsWith(other.Schedule);
        }

        public override string ToString()
            => CourseCode + " - " + CourseName
               + " (" + EnrolledCount + "/" + Capacity
               + ", " + Status + ")";
    }


    public class CourseRecord
    {
        public string CourseId { get; set; }

        public string CourseCode { get; set; }

        public string CourseName { get; set; }

        public string Semester { get; set; }

        public Grade Grade { get; set; }

        public int Credits { get; set; }

        public DateTime CompletedAt { get; set; }

        public CourseRecord() { }

        public CourseRecord(string courseId, string courseCode, string courseName,
                            string semester, Grade grade, int credits)
        {
            CourseId     = courseId;
            CourseCode   = courseCode;
            CourseName   = courseName;
            Semester     = semester;
            Grade        = grade;
            Credits      = credits;
            CompletedAt  = DateTime.UtcNow;
        }

        public bool IsPassing => Grade.IsPassing();
    }


    public class Enrollment
    {
        public string EnrollmentId { get; set; }

        public string StudentId { get; set; }

        public string CourseId { get; set; }

        public EnrollmentStatus Status { get; set; }

        public DateTime EnrolledAt { get; set; }

        public Grade? Grade { get; set; }

        public DateTime? GradedAt { get; set; }

        public GradeSubmissionStatus SubmissionStatus { get; set; }

        public Enrollment() { }

        public Enrollment(string enrollmentId, string studentId, string courseId,
                          EnrollmentStatus status = EnrollmentStatus.Enrolled)
        {
            EnrollmentId     = enrollmentId;
            StudentId        = studentId;
            CourseId         = courseId;
            Status           = status;
            EnrolledAt       = DateTime.UtcNow;
            SubmissionStatus = GradeSubmissionStatus.NotSubmitted;
        }

        public void SubmitGradePending(Grade grade)
        {
            Grade            = grade;
            SubmissionStatus = GradeSubmissionStatus.Pending;
        }

        public void FinaliseGrade()
        {
            if (SubmissionStatus != GradeSubmissionStatus.Pending)
                throw new InvalidOperationException(
                    "Cannot finalise a grade that has not been submitted as Pending.");
            if (Grade == null)
                throw new InvalidOperationException("Cannot finalise a null grade.");

            Status           = EnrollmentStatus.Completed;
            GradedAt         = DateTime.UtcNow;
            SubmissionStatus = GradeSubmissionStatus.Submitted;
        }

        public void Drop()
        {
            Status = EnrollmentStatus.Dropped;
        }

        public override string ToString()
            => "Enrollment[" + StudentId + " -> " + CourseId
               + ", " + Status + ", " + SubmissionStatus + "]";
    }
}
