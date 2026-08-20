using System;
using System.Collections.Generic;
using System.Linq;
using NexusEnroll.Models;

namespace NexusEnroll.Services
{

    // Define the IStudentService Interface

   public interface IStudentService
    {
        // Course Catalogue Browse & Search
        IEnumerable<Course> BrowseCatalogue(IEnumerable<Course> allCourses, string department = null, string keyword = null, string instructorName = null);
        Course GetCourseDetails(IEnumerable<Course> allCourses, string courseId);

        // Registration & Enrolment Validation
        bool EnrolInCourse(Student student, Course course, IEnumerable<Course> allCourses, out string validationMessage);
        bool DropCourse(Student student, Course course, out string message);
        bool JoinWaitlist(Student student, Course course, out string message);

        // Schedule & Academic Progress
        IEnumerable<Course> GetStudentSchedule(Student student, IEnumerable<Course> allCourses);
        IEnumerable<CourseRecord> GetAcademicHistory(Student student);
        IEnumerable<string> GetRemainingRequiredCourses(Student student, DegreeProgram program);
    }

   
    public class StudentService : IStudentService
    {
      
        // Course Catalogue Browse & Search
        
        public IEnumerable<Course> BrowseCatalogue(IEnumerable<Course> allCourses, string department = null, string keyword = null, string instructorName = null)
        {
            if (allCourses == null) return Enumerable.Empty<Course>();

            var query = allCourses.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(c => c.Department != null && c.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(instructorName))
            {
                query = query.Where(c => c.InstructorName != null && c.InstructorName.Contains(instructorName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(c => 
                    (c.CourseCode != null && c.CourseCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (c.CourseName != null && c.CourseName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (c.Description != null && c.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            return query.ToList();
        }

        public Course GetCourseDetails(IEnumerable<Course> allCourses, string courseId)
        {
            if (allCourses == null || string.IsNullOrWhiteSpace(courseId)) return null;
            return allCourses.FirstOrDefault(c => c.CourseId == courseId);
        }

       
        // Registration and Enrolment (Validation Rules)
        
        public bool EnrolInCourse(Student student, Course course, IEnumerable<Course> allCourses, out string validationMessage)
        {
            if (student == null)
            {
                validationMessage = "Invalid student reference.";
                return false;
            }

            if (course == null)
            {
                validationMessage = "Invalid course reference.";
                return false;
            }

            // Already enrolled?
            if (student.EnrolledCourseIds.Contains(course.CourseId))
            {
                validationMessage = $"Already enrolled in course {course.CourseCode}.";
                return false;
            }

            // Prerequisite Check
            if (!student.HasCompletedPrerequisites(course.PrerequisiteCourseIds))
            {
                validationMessage = $"Enrolment failed: Student has not met prerequisites for {course.CourseCode}.";
                return false;
            }

            // Capacity Check
            if (!course.HasAvailableSeats())
            {
                validationMessage = $"Enrolment failed: Course {course.CourseCode} is at full capacity ({course.Capacity}/{course.Capacity}). You can join the waitlist.";
                return false;
            }

            // Schedule Time Conflict Check
            var enrolledCourses = GetStudentSchedule(student, allCourses);
            foreach (var enrolledCourse in enrolledCourses)
            {
                if (course.HasTimeConflictWith(enrolledCourse))
                {
                    validationMessage = $"Enrolment failed: Schedule conflict detected between {course.CourseCode} ({course.Schedule}) and enrolled course {enrolledCourse.CourseCode} ({enrolledCourse.Schedule}).";
                    return false;
                }
            }

            
            bool success = course.Enroll();
            if (success)
            {
                student.EnrollInCourse(course.CourseId);

               
                if (course.IsWaitlisted(student.UserId))
                {
                    course.RemoveFromWaitlist(student.UserId);
                    student.WaitlistedCourseIds.Remove(course.CourseId);
                }

                validationMessage = $"Successfully enrolled in {course.CourseCode} - {course.CourseName}.";
                return true;
            }

            validationMessage = "Enrolment failed due to system error.";
            return false;
        }

        public bool DropCourse(Student student, Course course, out string message)
        {
            if (student == null || course == null)
            {
                message = "Invalid parameters.";
                return false;
            }

            if (!student.EnrolledCourseIds.Contains(course.CourseId))
            {
                message = $"Student is not enrolled in {course.CourseCode}.";
                return false;
            }

            
            course.Drop();
            student.DropCourse(course.CourseId);
            message = $"Successfully dropped course {course.CourseCode}.";

            return true;
        }

        public bool JoinWaitlist(Student student, Course course, out string message)
        {
            if (student == null || course == null)
            {
                message = "Invalid parameters.";
                return false;
            }

            if (student.EnrolledCourseIds.Contains(course.CourseId))
            {
                message = $"Already enrolled in {course.CourseCode}.";
                return false;
            }

            if (course.HasAvailableSeats())
            {
                message = $"Seats are still available in {course.CourseCode}. You can enrol directly.";
                return false;
            }

            if (course.IsWaitlisted(student.UserId))
            {
                message = $"Already on the waitlist for {course.CourseCode}.";
                return false;
            }

            course.AddToWaitlist(student.UserId);
            student.WaitlistForCourse(course.CourseId);
            message = $"Added to waitlist for {course.CourseCode}. Position: #{course.Waitlist.Count}";
            return true;
        }

    
        // Personal Schedule & Academic Progress Tracking
        
        public IEnumerable<Course> GetStudentSchedule(Student student, IEnumerable<Course> allCourses)
        {
            if (student == null || allCourses == null) return Enumerable.Empty<Course>();

            var enrolledIds = new HashSet<string>(student.EnrolledCourseIds);
            return allCourses.Where(c => enrolledIds.Contains(c.CourseId)).ToList();
        }

        public IEnumerable<CourseRecord> GetAcademicHistory(Student student)
        {
            return student?.AcademicHistory ?? Enumerable.Empty<CourseRecord>();
        }

        public IEnumerable<string> GetRemainingRequiredCourses(Student student, DegreeProgram program)
        {
            if (student == null || program == null) return Enumerable.Empty<string>();

            var completedCourseIds = new HashSet<string>(
                student.AcademicHistory
                       .Where(r => r.IsPassing)
                       .Select(r => r.CourseId));

            return program.RequiredCourseIds.Where(reqId => !completedCourseIds.Contains(reqId)).ToList();
        }
    }
}