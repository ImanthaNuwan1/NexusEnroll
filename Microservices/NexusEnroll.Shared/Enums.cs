using System;

namespace NexusEnroll.Shared
{
    public enum UserRole
    {
        Student,
        Faculty,
        Admin
    }

    public enum AdminScope
    {
        ReadOnly,
        CourseManagement,
        Full
    }

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

    public enum ChangeRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public static class GradeExtensions
    {
        public static bool IsPassing(this Grade grade)
            => grade == Grade.A
            || grade == Grade.B
            || grade == Grade.C
            || grade == Grade.D;
    }
}
