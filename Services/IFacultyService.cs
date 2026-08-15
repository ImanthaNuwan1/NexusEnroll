using System.Collections.Generic;
using NexusEnroll.Models;

namespace NexusEnroll.Services
{
    /// <summary>
    /// Contract for the Faculty module (SRS section 3.2).
    /// UniversityFacade depends on this interface, not on the concrete
    /// FacultyService class -- this is the Dependency Inversion relationship
    /// shown on the class diagram (UniversityFacade *-- IFacultyService,
    /// FacultyService ..|> IFacultyService).
    /// </summary>
    public interface IFacultyService
    {
        // ---- Setup / seeding -------------------------------------------
        // Faculty objects are created elsewhere (Factory Method pattern) and
        // handed to this service -- FacultyService manages them, it does not
        // construct them (Single Responsibility Principle).
        void RegisterFaculty(Faculty faculty);
        void AddCourse(Course course);
        void AddEnrollment(Enrollment enrollment);
        void AddStudent(Student student);

        // ---- Class Roster Viewing (SRS 3.2.1) ---------------------------
        List<Student> GetClassRoster(string facultyId, string courseId);

        // ---- Grade Submission (SRS 3.2.2) --------------------------------
        GradeSubmissionResult SubmitGrades(string facultyId, string courseId,
                                            Dictionary<string, string> rawGrades);

        // ---- Grade Approval (triggered by Administrator, executed here
        //      because the grade data belongs to the Faculty/Course domain) --
        GradeApprovalResult ApproveGrades(string courseId);

        GradeSubmissionStatus GetGradeStatus(string courseId);

        // ---- Course Information Management (SRS 3.2.3) -------------------
        CourseChangeRequest RequestCourseUpdate(string facultyId, string courseId,
                                                 string fieldChanged, string newValue);

        List<CourseChangeRequest> GetChangeRequestsFor(string facultyId);

        // ---- Teaching schedule -------------------------------------------
        List<Course> GetTeachingSchedule(string facultyId);
    }
}
