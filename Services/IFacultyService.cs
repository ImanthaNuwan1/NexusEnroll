using System.Collections.Generic;
using NexusEnroll.Models;

namespace NexusEnroll.Services
{
    // Interface
    public interface IFacultyService
    {
        // Setup
        void RegisterFaculty(Faculty faculty);
        void AddCourse(Course course);
        void AddEnrollment(Enrollment enrollment);
        void RemoveEnrollment(string studentId, string courseId);
        void AddStudent(Student student);

        // Roster
        List<Student> GetClassRoster(string facultyId, string courseId);

        // Grading
        GradeSubmissionResult SubmitGrades(string facultyId, string courseId,
                                            Dictionary<string, string> rawGrades);

        GradeApprovalResult ApproveGrades(string courseId);

        GradeSubmissionStatus GetGradeStatus(string courseId);

        // Course changes
        CourseChangeRequest RequestCourseUpdate(string facultyId, string courseId,
                                                 string fieldChanged, string newValue);

        List<CourseChangeRequest> GetChangeRequestsFor(string facultyId);

        // Schedule
        List<Course> GetTeachingSchedule(string facultyId);
    }
}
