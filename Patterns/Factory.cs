using System;
using System.Collections.Generic;
using NexusEnroll.Models;

namespace NexusEnroll.Patterns
{

    // Define the Factory Interface (IUserFactory)
    public interface IUserFactory
    {
        User CreateUser(string userId, string fullName, string email, string phone, IDictionary<string, object> attributes = null);
    }

    
    // Implement Concrete Factories for Each User Role
    

    public class StudentFactory : IUserFactory
    {
        public User CreateUser(string userId, string fullName, string email, string phone, IDictionary<string, object> attributes = null)
        {
            string studentNumber = attributes?.TryGetValue("StudentNumber", out var sn) == true ? sn?.ToString() : $"STU-{userId}";
            string programId = attributes?.TryGetValue("ProgramId", out var pid) == true ? pid?.ToString() : "BS-CS";
            int enrolledYear = attributes?.TryGetValue("EnrolledYear", out var ey) == true ? Convert.ToInt32(ey) : DateTime.UtcNow.Year;

            return new Student(userId, fullName, email, phone, studentNumber, programId, enrolledYear);
        }
    }


    public class FacultyFactory : IUserFactory
    {
        public User CreateUser(string userId, string fullName, string email, string phone, IDictionary<string, object> attributes = null)
        {
            string employeeNumber = attributes?.TryGetValue("EmployeeNumber", out var en) == true ? en?.ToString() : $"FAC-{userId}";
            string department = attributes?.TryGetValue("Department", out var dept) == true ? dept?.ToString() : "Computer Science";
            string rank = attributes?.TryGetValue("Rank", out var rk) == true ? rk?.ToString() : "Lecturer";

            return new Faculty(userId, fullName, email, phone, employeeNumber, department, rank);
        }
    }


    public class AdminFactory : IUserFactory
    {
        public User CreateUser(string userId, string fullName, string email, string phone, IDictionary<string, object> attributes = null)
        {
            string staffNumber = attributes?.TryGetValue("StaffNumber", out var stf) == true ? stf?.ToString() : $"ADM-{userId}";
            string office = attributes?.TryGetValue("Office", out var off) == true ? off?.ToString() : "Admin Office 101";
            AdminScope scope = attributes?.TryGetValue("Scope", out var sc) == true && sc is AdminScope adminScope 
                ? adminScope 
                : AdminScope.Full;

            return new Admin(userId, fullName, email, phone, staffNumber, office, scope);
        }
    }


    //  Implement ConcreteUserFactory (Unified Role-Based Factory)

    public class ConcreteUserFactory : IUserFactory
    {
        private readonly UserRole _targetRole;

        public ConcreteUserFactory(UserRole targetRole = UserRole.Student)
        {
            _targetRole = targetRole;
        }

        public User CreateUser(string userId, string fullName, string email, string phone, IDictionary<string, object> attributes = null)
        {
            return _targetRole switch
            {
                UserRole.Student => new StudentFactory().CreateUser(userId, fullName, email, phone, attributes),
                UserRole.Faculty => new FacultyFactory().CreateUser(userId, fullName, email, phone, attributes),
                UserRole.Admin => new AdminFactory().CreateUser(userId, fullName, email, phone, attributes),
                _ => throw new ArgumentOutOfRangeException(nameof(_targetRole), $"Unsupported user role: {_targetRole}")
            };
        }
    }


    // Implement UserFactoryManager (Central Registry)

    public class UserFactoryManager
    {
        private readonly Dictionary<UserRole, IUserFactory> _factories;

        public UserFactoryManager()
        {
            _factories = new Dictionary<UserRole, IUserFactory>
            {
                { UserRole.Student, new StudentFactory() },
                { UserRole.Faculty, new FacultyFactory() },
                { UserRole.Admin, new AdminFactory() }
            };
        }

        public void RegisterFactory(UserRole role, IUserFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factories[role] = factory;
        }


        public User CreateUser(UserRole role, string userId, string fullName, string email, string phone, IDictionary<string, object> attributes = null)
        {
            if (!_factories.TryGetValue(role, out var factory))
            {
                throw new NotSupportedException($"No factory registered for user role: {role}");
            }

            return factory.CreateUser(userId, fullName, email, phone, attributes);
        }


        public Student CreateStudent(string userId, string fullName, string email, string phone, string studentNumber, string programId, int enrolledYear)
        {
            var attributes = new Dictionary<string, object>
            {
                { "StudentNumber", studentNumber },
                { "ProgramId", programId },
                { "EnrolledYear", enrolledYear }
            };
            return (Student)CreateUser(UserRole.Student, userId, fullName, email, phone, attributes);
        }

        public Faculty CreateFaculty(string userId, string fullName, string email, string phone, string employeeNumber, string department, string rank)
        {
            var attributes = new Dictionary<string, object>
            {
                { "EmployeeNumber", employeeNumber },
                { "Department", department },
                { "Rank", rank }
            };
            return (Faculty)CreateUser(UserRole.Faculty, userId, fullName, email, phone, attributes);
        }


        public Admin CreateAdmin(string userId, string fullName, string email, string phone, string staffNumber, string office, AdminScope scope = AdminScope.Full)
        {
            var attributes = new Dictionary<string, object>
            {
                { "StaffNumber", staffNumber },
                { "Office", office },
                { "Scope", scope }
            };
            return (Admin)CreateUser(UserRole.Admin, userId, fullName, email, phone, attributes);
        }
    }
}