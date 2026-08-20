using System;
using System.Collections.Generic;

namespace NexusEnroll.Shared
{
    public class IntegrationEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class StudentCreatedEvent : IntegrationEvent
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string StudentNumber { get; set; }
        public string ProgramId { get; set; }
        public int EnrolledYear { get; set; }
    }

    public class StudentDeletedEvent : IntegrationEvent
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
    }

    public class FacultyCreatedEvent : IntegrationEvent
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string EmployeeNumber { get; set; }
        public string Department { get; set; }
        public string Rank { get; set; }
    }

    public class FacultyDeletedEvent : IntegrationEvent
    {
        public string FacultyId { get; set; }
        public string FacultyName { get; set; }
    }

    public class UserStatusChangedEvent : IntegrationEvent
    {
        public string UserId { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class CourseCreatedEvent : IntegrationEvent
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
        public CourseStatus Status { get; set; }
        public List<string> PrerequisiteCourseIds { get; set; }
    }

    public class CourseUpdatedEvent : IntegrationEvent
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
        public CourseStatus Status { get; set; }
        public List<string> PrerequisiteCourseIds { get; set; }
    }

    public class CourseDeletedEvent : IntegrationEvent
    {
        public string CourseId { get; set; }
    }

    public class StudentEnrolledEvent : IntegrationEvent
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string RecipientEmail { get; set; }
        public string RecipientPhone { get; set; }
        public string CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Message { get; set; }
    }

    public class StudentDroppedEvent : IntegrationEvent
    {
        public string StudentId { get; set; }
        public string CourseId { get; set; }
        public string RecipientEmail { get; set; }
        public string RecipientPhone { get; set; }
        public string Message { get; set; }
    }

    public class WaitlistJoinedEvent : IntegrationEvent
    {
        public string StudentId { get; set; }
        public string CourseId { get; set; }
        public string RecipientEmail { get; set; }
        public string RecipientPhone { get; set; }
        public int Position { get; set; }
    }

    public class WaitlistPromotedEvent : IntegrationEvent
    {
        public string StudentId { get; set; }
        public string CourseId { get; set; }
        public string RecipientEmail { get; set; }
        public string RecipientPhone { get; set; }
        public string Message { get; set; }
    }

    public class GradesSubmittedEvent : IntegrationEvent
    {
        public string FacultyId { get; set; }
        public string CourseId { get; set; }
        public int SuccessCount { get; set; }
        public string Message { get; set; }
        // Holds mapping of StudentId -> Grade string
        public Dictionary<string, string> Grades { get; set; }
    }

    public class GradesApprovedEvent : IntegrationEvent
    {
        public string CourseId { get; set; }
        public int ApprovedCount { get; set; }
        public string Message { get; set; }
    }

    public class CourseChangeRequestedEvent : IntegrationEvent
    {
        public string RequestId { get; set; }
        public string CourseId { get; set; }
        public string FacultyId { get; set; }
        public string FieldChanged { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Message { get; set; }
    }

    public class CourseChangeApprovedEvent : IntegrationEvent
    {
        public string RequestId { get; set; }
        public string CourseId { get; set; }
        public string FieldChanged { get; set; }
        public string NewValue { get; set; }
    }

    public class CourseChangeRejectedEvent : IntegrationEvent
    {
        public string RequestId { get; set; }
        public string Reason { get; set; }
    }
}
