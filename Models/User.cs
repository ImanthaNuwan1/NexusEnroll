using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusEnroll.Models
{ 
    public enum UserRole
    {Student,Faculty,Admin}

    public enum AdminScope
    {
        ReadOnly,
        CourseManagement,
        Full
    }

    public abstract class User
    {
        public string UserId { get; set; }
        
        public string FullName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public UserRole Role { get; protected set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        protected User(string userId, string fullName, string email, string phone, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            UserId    = userId;
            FullName  = fullName;
            Email     = email;
            Phone     = phone;
            Role      = role;
            IsActive  = true;
            CreatedAt = DateTime.UtcNow;
        }

        public abstract string GetProfile();

        public virtual void Deactivate()
        {
            IsActive = false;
        }

        public virtual void Activate()
        {
            IsActive = true;
        }

        public virtual void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        public override string ToString()
            => $"[{Role}] {FullName} <{Email}> ({(IsActive ? "active" : "inactive")})";
    }


    // Student
    public class Student : User
    {
        public string StudentNumber { get; set; }

        public string ProgramId { get; set; }

        public int EnrolledYear { get; set; }

        public List<string> EnrolledCourseIds { get; set; }

        public List<string> WaitlistedCourseIds { get; set; }

        public List<CourseRecord> AcademicHistory { get; set; }

        public Student(string userId, string fullName, string email, string phone,
                       string studentNumber, string programId, int enrolledYear)
            : base(userId, fullName, email, phone, UserRole.Student)
        {
            StudentNumber       = studentNumber;
            ProgramId           = programId;
            EnrolledYear        = enrolledYear;
            EnrolledCourseIds   = new List<string>();
            WaitlistedCourseIds = new List<string>();
            AcademicHistory     = new List<CourseRecord>();
        }

        /// <inheritdoc/>
        public override string GetProfile()
        {
            return "Student: " + FullName + " (" + StudentNumber + ")\n" +
                   "  Programme : " + ProgramId + "\n" +
                   "  Enrolled  : " + EnrolledYear + "\n" +
                   "  Email     : " + Email + "\n" +
                   "  Enrolled  : " + EnrolledCourseIds.Count + " course(s), " +
                   "Waitlist: " + WaitlistedCourseIds.Count + ", " +
                   "Completed: " + AcademicHistory.Count;
        }

        public void EnrollInCourse(string courseId)
        {
            if (courseId == null) return;
            if (!EnrolledCourseIds.Contains(courseId))EnrolledCourseIds.Add(courseId);
        }

        public void DropCourse(string courseId)
        {
            if (courseId == null) return;
            EnrolledCourseIds.Remove(courseId);
            WaitlistedCourseIds.Remove(courseId);
        }

        public void WaitlistForCourse(string courseId)
        {
            if (courseId == null) return;
            if (!WaitlistedCourseIds.Contains(courseId))WaitlistedCourseIds.Add(courseId);
        }

        public void RecordCompletedCourse(CourseRecord record)
        {
            if (record != null)
                AcademicHistory.Add(record);
        }

        public bool HasCompletedPrerequisites(IEnumerable<string> prerequisiteCourseIds)
        {
            if (prerequisiteCourseIds == null) return true;

            var passed = new HashSet<string>(
                AcademicHistory
                    .Where(r => r.Grade.IsPassing())
                    .Select(r => r.CourseId));

            return prerequisiteCourseIds.All(id => passed.Contains(id));
        }

        public IEnumerable<string> GetCurrentEnrollments() => EnrolledCourseIds;
    }


    // Faculty
    public class Faculty : User
    {
        public string EmployeeNumber { get; set; }

        public string Department { get; set; }

        public string Rank { get; set; }

        public List<string> TeachingCourseIds { get; set; }

        public Faculty(string userId, string fullName, string email, string phone,
                       string employeeNumber, string department, string rank)
            : base(userId, fullName, email, phone, UserRole.Faculty)
        {
            EmployeeNumber   = employeeNumber;
            Department       = department;
            Rank             = rank;
            TeachingCourseIds = new List<string>();
        }

        /// <inheritdoc/>
        public override string GetProfile()
        {
            return "Faculty: " + FullName + " (" + EmployeeNumber + ")\n" +
                   "  Department : " + Department + "\n" +
                   "  Rank       : " + Rank + "\n" +
                   "  Email      : " + Email + "\n" +
                   "  Teaching   : " + TeachingCourseIds.Count + " course(s)";
        }

        public void AssignCourse(string courseId)
        {
            if (courseId == null) return;
            if (!TeachingCourseIds.Contains(courseId))
                TeachingCourseIds.Add(courseId);
        }

        public void UnassignCourse(string courseId)
        {
            if (courseId == null) return;
            TeachingCourseIds.Remove(courseId);
        }

        public bool TeachesCourse(string courseId)
            => courseId != null && TeachingCourseIds.Contains(courseId);
    }


    // Admin
    public class Admin : User
    {
        public string StaffNumber { get; set; }

        public string Office { get; set; }

        public AdminScope Scope { get; set; }

        public Admin(string userId, string fullName, string email, string phone,
                     string staffNumber, string office, AdminScope scope = AdminScope.Full)
            : base(userId, fullName, email, phone, UserRole.Admin)
        {
            StaffNumber = staffNumber;
            Office      = office;
            Scope       = scope;
        }

        /// <inheritdoc/>
        public override string GetProfile()
        {
            return "Administrator: " + FullName + " (" + StaffNumber + ")\n" +
                   "  Office : " + Office + "\n" +
                   "  Scope  : " + Scope + "\n" +
                   "  Email  : " + Email;
        }

        public bool CanManageCourses
            => Scope == AdminScope.CourseManagement || Scope == AdminScope.Full;

        public bool CanManageAccounts
            => Scope == AdminScope.Full;
    }
}
